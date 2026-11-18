namespace Server.Database;

public partial class ItemInfoFormNew
{
    // Icons are authored for their own pixel size, so they are shown unscaled and centred.
    private readonly PictureBox itemPreview = new() { Dock = DockStyle.Fill };
    private Bitmap itemPreviewImage;

    private Control CreateItemPreview()
    {
        var group = new GroupBox { Text = GameLanguage.Get("Item image"), Dock = DockStyle.Right, Width = LogicalToDeviceUnits(160) };
        group.Controls.Add(itemPreview);
        itemPreview.Resize += (_, _) => ShowItemPreview();
        itemInfoGridView.CurrentCellChanged += (_, _) => UpdateItemPreview();
        itemInfoGridView.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && itemInfoGridView.Columns[e.ColumnIndex].Name == "ItemImage") UpdateItemPreview();
        };
        FormClosed += (_, _) =>
        {
            itemPreview.Image = null;
            itemPreviewImage = null;
            ItemPreviewImage.ClearCache();
        };
        UpdateItemPreview();
        return group;
    }

    // The client configured in VersionPath owns the image; without a usable client nothing is shown.
    private void UpdateItemPreview()
    {
        var value = itemInfoGridView.CurrentRow?.Cells["ItemImage"].Value;
        itemPreviewImage = value == null || value == DBNull.Value
            ? null
            : ItemPreviewImage.Get(Convert.ToUInt16(value));
        ShowItemPreview();
    }

    // Icons keep their own pixel size; only those wider than the pane are scaled down to fit.
    private void ShowItemPreview()
    {
        itemPreview.Image = itemPreviewImage;

        var size = itemPreview.ClientSize;
        itemPreview.SizeMode = itemPreviewImage != null &&
                               (itemPreviewImage.Width > size.Width || itemPreviewImage.Height > size.Height)
            ? PictureBoxSizeMode.Zoom
            : PictureBoxSizeMode.CenterImage;
    }
}
