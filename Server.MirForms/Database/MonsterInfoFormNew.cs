using System.Data;
using System.Text.RegularExpressions;
using Server.MirDatabase;
using Server.MirEnvir;
using Shared.Helpers;

namespace Server.Database
{
    public partial class MonsterInfoFormNew : Form
    {
        public Envir Envir => SMain.EditEnvir;

        private readonly Array StatEnums = Enum.GetValues(typeof(Stat));

        private DataTable Table;
        private List<(MonsterInfo Monster, object[] Values)> savedMonsterState;
        private bool unsavedChanges;

        public MonsterInfoFormNew()
        {
            InitializeComponent();
            txtSearch.PlaceholderText = GameLanguage.Get("Search...");

            SetDoubleBuffered(monsterInfoGridView);

            InitializeItemInfoGridView();
            InitializeNameValidation();
            CreateDynamicColumns();
            UiText.Apply(this);
            InitializeRespawns();
            InitializeDrops();

            PopulateTable();
            savedMonsterState = CaptureMonsterState();

            rbtnViewBasic.Checked = true;
            UpdateMonsterPreview();
        }

        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }

        private void InitializeItemInfoGridView()
        {
            Modified.ValueType = typeof(bool);
            MonsterIndex.ValueType = typeof(int);
            MonsterName.ValueType = typeof(string);
            MonsterAI.ValueType = typeof(byte);
            MonsterEffect.ValueType = typeof(byte);
            MonsterLevel.ValueType = typeof(ushort);
            MonsterLight.ValueType = typeof(byte);
            MonsterAttackSpeed.ValueType = typeof(ushort);
            MonsterMoveSpeed.ValueType = typeof(ushort);
            MonsterViewRange.ValueType = typeof(byte);
            MonsterCoolEye.ValueType = typeof(byte);
            MonsterExperience.ValueType = typeof(uint);
            MonsterCanPush.ValueType = typeof(bool);
            MonsterAutoRev.ValueType = typeof(bool);
            MonsterUndead.ValueType = typeof(bool);
            MonsterCanTame.ValueType = typeof(bool);
            MonsterIsBoss.ValueType = typeof(bool);
            MonsterRecall.ValueType = typeof(bool);
            MonsterDropPath.ValueType = typeof(string);

            //Basic
            this.MonsterImage.ValueType = typeof(Monster);
            this.MonsterImage.DataSource = Enum2DataTable<Monster>(true);
            this.MonsterImage.ValueMember = "Value";
            this.MonsterImage.DisplayMember = "Display";
        }

        public static DataTable Enum2DataTable<T>(bool sort = false)
        {
            DataTable enumTable = new DataTable();
            enumTable.Columns.Add(new DataColumn("Value", Enum.GetUnderlyingType(typeof(T))));
            enumTable.Columns.Add(new DataColumn("Display", typeof(string)));
            DataRow EnumRow;

            var values = Enum.GetValues(typeof(T)).Cast<T>();

            if (sort)
            {
                values = values.OrderBy(x => x.ToString());
            }

            foreach (T e in values)
            {
                EnumRow = enumTable.NewRow();
                EnumRow["Value"] = e;
                EnumRow["Display"] = e.ToString();
                enumTable.Rows.Add(EnumRow);
            }

            return enumTable;
        }

        private void CreateDynamicColumns()
        {
            foreach (Stat stat in StatEnums)
            {
                if (stat == Stat.Unknown) continue;

                var key = stat.ToString();
                var strKey = RegexFunctions.SeperateCamelCase(key.Replace("Rate", "").Replace("Multiplier", "").Replace("Percent", ""));

                var sign = "";

                if (key.Contains("Percent"))
                    sign = "%";
                else if (key.Contains("Multiplier"))
                    sign = "x";

                var col = new DataGridViewTextBoxColumn
                {
                    HeaderText = $"{strKey} {sign}",
                    Name = "Stat" + stat.ToString(),
                    ValueType = typeof(int),
                    DataPropertyName = "Stat" + stat.ToString()
                };

                monsterInfoGridView.Columns.Add(col);
            }

        }

