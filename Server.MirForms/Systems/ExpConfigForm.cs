using Server.MirEnvir;

namespace Server
{
    public partial class ExpConfigForm : Form
    {
        private bool Populating;
        private bool Changed;

        public ExpConfigForm()
        {
            InitializeComponent();
            UiText.Apply(this);

            Populate();
        }

        /// <summary>
        /// Levels above the table length resolve to a maximum experience of zero, so the table length
        /// is the level cap of the server.
        /// </summary>
        private DataGridView ActiveGrid => expTabs.SelectedIndex == 1 ? heroGridView : playerGridView;

        private void Populate()
        {
            Populating = true;

            FillGrid(playerGridView, Settings.ExperienceList);
            FillGrid(heroGridView, Settings.HeroExperienceList);

            Populating = false;

            UpdateLevelCount();
        }

        private static void FillGrid(DataGridView grid, List<long> experienceList)
        {
            grid.Rows.Clear();

            for (var i = 0; i < experienceList.Count; i++)
                grid.Rows.Add(i + 1, experienceList[i]);
        }

        private void UpdateLevelCount()
        {
            lblLevelCount.Text = ActiveGrid.Rows.Count.ToString();
        }

        private void ExpConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!Changed) return;

            SaveGrid(playerGridView, Settings.ExperienceList);
            SaveGrid(heroGridView, Settings.HeroExperienceList);

            Settings.SaveEXP();
            Settings.SaveHeroEXP();
        }

        private static void SaveGrid(DataGridView grid, List<long> experienceList)
        {
            experienceList.Clear();

            foreach (DataGridViewRow row in grid.Rows)
                experienceList.Add(Convert.ToInt64(row.Cells["Experience"].Value ?? 0L));
        }

        private void btnAddLevel_Click(object sender, EventArgs e)
        {
            var grid = ActiveGrid;

            var last = grid.Rows.Count == 0 ? 100L : Convert.ToInt64(grid.Rows[grid.Rows.Count - 1].Cells["Experience"].Value ?? 0L);

            // Keep the last growth step so an extended table stays in the shape the server already uses.
            grid.Rows.Add(grid.Rows.Count + 1, last > long.MaxValue / 2 ? long.MaxValue : last * 2);
            grid.FirstDisplayedScrollingRowIndex = grid.Rows.Count - 1;

            Changed = true;
            UpdateLevelCount();
        }

        private void btnRemoveLevel_Click(object sender, EventArgs e)
        {
            var grid = ActiveGrid;

            if (grid.Rows.Count <= 1) return;

            if (MessageBox.Show(GameLanguage.Get("Are you sure you want to delete the last index?"), GameLanguage.Get("Delete?"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            grid.Rows.RemoveAt(grid.Rows.Count - 1);

            Changed = true;
            UpdateLevelCount();
        }

        private void expTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLevelCount();
        }

        private void ExpGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Populating || e.RowIndex < 0) return;

            Changed = true;
        }

        private void ExpGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var grid = (DataGridView)sender;

            if (e.RowIndex < 0) return;

            grid.Rows[e.RowIndex].ErrorText = "";

            if (grid.Columns[e.ColumnIndex].Name != "Experience") return;

            if (long.TryParse(e.FormattedValue.ToString(), out var value) && value >= 0) return;

            e.Cancel = true;
            grid.Rows[e.RowIndex].ErrorText = GameLanguage.Get("the value must be a whole number of 0 or more");
        }
    }
}
