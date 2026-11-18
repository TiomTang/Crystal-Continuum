using Server.MirDatabase;

namespace Server.Database;

public partial class MonsterInfoFormNew
{
    private sealed record PendingRespawn(MonsterInfo Monster, MapInfo Map, RespawnInfo Spawn);
    private readonly List<PendingRespawn> pendingRespawns = new();
    private MonsterInfo displayedRespawnMonster;
    private readonly DataGridView respawnGrid = new()
    {
        Name = "respawnGrid", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, RowHeadersVisible = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
    };
    private readonly Label respawnHint = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Button addRespawn = new() { Name = "addRespawn", AutoSize = true };
    private readonly Button discardRespawn = new() { AutoSize = true };

    private void InitializeRespawns()
    {
        var split = new SplitContainer
        {
            Name = "monsterRespawnSplit", Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
            Size = panel2.ClientSize, SplitterWidth = LogicalToDeviceUnits(7),
            SplitterDistance = panel2.ClientSize.Height * 2 / 3,
            Panel1MinSize = LogicalToDeviceUnits(90), Panel2MinSize = LogicalToDeviceUnits(100)
        };
        split.Panel1.Controls.Add(monsterInfoGridView);
        split.Panel1.Controls.Add(nameFeedback);
        nameFeedback.SendToBack();
        split.Panel1.Controls.Add(CreateMonsterPreview());
        var details = new GroupBox { Text = GameLanguage.Get("Respawn points"), Dock = DockStyle.Fill };
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        addRespawn.Text = GameLanguage.Get("Add respawn point");
        discardRespawn.Text = GameLanguage.Get("Discard selected pending point");
        buttons.Controls.AddRange(new Control[] { addRespawn, discardRespawn, respawnHint });
        details.Controls.Add(respawnGrid);
        details.Controls.Add(buttons);
        split.Panel2.Controls.Add(details);
        panel2.Controls.Add(split);
        MinimumSize = SizeFromClientSize(new Size(LogicalToDeviceUnits(560),
            panel1.Height + split.Panel1MinSize + split.SplitterWidth + split.Panel2MinSize));
        foreach (var title in new[]
        {
            GameLanguage.Get("Map index"), GameLanguage.Get("Map name"), GameLanguage.Get("X"),
            GameLanguage.Get("Y"), GameLanguage.Get("Spread"), GameLanguage.Get("Count"),
            GameLanguage.Get("Delay (minutes)"), GameLanguage.Get("Random delay (minutes)"), GameLanguage.Get("Direction (0-7)"),
            GameLanguage.Get("Route path"), GameLanguage.Get("Respawn ticks"), GameLanguage.Get("Save respawn time"),
            GameLanguage.Get("Pending")
        }) respawnGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = title });
        monsterInfoGridView.CurrentCellChanged += (_, _) => RefreshRespawns(false);
        monsterInfoGridView.SelectionChanged += (_, _) => RefreshRespawns(false);
        respawnGrid.SelectionChanged += (_, _) => discardRespawn.Enabled = respawnGrid.SelectedRows.Count == 1 && respawnGrid.SelectedRows[0].Tag is PendingRespawn;
        discardRespawn.Click += (_, _) =>
        {
            if (respawnGrid.CurrentRow?.Tag is not PendingRespawn pending) return;
            pendingRespawns.Remove(pending);
            RefreshRespawns();
        };
        addRespawn.Click += (_, _) =>
        {
            if (!ValidateChildren() || !monsterInfoGridView.EndEdit() || SelectedRespawnMonster() is not { } monster) return;
            using var dialog = new MonsterRespawnDialog(Envir.MapInfoList, monster.Index);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            pendingRespawns.Add(new(monster, dialog.SelectedMap, dialog.Respawn));
            RefreshRespawns();
        };
        RefreshRespawns();
        UpdateMonsterPreview();
    }

    private MonsterInfo SelectedRespawnMonster()
    {
        var cell = monsterInfoGridView.CurrentCell;
        if (cell?.Selected != true || cell.OwningRow.IsNewRow || cell.OwningRow.Cells["MonsterIndex"].Value is not int id) return null;
        return Envir.MonsterInfoList.FirstOrDefault(x => x.Index == id);
    }

    private void RefreshRespawns(bool force = true)
    {
        var monster = SelectedRespawnMonster();
        if (!force && displayedRespawnMonster == monster) return;
        displayedRespawnMonster = monster;
        respawnGrid.Rows.Clear();
        discardRespawn.Enabled = false;
        addRespawn.Enabled = monster != null && Envir.MapInfoList.Count != 0;
        if (monster == null)
        {
            respawnHint.Text = GameLanguage.Get("Select a monster with an index to view its respawn points.");
            return;
        }
        foreach (var map in Envir.MapInfoList)
            foreach (var spawn in map.Respawns)
                if (spawn.MonsterIndex == monster.Index) AddRow(map, spawn, null);
        foreach (var pending in pendingRespawns)
            if (pending.Monster == monster) AddRow(pending.Map, pending.Spawn, pending);

        respawnHint.Text = respawnGrid.Rows.Count == 0 ? GameLanguage.Get("This monster has no respawn points.") : string.Empty;

        void AddRow(MapInfo map, RespawnInfo spawn, PendingRespawn pending)
        {
            int row = respawnGrid.Rows.Add(map.Index, map.Title, spawn.Location.X, spawn.Location.Y, spawn.Spread, spawn.Count,
                spawn.Delay, spawn.RandomDelay, spawn.Direction, spawn.RoutePath, spawn.RespawnTicks, spawn.SaveRespawnTime ? GameLanguage.Get("Yes") : GameLanguage.Get("No"),
                pending != null ? GameLanguage.Get("Yes") : GameLanguage.Get("No"));
            respawnGrid.Rows[row].Tag = pending;
        }
        discardRespawn.Enabled = respawnGrid.CurrentRow?.Tag is PendingRespawn;
    }

    // Keep drafts detached until the same save boundary as monster edits and reference rewrites.
    private void SaveWithRespawns(Action save)
    {
        if (pendingRespawns.Count == 0) { save(); return; }
        lock (Settings.MonsterNameSyncRoot)
        {
            int originalIndex = Envir.RespawnIndex;
            int next = Math.Max(originalIndex, Envir.MapInfoList.SelectMany(x => x.Respawns).Select(x => x.RespawnIndex).DefaultIfEmpty().Max());
            var applied = new List<PendingRespawn>();
            try
            {
                foreach (var pending in pendingRespawns)
                {
                    if (!Envir.MapInfoList.Contains(pending.Map) || !Envir.MonsterInfoList.Contains(pending.Monster) ||
                        pending.Monster.Index != pending.Spawn.MonsterIndex)
                        throw new IOException(GameLanguage.Get("The respawn map or monster has changed. Discard the pending point and select it again."));
                    pending.Spawn.RespawnIndex = checked(++next);
                    pending.Map.Respawns.Add(pending.Spawn);
                    applied.Add(pending);
                }
                Envir.RespawnIndex = next;
                save();
            }
            catch
            {
                foreach (var pending in applied)
                {
                    pending.Map.Respawns.Remove(pending.Spawn);
                    pending.Spawn.RespawnIndex = 0;
                }
                Envir.RespawnIndex = originalIndex;
                throw;
            }
            pendingRespawns.Clear();
        }
    }
}