        private void PopulateTable()
        {
            Table = new DataTable("monsterInfo");

            foreach (DataGridViewColumn col in monsterInfoGridView.Columns)
            {
                Table.Columns.Add(col.DataPropertyName, col.ValueType);
            }

            foreach (MirDatabase.MonsterInfo item in Envir.MonsterInfoList)
            {
                DataRow row = Table.NewRow();

                row["Modified"] = false;

                row["MonsterIndex"] = item.Index;
                row["MonsterName"] = item.Name;

                row["MonsterImage"] = item.Image;
                row["MonsterAI"] = item.AI;
                row["MonsterLevel"] = item.Level;
                row["MonsterEffect"] = item.Effect;
                row["MonsterLight"] = item.Light;
                row["MonsterAttackSpeed"] = item.AttackSpeed;
                row["MonsterMoveSpeed"] = item.MoveSpeed;
                row["MonsterViewRange"] = item.ViewRange;
                row["MonsterCoolEye"] = item.CoolEye;
                row["MonsterExperience"] = item.Experience;
                row["MonsterCanPush"] = item.CanPush;
                row["MonsterCanTame"] = item.CanTame;
                row["MonsterIsBoss"] = item.IsBoss;
                row["MonsterUndead"] = item.Undead;
                row["MonsterAutoRev"] = item.AutoRev;
                row["MonsterRecall"] = item.CanRecall;
                row["MonsterDropPath"] = item.DropPath;

                foreach (Stat stat in StatEnums)
                {
                    if (stat == Stat.Unknown) continue;

                    row["Stat" + stat.ToString()] = item.Stats[stat];
                }

                Table.Rows.Add(row);
            }

            monsterInfoGridView.DataSource = Table;
        }

        private void UpdateFilter()
        {
            var filterText = txtSearch.Text;

            if (string.IsNullOrEmpty(filterText))
            {
                (monsterInfoGridView.DataSource as DataTable).DefaultView.RowFilter = "";
                return;
            }

            string rowFilter = string.Format("[MonsterName] LIKE '%{0}%'", filterText);

            (monsterInfoGridView.DataSource as DataTable).DefaultView.RowFilter = rowFilter;
        }

