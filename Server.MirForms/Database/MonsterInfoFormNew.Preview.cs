namespace Server.Database;

public partial class MonsterInfoFormNew
{
    private readonly PictureBox monsterPreview = new() { SizeMode = PictureBoxSizeMode.Normal, BackColor = Color.Black };

    private Control CreateMonsterPreview()
    {
        var group = new GroupBox { Text = GameLanguage.Get("Monster image"), Dock = DockStyle.Right, Width = LogicalToDeviceUnits(200) };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(monsterPreview);
        group.Controls.Add(scroll);
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
        var image = value == null || value == DBNull.Value
            ? null
            : MonsterPreviewImage.Get((Monster)Convert.ToUInt16(value));

        // 1:1 pixels: scaling a low resolution sprite up only blurs it.
        monsterPreview.Image = image;
        monsterPreview.Size = image?.Size ?? Size.Empty;
    }
}
