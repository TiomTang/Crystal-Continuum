namespace Server.Database;

public partial class MonsterInfoFormNew
{
    private readonly PictureBox monsterPreview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };

    private Control CreateMonsterPreview()
    {
        var group = new GroupBox { Text = GameLanguage.Get("Monster image"), Dock = DockStyle.Right, Width = LogicalToDeviceUnits(160) };
        group.Controls.Add(monsterPreview);
        monsterInfoGridView.CurrentCellChanged += (_, _) => UpdateMonsterPreview();
        monsterInfoGridView.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && monsterInfoGridView.Columns[e.ColumnIndex].Name == "MonsterImage") UpdateMonsterPreview();
        };
        FormClosed += (_, _) =>
        {
            monsterPreview.Image = null;
            MonsterPreviewImage.ClearCache();
        };
        return group;
    }

    // The client configured in VersionPath owns the image; without a usable client nothing is shown.
    private void UpdateMonsterPreview()
    {
        var value = monsterInfoGridView.CurrentRow?.Cells["MonsterImage"].Value;
        monsterPreview.Image = value == null || value == DBNull.Value
            ? null
            : MonsterPreviewImage.Get((Monster)Convert.ToUInt16(value));
    }
}