        private void SaveForm()
        {
            int lastIndex = 0;
            if (Envir.MonsterInfoList.Count > 0)
            {
                lastIndex = Envir.MonsterInfoList.Max(x => x.Index);
            }

            foreach (DataGridViewRow row in monsterInfoGridView.Rows)
            {
                var name = row.Cells["MonsterName"].Value;

                if (name == null || name.GetType() == typeof(System.DBNull) || string.IsNullOrWhiteSpace((string)name))
                {
                    continue;
                }

                MonsterInfo monster;

                if (string.IsNullOrEmpty((string)row.Cells["MonsterIndex"].FormattedValue))
                {
                    Envir.MonsterInfoList.Add(monster = new MonsterInfo());

                    monster.Index = ++lastIndex;
                }
                else
                {
                    int index = (int)row.Cells["MonsterIndex"].Value;

                    monster = Envir.MonsterInfoList.FirstOrDefault(x => x.Index == index);

                    if (row.Cells["Modified"].Value != null && (bool)row.Cells["Modified"].Value == false) continue;
                }

                monster.Name = (string)row.Cells["MonsterName"].Value;
                monster.Image = (Monster)row.Cells["MonsterImage"].Value;
                monster.AI = (byte)row.Cells["MonsterAI"].Value;
                monster.Level = (ushort)row.Cells["MonsterLevel"].Value;
                monster.Effect = (byte)row.Cells["MonsterEffect"].Value;
                monster.Light = (byte)row.Cells["MonsterLight"].Value;
                monster.AttackSpeed = (ushort)row.Cells["MonsterAttackSpeed"].Value;
                monster.MoveSpeed = (ushort)row.Cells["MonsterMoveSpeed"].Value;
                monster.ViewRange = (byte)row.Cells["MonsterViewRange"].Value;
                monster.CoolEye = (byte)row.Cells["MonsterCoolEye"].Value;
                monster.Experience = (uint)row.Cells["MonsterExperience"].Value;
                monster.CanPush = (bool)row.Cells["MonsterCanPush"].Value;
                monster.CanTame = (bool)row.Cells["MonsterCanTame"].Value;
                monster.IsBoss = row.Cells["MonsterIsBoss"].Value != null && (bool)row.Cells["MonsterIsBoss"].Value;
                monster.Undead = (bool)row.Cells["MonsterUndead"].Value;
                monster.AutoRev = (bool)row.Cells["MonsterAutoRev"].Value;
                monster.CanRecall = row.Cells["MonsterRecall"].Value != null && (bool)row.Cells["MonsterRecall"].Value;
                monster.DropPath = row.Cells["MonsterDropPath"].Value?.ToString() ?? string.Empty;

                monster.Stats.Clear();

                foreach (DataGridViewColumn col in monsterInfoGridView.Columns)
                {
                    if (col.Name.StartsWith("Stat"))
                    {
                        var stat = col.Name.Substring(4);

                        Stat enumStat = (Stat)Enum.Parse(typeof(Stat), stat);

                        monster.Stats[enumStat] = (int)row.Cells[col.Name].Value;
                    }
                }
            }

        }

        private DataRow FindRowByMonsterName(string value)
        {
            foreach (DataRow row in Table.Rows)
            {
                var val = row["MonsterName"];

                if (val?.ToString().Equals(value) ?? false)
                {
                    return row;
                }
            }

            return null;
        }
        private DataRow FindRowByMonsterIndex(string index)
        {
            foreach (DataRow row in Table.Rows)
            {
                var val = row["MonsterIndex"];

                if (val?.ToString().Equals(index) ?? false)
                {
                    return row;
                }
            }

            return null;
        }
        private void monsterInfoGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var col = monsterInfoGridView.Columns[e.ColumnIndex];

            var cell = monsterInfoGridView.Rows[e.RowIndex].Cells[col.Name];

            if (col.Name == nameof(MonsterName) && monsterInfoGridView.IsCurrentCellInEditMode)
            {
                var error = GetNameError(cell.OwningRow, e.FormattedValue?.ToString() ?? string.Empty);
                ShowNameError(error);
                if (error != null)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (cell.FormattedValue != null && e.FormattedValue != null && cell.FormattedValue.ToString() == e.FormattedValue.ToString())
            {
                return;
            }

            monsterInfoGridView.Rows[e.RowIndex].Cells["Modified"].Value = true;
            MarkDirty();

            var val = e.FormattedValue.ToString();

            monsterInfoGridView.Rows[e.RowIndex].ErrorText = "";

            if (col.ValueType == typeof(int) && int.TryParse(val, out int val1) && val1 < 0)
            {
                e.Cancel = true;
                monsterInfoGridView.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be a positive integer");
            }

            if (col.ValueType == typeof(int) && !int.TryParse(val, out _))
            {
                e.Cancel = true;
                monsterInfoGridView.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be an integer");
            }
            else if (col.ValueType == typeof(byte) && !byte.TryParse(val, out _))
            {
                e.Cancel = true;
                monsterInfoGridView.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be a byte");
            }
            else if (col.ValueType == typeof(short) && !short.TryParse(val, out _))
            {
                e.Cancel = true;
                monsterInfoGridView.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be a short");
            }
            else if (col.ValueType == typeof(ushort) && !ushort.TryParse(val, out _))
            {
                e.Cancel = true;
                monsterInfoGridView.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be a ushort");
            }
            else if (col.ValueType == typeof(long) && !long.TryParse(val, out _))
            {
                e.Cancel = true;
                monsterInfoGridView.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be a long");
            }
        }

