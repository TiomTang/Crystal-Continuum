using Server.MirDatabase;
using Server.MirForms.DropBuilder;

namespace Server.Database;

public partial class MonsterInfoFormNew
{
    private readonly TabControl dropItemTabs = new() { Dock = DockStyle.Fill, Multiline = true, SizeMode = TabSizeMode.FillToRight };
    private readonly ListBox[] dropItemLists = new ListBox[DropFileSchema.Tags.Length];
    private readonly TextBox dropTextBox = new()
    {
        Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, WordWrap = false,
        ScrollBars = ScrollBars.Both, BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.Cornsilk
    };
    private readonly Label dropMonsterLabel = new() { AutoSize = true, Margin = new Padding(0, 6, 12, 0) };
    private readonly Label dropFilePathLabel = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(0, 4, 12, 0) };
    private readonly Label dropHint = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(0, 4, 0, 0) };
    private readonly Button dropEditButton = new() { AutoSize = true, Margin = new Padding(0, 2, 6, 0) };
    private readonly Button dropDiscardButton = new() { AutoSize = true, Visible = false, Margin = new Padding(0, 2, 6, 0) };
    private readonly Button dropAddButton = new() { AutoSize = true };
    private readonly Button dropGoldUpdate = new() { AutoSize = true };
    private readonly TextBox dropItemOdds = new();
    private readonly TextBox dropGoldOdds = new();
    private readonly TextBox dropGoldAmount = new();
    private readonly TextBox dropMinLevel = new();
    private readonly TextBox dropMaxLevel = new();
    private readonly CheckBox dropQuestOnly = new() { AutoSize = true };
    private readonly CheckBox dropCapLevel = new() { AutoSize = true };

    private DropFileDocument dropDocument;
    private string dropDocumentPath;
    private MonsterInfo dropMonster;
    private bool dropLoading;

    private void InitializeDrops()
    {
        var page = new TabPage(GameLanguage.Get("Drops")) { Name = "dropsPage", Padding = new Padding(3) };

        dropMonsterLabel.Text = GameLanguage.Get("Select a monster with an index to edit its drop file.");
        dropEditButton.Text = GameLanguage.Get("Edit Drop File");
        dropDiscardButton.Text = GameLanguage.Get("Discard edit");
        dropAddButton.Text = GameLanguage.Get("Add Item");
        dropGoldUpdate.Text = GameLanguage.Get("Update");
        dropQuestOnly.Text = GameLanguage.Get("Quest Only");
        dropCapLevel.Text = GameLanguage.Get("Set cap to mobs level");
        dropItemOdds.Width = LogicalToDeviceUnits(60);
        dropGoldOdds.Width = LogicalToDeviceUnits(60);
        dropGoldAmount.Width = LogicalToDeviceUnits(80);
        dropMinLevel.Width = LogicalToDeviceUnits(50);
        dropMaxLevel.Width = LogicalToDeviceUnits(50);

        var body = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
            Size = panel2.ClientSize,
            SplitterWidth = LogicalToDeviceUnits(7),
            Panel1MinSize = LogicalToDeviceUnits(300), Panel2MinSize = LogicalToDeviceUnits(240)
        };

        var goldRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, WrapContents = false,
            Padding = new Padding(0, 4, 0, 2)
        };
        goldRow.Controls.Add(DropLabel("Gold Settings"));
        goldRow.Controls.Add(DropLabel("Odds: 1 /"));
        goldRow.Controls.Add(dropGoldOdds);
        goldRow.Controls.Add(DropLabel("Amount:"));
        goldRow.Controls.Add(dropGoldAmount);
        goldRow.Controls.Add(dropGoldUpdate);

        var itemRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, WrapContents = false,
            Padding = new Padding(0, 2, 0, 4)
        };
        itemRow.Controls.Add(DropLabel("Item Settings"));
        itemRow.Controls.Add(DropLabel("Odds: 1 /"));
        itemRow.Controls.Add(dropItemOdds);
        itemRow.Controls.Add(dropQuestOnly);
        itemRow.Controls.Add(dropCapLevel);
        itemRow.Controls.Add(DropLabel("Level range:"));
        itemRow.Controls.Add(dropMinLevel);
        itemRow.Controls.Add(new Label { Text = "-", AutoSize = true, Margin = new Padding(4, 8, 2, 0) });
        itemRow.Controls.Add(dropMaxLevel);
        itemRow.Controls.Add(dropAddButton);

        for (int index = 0; index < DropFileSchema.Tags.Length; index++)
        {
            string tag = DropFileSchema.Tags[index];
            var list = new ListBox { Dock = DockStyle.Fill, Tag = tag, HorizontalScrollbar = true };
            list.DoubleClick += (sender, _) => AddDropItem((ListBox)sender);
            dropItemLists[index] = list;

            var itemPage = new TabPage(GameLanguage.Get(tag)) { Padding = new Padding(2) };
            itemPage.Controls.Add(list);
            dropItemTabs.TabPages.Add(itemPage);
        }
        dropItemTabs.SelectedIndexChanged += (_, _) => RefreshDropItems();

        var titleRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        titleRow.Controls.Add(dropMonsterLabel);
        titleRow.Controls.Add(dropEditButton);
        titleRow.Controls.Add(dropDiscardButton);
        var pathRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0) };
        pathRow.Controls.Add(dropFilePathLabel);
        pathRow.Controls.Add(dropHint);
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, WrapContents = false,
            FlowDirection = FlowDirection.TopDown
        };
        header.Controls.Add(titleRow);
        header.Controls.Add(pathRow);

        body.Panel2.Controls.Add(dropTextBox);
        body.Panel2.Controls.Add(itemRow);
        body.Panel2.Controls.Add(goldRow);
        body.Panel2.Controls.Add(header);
        body.Panel1.Controls.Add(dropItemTabs);
        page.Controls.Add(body);
        monsterTabs.TabPages.Add(page);

        dropEditButton.Click += (_, _) => ToggleDropEdit();
        dropDiscardButton.Click += (_, _) => DiscardDropText();
        dropAddButton.Click += (_, _) =>
        {
            int index = dropItemTabs.SelectedIndex;
            if (index >= 0) AddDropItem(dropItemLists[index]);
        };
        dropGoldUpdate.Click += (_, _) => UpdateGold();
        dropGoldOdds.TextChanged += DropGoldTextChanged;
        dropGoldAmount.TextChanged += DropGoldTextChanged;
        dropMinLevel.TextChanged += (_, _) => RefreshDropItems();
        dropMaxLevel.TextChanged += (_, _) => RefreshDropItems();
        dropCapLevel.CheckedChanged += DropLevelCapChanged;
        monsterInfoGridView.CurrentCellChanged += (_, _) => RefreshDrops();
        monsterInfoGridView.SelectionChanged += (_, _) => RefreshDrops();

        SetDropEditMode(false);
        RefreshDrops(true);
    }

    private Label DropLabel(string text)
    {
        return new Label { Text = GameLanguage.Get(text), AutoSize = true, Margin = new Padding(8, 8, 2, 0) };
    }

    // The drop file follows the saved monster: an unsaved rename in the grid must not move the target.
    private static string DropFilePath(MonsterInfo monster)
    {
        string name = string.IsNullOrEmpty(monster.DropPath) ? monster.Name : monster.DropPath;
        return Path.Combine(Settings.DropPath, name + ".txt");
    }

    private void RefreshDrops(bool force = false)
    {
        if (dropLoading) return;
        if (!force && !dropTextBox.ReadOnly) return;

        dropLoading = true;
        try
        {
            var monster = SelectedMonster();
            if (!force && ReferenceEquals(monster, dropMonster) && dropDocument != null) return;

            dropMonster = monster;
            SetDropEditMode(false);
            dropHint.Text = string.Empty;
            dropMinLevel.Text = string.Empty;
            dropMaxLevel.Text = string.Empty;
            dropCapLevel.Checked = false;

            if (monster == null)
            {
                dropDocument = null;
                dropDocumentPath = null;
                dropMonsterLabel.Text = GameLanguage.Get("Select a monster with an index to edit its drop file.");
                dropFilePathLabel.Text = string.Empty;
                dropTextBox.Text = string.Empty;
                UpdateGoldFields();
                ClearDropItems();
                return;
            }

            dropMonsterLabel.Text = GameLanguage.Get("Currently Editing:") + $" {monster.Name} - {GameLanguage.Get("Level")} {monster.Level}";
            LoadDropDocument();
            RefreshDropItems();
        }
        finally
        {
            dropLoading = false;
        }
    }

    private void LoadDropDocument()
    {
        dropDocumentPath = DropFilePath(dropMonster);
        dropDocument = DropFileDocument.Load(dropDocumentPath);
        dropFilePathLabel.Text = dropDocumentPath;
        dropTextBox.Text = dropDocument.Render();
        UpdateGoldFields();
        UpdateHiddenSections();
        SetDropEditMode(false);
    }

    private void UpdateHiddenSections()
    {
        int hiddenSections = 0;
        if (dropDocument != null)
        {
            foreach (var section in dropDocument.Sections)
            {
                if (section.IsGold || DropFileSchema.IndexForHeader(section.Header) >= 0) continue;
                if (section.Lines.Any(line => DropFileSchema.TryParseLine(line.Text, out _, out _, out _))) hiddenSections++;
            }
        }

        dropHint.Text = hiddenSections > 0
            ? GameLanguage.Get("Sections not shown here, kept as-is: {0}", hiddenSections)
            : string.Empty;
    }

    private void RefreshDropItems()
    {
        ClearDropItems();

        if (dropMonster == null || dropItemTabs.SelectedTab == null) return;

        if (!TryDropLevelRange(out int minLevel, out int maxLevel))
        {
            MessageBox.Show(this, GameLanguage.Get("Unreadable level filters."));
            return;
        }

        int index = dropItemTabs.SelectedIndex;
        var list = dropItemLists[index];
        string tag = DropFileSchema.Tags[index];

        list.BeginUpdate();
        foreach (var item in Envir.ItemInfoList)
        {
            if (item.Type.ToString() != tag) continue;
            if (item.RequiredAmount < minLevel || item.RequiredAmount > maxLevel) continue;

            list.Items.Add(item.Name);
        }
        list.EndUpdate();
    }

    private void ClearDropItems()
    {
        foreach (var list in dropItemLists)
        {
            list?.Items.Clear();
        }
    }

    private bool TryDropLevelRange(out int minLevel, out int maxLevel)
    {
        minLevel = 0;
        maxLevel = int.MaxValue;

        string min = dropMinLevel.Text.Trim();
        string max = dropMaxLevel.Text.Trim();

        if (min.Length > 0 && !int.TryParse(min, out minLevel)) return false;
        if (max.Length > 0 && !int.TryParse(max, out maxLevel)) return false;

        return true;
    }

    private void DropLevelCapChanged(object sender, EventArgs e)
    {
        if (dropCapLevel.Checked)
        {
            dropMinLevel.Text = "0";
            dropMaxLevel.Text = dropMonster?.Level.ToString() ?? string.Empty;
        }
        else
        {
            dropMinLevel.Text = string.Empty;
            dropMaxLevel.Text = string.Empty;
        }

        RefreshDropItems();
    }

    private void UpdateGoldFields()
    {
        var gold = dropDocument?.GetGold();
        dropGoldOdds.Text = gold?.Odds ?? string.Empty;
        dropGoldAmount.Text = gold?.Amount ?? "0";
        UpdateGoldButton();
    }

    private void DropGoldTextChanged(object sender, EventArgs e)
    {
        if (dropLoading) return;
        UpdateGoldButton();
    }

    private void UpdateGoldButton()
    {
        if (dropDocument == null)
        {
            dropGoldUpdate.Enabled = false;
            return;
        }

        var gold = dropDocument.GetGold();

        dropGoldUpdate.Enabled = !dropTextBox.ReadOnly &&
            (!string.Equals(dropGoldOdds.Text.Trim(), gold?.Odds ?? string.Empty) ||
             !string.Equals(dropGoldAmount.Text.Trim(), gold?.Amount ?? "0"));
    }

    // Gold edits and added items are written straight to the drop file, like the standalone builder.
    private void UpdateGold()
    {
        if (dropDocument == null) return;

        string odds = dropGoldOdds.Text.Trim();
        string amount = dropGoldAmount.Text.Trim();

        if (amount == "0")
        {
            dropDocument.RemoveGold();
            SaveDropDocument();
            return;
        }

        if (!int.TryParse(odds, out int chance) || chance < 1 ||
            !uint.TryParse(amount, out uint gold) || gold == 0)
        {
            MessageBox.Show(this, GameLanguage.Get("Gold rate and amount must be positive numbers."));
            return;
        }

        dropDocument.SetGold(odds, amount);
        SaveDropDocument();
    }

    private void AddDropItem(ListBox list)
    {
        int index = Array.IndexOf(dropItemLists, list);
        if (index < 0 || list.SelectedItem == null || dropDocument == null) return;

        if (!int.TryParse(dropItemOdds.Text.Trim(), out int chance) || chance < 1)
        {
            MessageBox.Show(this, GameLanguage.Get("Drop rate must be a positive whole number."));
            return;
        }

        string odds = $"1/{chance}";
        string name = list.SelectedItem.ToString();
        string quest = dropQuestOnly.Checked ? "Q" : string.Empty;
        string line = string.IsNullOrEmpty(quest) ? $"{odds} {name}" : $"{odds} {name} {quest}";

        dropDocument.AppendItem(DropFileSchema.Headers[index], DropFileSchema.Aliases[index], line);
        SaveDropDocument();
    }

    private void SaveDropDocument()
    {
        if (dropDocument == null || string.IsNullOrEmpty(dropDocumentPath)) return;

        try
        {
            dropDocument.Save(dropDocumentPath);
        }
        catch (Exception error)
        {
            // Keep the editor in step with the file that is actually on disk.
            dropDocument = DropFileDocument.Load(dropDocumentPath);
            MessageBox.Show(this, error.Message, GameLanguage.Get("Failed to save the drop file"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        dropTextBox.Text = dropDocument.Render();
        UpdateGoldFields();
        UpdateHiddenSections();
    }

    private void ToggleDropEdit()
    {
        if (dropMonster == null) return;

        if (dropTextBox.ReadOnly)
        {
            SetDropEditMode(true);
            dropTextBox.Focus();
            return;
        }

        if (!CommitDropText()) return;

        LoadDropDocument();
        RefreshDropItems();
    }

    private void DiscardDropText()
    {
        SetDropEditMode(false);
        LoadDropDocument();
        RefreshDropItems();
    }

    // Writes the text editor content back to the drop file. Used by Accept, saving and closing.
    private bool CommitDropText()
    {
        if (dropDocument == null || string.IsNullOrEmpty(dropDocumentPath)) return true;

        var document = DropFileDocument.FromText(dropTextBox.Text, dropDocumentPath, dropDocument.TextEncoding, dropDocument.HasBom);

        try
        {
            document.Save(dropDocumentPath);
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, GameLanguage.Get("Failed to save the drop file"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        dropDocument = document;
        return true;
    }

    private void SetDropEditMode(bool edit)
    {
        bool editable = dropMonster != null && !edit;

        dropTextBox.ReadOnly = !edit;
        dropTextBox.BackColor = edit ? Color.Honeydew : Color.Cornsilk;
        dropEditButton.Text = edit ? GameLanguage.Get("Accept") : GameLanguage.Get("Edit Drop File");
        dropEditButton.Enabled = dropMonster != null;
        dropDiscardButton.Visible = edit;
        dropDiscardButton.Enabled = dropMonster != null;
        dropItemTabs.Enabled = editable;
        dropAddButton.Enabled = editable;
        dropItemOdds.Enabled = editable;
        dropGoldOdds.Enabled = editable;
        dropGoldAmount.Enabled = editable;
        dropMinLevel.Enabled = editable;
        dropMaxLevel.Enabled = editable;
        dropQuestOnly.Enabled = editable;
        dropCapLevel.Enabled = editable;
        monsterInfoGridView.Enabled = !edit;
        UpdateGoldButton();
    }
}