        private void rbtnViewAll_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnViewAll.Checked)
            {
                foreach (DataGridViewColumn col in monsterInfoGridView.Columns)
                {
                    if (col.Name == "Modified") continue;

                    col.Visible = true;
                    continue;
                }
            }
        }

        private void rbtnViewBasic_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnViewBasic.Checked)
            {
                foreach (DataGridViewColumn col in monsterInfoGridView.Columns)
                {
                    if (col.Name == "MonsterIndex" || col.Name == "MonsterName" || col.Name == "Modified")
                    {
                        continue;
                    }

                    if (col.Name.StartsWith("Monster"))
                    {
                        col.Visible = true;
                        continue;
                    }

                    if (col.Name.Equals("StatHP") ||
                        col.Name.Equals("StatMinAC") || col.Name.Equals("StatMaxAC") ||
                        col.Name.Equals("StatMinMAC") || col.Name.Equals("StatMaxMAC") ||
                        col.Name.Equals("StatMinDC") || col.Name.Equals("StatMaxDC") ||
                        col.Name.Equals("StatMinMC") || col.Name.Equals("StatMaxMC") ||
                        col.Name.Equals("StatMinSC") || col.Name.Equals("StatMaxSC") ||
                        col.Name.Equals("StatAccuracy") || col.Name.Equals("StatAgility"))
                    {
                        col.Visible = true;
                        continue;
                    }

                    col.Visible = false;
                }
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                UpdateFilter();

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog { Filter = "CSV (*.csv)|*.csv" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                var records = CsvFile.ReadAllRows(dialog.FileName);
                if (!monsterInfoGridView.EndEdit()) throw new FormatException(GameLanguage.Get("Invalid current monster cell."));
                BindingContext[Table].EndCurrentEdit();
                var imported = MonsterCsvImport.Prepare(Table, records, out int count);
                monsterInfoGridView.DataSource = imported;
                Table = imported;
                MarkDirty();
                MessageBox.Show(GameLanguage.Get($"{count} monsters have been imported."));
            }
            catch (Exception ex)
            {
                MessageBox.Show(GameLanguage.Get("Error :") + ex.Message);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (monsterInfoGridView.Rows.Count > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV (*.csv)|*.csv";
                sfd.FileName = "MonsterInfo Output.csv";
                bool fileError = false;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (File.Exists(sfd.FileName))
                    {
                        try
                        {
                            File.Delete(sfd.FileName);
                        }
                        catch (IOException ex)
                        {
                            fileError = true;
                            MessageBox.Show(GameLanguage.Get("It wasn't possible to write the data to the disk.") + ex.Message);
                        }
                    }
                    if (!fileError)
                    {
                        try
                        {
                            int columnCount = monsterInfoGridView.Columns.Count;
                            var columnNames = new string[columnCount];
                            var outputCsv = new List<string[]>(monsterInfoGridView.Rows.Count + 1);
                            for (int i = 1; i < columnCount; i++)
                            {
                                columnNames[i - 1] = monsterInfoGridView.Columns[i].Name;
                            }
                            columnNames[columnCount - 1] = string.Empty;
                            outputCsv.Add(columnNames);

                            for (int i = 1; (i - 1) < monsterInfoGridView.Rows.Count; i++)
                            {
                                if (monsterInfoGridView.Rows[i - 1].IsNewRow) continue;
                                var cells = new string[columnCount];
                                for (int j = 1; j < columnCount; j++)
                                {
                                    var cell = monsterInfoGridView.Rows[i - 1].Cells[j];

                                    var valueType = monsterInfoGridView.Columns[j].ValueType;
                                    if (valueType.IsEnum)
                                    {
                                        cells[j - 1] = Enum.ToObject(valueType, cell.Value ?? 0)?.ToString() ?? "";
                                    }
                                    else
                                    {
                                        cells[j - 1] = cell.Value?.ToString() ?? "";
                                    }
                                }
                                cells[columnCount - 1] = string.Empty;
                                outputCsv.Add(cells);
                            }

                            CsvFile.WriteAllRows(sfd.FileName, outputCsv);
                            MessageBox.Show(GameLanguage.Get("Data Exported Successfully."), GameLanguage.Get("Info"));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(GameLanguage.Get("Error :") + ex.Message);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(GameLanguage.Get("No Monsters To Export."), GameLanguage.Get("Info"));
            }
        }

        private void monsterInfoGridView_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            var row = e.Row;

            row.Cells["Modified"].Value = (bool)true;
            MarkDirty();

            row.Cells["MonsterName"].Value = "";
            row.Cells["MonsterImage"].Value = (Monster)0;
            row.Cells["MonsterAI"].Value = (byte)0;
            row.Cells["MonsterLevel"].Value = (ushort)0;
            row.Cells["MonsterEffect"].Value = (byte)0;
            row.Cells["MonsterLight"].Value = (byte)0;
            row.Cells["MonsterAttackSpeed"].Value = (ushort)2500;
            row.Cells["MonsterMoveSpeed"].Value = (ushort)1800;
            row.Cells["MonsterViewRange"].Value = (byte)7;
            row.Cells["MonsterCoolEye"].Value = (byte)0;
            row.Cells["MonsterExperience"].Value = (uint)0;
            row.Cells["MonsterCanPush"].Value = (bool)true;
            row.Cells["MonsterCanTame"].Value = (bool)true;
            row.Cells["MonsterIsBoss"].Value = (bool)false;
            row.Cells["MonsterUndead"].Value = (bool)false;
            row.Cells["MonsterAutoRev"].Value = (bool)true;
            row.Cells["MonsterRecall"].Value = (bool)false;
            row.Cells["MonsterDropPath"].Value = "";

            foreach (Stat stat in StatEnums)
            {
                if (stat == Stat.Unknown) continue;

                row.Cells["Stat" + stat.ToString()].Value = 0;
            }
        }

        private void monsterInfoGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var row = e.Row;

            if (row.Cells["MonsterIndex"].Value is int index)
            {
                if (pendingRespawns.Any(p => p.Monster.Index == index))
                {
                    e.Cancel = true;
                    row.ErrorText = GameLanguage.Get("Discard this monster's pending respawn points before deleting it.");
                    return;
                }

                var item = Envir.MonsterInfoList.FirstOrDefault(x => x.Index == index);

                Envir.MonsterInfoList.Remove(item);
                MarkDirty();
            }
        }

        private void monsterInfoFormNew_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (monsterInfoGridView.IsCurrentCellInEditMode && nameEditor != null &&
                GetNameError(monsterInfoGridView.CurrentRow, nameEditor.Text) is { } error)
            {
                e.Cancel = true;
                ShowNameError(error);
                nameEditor.Focus();
                return;
            }
            bool editingDrops = dropDocument != null && !dropTextBox.ReadOnly;
            if (!unsavedChanges && !editingDrops) return;

            var answer = MessageBox.Show(this, GameLanguage.Get("There are unsaved changes. Save before closing?"),
                GameLanguage.Get("Info"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (answer == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (answer == DialogResult.No)
            {
                DiscardChanges();
                return;
            }

            if (!TrySave()) e.Cancel = true;
        }

        internal void SaveChanges()
        {
            var filter = Table.DefaultView.RowFilter;
            try
            {
                if (!monsterInfoGridView.EndEdit()) throw new IOException(GameLanguage.Get("Cannot commit the current monster cell. Check the input value."));
                ((BindingManagerBase)BindingContext[Table]).EndCurrentEdit();
                // SaveForm uses the grid: include rows hidden by the search filter.
                Table.DefaultView.RowFilter = string.Empty;
                SaveWithRespawns(SynchronizeMonsterReferences);
            }
            catch
            {
                RestoreMonsterState();
                throw;
            }
            finally
            {
                Table.DefaultView.RowFilter = filter;
            }
            PopulateTable();
            Table.DefaultView.RowFilter = filter;
            RefreshRespawns();
            // A saved rename moves the drop file, so the editor has to follow it.
            RefreshDrops(true);
            savedMonsterState = CaptureMonsterState();
            unsavedChanges = false;
            btnSave.Enabled = false;
        }

        private List<(MonsterInfo Monster, object[] Values)> CaptureMonsterState()
        {
            var fields = typeof(MonsterInfo).GetFields();
            return Envir.MonsterInfoList.Select(monster => (monster, fields.Select(field =>
                field.Name == nameof(MonsterInfo.Stats) ? new Stats(monster.Stats) : field.GetValue(monster)).ToArray())).ToList();
        }

        private void RestoreMonsterState()
        {
            if (savedMonsterState == null) return;
            var fields = typeof(MonsterInfo).GetFields();
            foreach (var (monster, values) in savedMonsterState)
            {
                for (int i = 0; i < fields.Length; i++)
                    fields[i].SetValue(monster, values[i] is Stats stats ? new Stats(stats) : values[i]);
            }
            Envir.MonsterInfoList.Clear();
            foreach (var (monster, _) in savedMonsterState) Envir.MonsterInfoList.Add(monster);
        }

        private void MarkDirty()
        {
            unsavedChanges = true;
            btnSave.Enabled = true;
        }

        private bool TrySave()
        {
            if (dropDocument != null && !dropTextBox.ReadOnly && !CommitDropText()) return false;

            try
            {
                SaveChanges();
                return true;
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.ToString(), GameLanguage.Get("Failed to save monsters"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            TrySave();
        }

        private void DiscardChanges()
        {
            SetDropEditMode(false);
            RestoreMonsterState();
            pendingRespawns.Clear();
            PopulateTable();
            unsavedChanges = false;
            btnSave.Enabled = false;
            RefreshRespawns();
            RefreshDrops(true);
        }

        private void SynchronizeMonsterReferences()
        {
            var changes = new List<(string OldName, string NewName, string OldDropPath, string NewDropPath)>();
            var retainedPaths = new List<string>();
            foreach (DataRow row in Table.Rows)
            {
                var newName = row["MonsterName"].ToString();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    if (int.TryParse(row["MonsterIndex"].ToString(), out var existingIndex) &&
                        Envir.MonsterInfoList.Any(existingMonster => existingMonster.Index == existingIndex))
                        throw new IOException(GameLanguage.Get("Monster name cannot be blank: {0}", existingIndex));
                    continue;
                }
                var newDropPath = row["MonsterDropPath"].ToString();
                retainedPaths.Add(string.IsNullOrEmpty(newDropPath) ? newName : newDropPath);
                if (!int.TryParse(row["MonsterIndex"].ToString(), out var index))
                {
                    changes.Add((null, newName, newDropPath, newDropPath));
                    continue;
                }
                var monster = Envir.MonsterInfoList.FirstOrDefault(x => x.Index == index);
                if (monster == null)
                {
                    changes.Add((null, newName, newDropPath, newDropPath));
                    continue;
                }
                changes.Add((monster.Name, newName, monster.DropPath, newDropPath));
            }
            var references = new MonsterNameSynchronization(changes.Select(x => (x.OldName, x.NewName)));
            references.Execute(Envir, saveDatabase => SynchronizeDropFiles(Settings.DropPath, changes.Where(x => x.OldName != null), retainedPaths, () =>
            {
                SaveForm();
                saveDatabase();
            }));
        }

        internal static void SynchronizeDropFiles(string directory,
            IEnumerable<(string OldName, string NewName, string OldDropPath, string NewDropPath)> changes,
            IEnumerable<string> retainedPaths, Action save = null)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var retained = retainedPaths.GroupBy(path => Path.GetFullPath(Path.Combine(directory, path + ".txt")), comparer)
                .ToDictionary(group => group.Key, group => group.Count(), comparer);
            var renames = new List<(string Source, string Target)>();
            var sources = new HashSet<string>(comparer);
            var targets = new HashSet<string>(comparer);
            foreach (var change in changes)
            {
                // Explicit drop paths are independent of the monster's display name.
                if (!string.IsNullOrEmpty(change.OldDropPath) || !string.IsNullOrEmpty(change.NewDropPath) ||
                    string.Equals(change.OldName, change.NewName, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var name in new[] { change.OldName, change.NewName })
                {
                    if (string.IsNullOrWhiteSpace(name) || name != name.Trim() ||
                        name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        throw new IOException(GameLanguage.Get($"Monster name cannot be used as a drop filename: {name}"));
                }
                var source = Path.GetFullPath(Path.Combine(directory, change.OldName + ".txt"));
                var target = Path.GetFullPath(Path.Combine(directory, change.NewName + ".txt"));
                if (!sources.Add(source) || !targets.Add(target) || Directory.Exists(source) || Directory.Exists(target))
                    throw new IOException(GameLanguage.Get($"Drop file rename conflict. Existing files will not be overwritten: {source} -> {target}"));
                renames.Add((source, target));
            }

            var moves = new List<(string Source, string Target, string Temporary)>();
            var affected = new HashSet<string>(comparer);
            foreach (var rename in renames)
            {
                if (File.Exists(rename.Target) && !sources.Contains(rename.Target))
                    throw new IOException(GameLanguage.Get($"Drop file rename conflict. Existing files will not be overwritten: {rename.Target}"));
                if (!File.Exists(rename.Source)) continue; // Absence follows the monster too.
                foreach (var path in new[] { rename.Source, rename.Target })
                {
                    // The incoming rename owns one final reference; all others are independent readers.
                    if (retained.TryGetValue(path, out int count) && count > (targets.Contains(path) ? 1 : 0))
                        throw new IOException(GameLanguage.Get($"Drop file is still used by another monster. Set an explicit drop path first: {path}"));
                    affected.Add(path);
                }
                moves.Add((rename.Source, rename.Target, Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp")));
            }

            if (moves.Count > 0 && Directory.Exists(directory))
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.txt", SearchOption.AllDirectories))
                foreach (var line in File.ReadLines(file))
                {
                    var match = Regex.Match(line, @"#INSERT \[(.*?)\]", RegexOptions.IgnoreCase);
                    if (!match.Success) continue;
                    var referenced = Path.GetFullPath(Path.Combine(directory, match.Groups[1].Value));
                    if (affected.Contains(referenced))
                        throw new IOException(GameLanguage.Get($"Drop file is referenced by #INSERT and cannot be renamed automatically: {file} -> {referenced}"));
                }
            }

            var completed = new Stack<(string Source, string Target)>();
            try
            {
                foreach (var move in moves)
                {
                    File.Move(move.Source, move.Temporary);
                    completed.Push((move.Source, move.Temporary));
                }
                foreach (var move in moves)
                {
                    File.Move(move.Temporary, move.Target);
                    completed.Push((move.Temporary, move.Target));
                }
                save?.Invoke();
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                while (completed.TryPop(out var move))
                {
                    try { File.Move(move.Target, move.Source); }
                    catch (Exception rollbackError) { errors.Add(rollbackError); }
                }
                throw new AggregateException(GameLanguage.Get("Monster save or drop file rename failed. Drop file rollback was attempted. Check the error details before retrying."), errors);
            }
        }

        private void monsterInfoGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }


    }
}