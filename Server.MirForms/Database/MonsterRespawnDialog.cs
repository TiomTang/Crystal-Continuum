using Server.MirDatabase;

namespace Server.Database;

internal sealed class MonsterRespawnDialog : Form
{
    private readonly IReadOnlyList<MapInfo> maps;
    private readonly int monsterIndex;
    private readonly ComboBox map = new() { Name = "map", DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    private readonly TextBox x, y, spread, count, delay, randomDelay, direction, ticks, route;
    private readonly CheckBox saveTime = new() { AutoSize = true, Name = "saveTime" };
    private readonly Label feedback = new() { AutoSize = true, MaximumSize = new Size(360, 0) };
    private readonly TableLayoutPanel fields = new() { AutoSize = true, ColumnCount = 2, Padding = new Padding(8), Dock = DockStyle.Fill };
    private MonsterRespawnAdvice advice;

    internal MapInfo SelectedMap { get; private set; }
    internal RespawnInfo Respawn { get; private set; }

    internal MonsterRespawnDialog(IReadOnlyList<MapInfo> maps, int monsterIndex)
    {
        this.maps = maps;
        this.monsterIndex = monsterIndex;
        Text = GameLanguage.Get("Add respawn point");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Controls.Add(fields);
        AddField(GameLanguage.Get("Map"), map);
        x = Input("x", GameLanguage.Get("X"));
        y = Input("y", GameLanguage.Get("Y"));
        spread = Input("spread", GameLanguage.Get("Spread"));
        count = Input("count", GameLanguage.Get("Count"));
        delay = Input("delay", GameLanguage.Get("Delay (minutes)"));
        randomDelay = Input("randomDelay", GameLanguage.Get("Random delay (minutes)"));
        direction = Input("direction", GameLanguage.Get("Direction (0-7)"), "0");
        route = Input("route", GameLanguage.Get("Route path"));
        ticks = Input("ticks", GameLanguage.Get("Respawn ticks"), "0");
        saveTime.Text = GameLanguage.Get("Save respawn time");
        AddWide(saveTime);
        AddWide(feedback);
        var buttons = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right };
        var ok = new Button { AutoSize = true, Text = GameLanguage.Get("OK") };
        var cancel = new Button { AutoSize = true, Text = GameLanguage.Get("Cancel"), DialogResult = DialogResult.Cancel };
        buttons.Controls.AddRange(new Control[] { ok, cancel });
        AddWide(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
        ok.Click += (_, _) => Submit();
        map.SelectedIndexChanged += (_, _) => UpdateAdvice();
        foreach (var item in maps) map.Items.Add(item);
        if (map.Items.Count != 0) map.SelectedIndex = 0;
        else UpdateAdvice();
    }

    private void AddField(string caption, Control control)
    {
        int row = fields.RowCount++;
        fields.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        control.AccessibleName = caption;
        control.Dock = DockStyle.Fill;
        fields.Controls.Add(control, 1, row);
    }

    private TextBox Input(string name, string caption, string value = "")
    {
        var input = new TextBox { Name = name, Text = value };
        AddField(caption, input);
        return input;
    }

    private void AddWide(Control control)
    {
        fields.Controls.Add(control, 0, fields.RowCount++);
        fields.SetColumnSpan(control, 2);
    }

    private void UpdateAdvice()
    {
        advice = null;
        x.Clear();
        y.Clear();
        feedback.Text = string.Empty;
        if (map.SelectedItem is not MapInfo selected)
        {
            feedback.Text = GameLanguage.Get("Select a map.");
            return;
        }
        advice = MonsterRespawnAdvice.Create(selected, maps, monsterIndex);
        spread.Text = advice.Spread.ToString();
        count.Text = advice.Count.ToString();
        delay.Text = advice.Delay.ToString();
        randomDelay.Text = advice.RandomDelay.ToString();
        if (advice.Location is Point point)
        {
            x.Text = point.X.ToString();
            y.Text = point.Y.ToString();
        }
        else feedback.Text = GameLanguage.Get("No spawn location could be recommended for this map.");
    }

    private void Reject(Control input, string message)
    {
        feedback.Text = message;
        input.Focus();
    }

    private bool Number(TextBox input, int min, int max, out int value)
    {
        if (int.TryParse(input.Text, out value) && value >= min && value <= max) return true;
        Reject(input, GameLanguage.Get("{0}: enter a whole number from {1} to {2}.", input.AccessibleName, min, max));
        return false;
    }

    private void Submit()
    {
        if (map.SelectedItem is not MapInfo selected || !maps.Contains(selected))
        {
            Reject(map, GameLanguage.Get("Select a map."));
            return;
        }
        if (!Number(x, 0, int.MaxValue, out int px) || !Number(y, 0, int.MaxValue, out int py)) return;
        if (advice?.IsSpawnable(px, py) != true)
        {
            Reject(x, GameLanguage.Get("Select a spawnable tile inside the map."));
            return;
        }
        if (!Number(count, 1, ushort.MaxValue, out int qty) || !Number(spread, 0, ushort.MaxValue, out int radius) ||
            !Number(delay, 0, ushort.MaxValue, out int minutes) || !Number(randomDelay, 0, ushort.MaxValue, out int variation) ||
            !Number(direction, 0, 7, out int facing) || !Number(ticks, 0, ushort.MaxValue, out int interval)) return;
        if (saveTime.Checked && interval == 0)
        {
            Reject(ticks, GameLanguage.Get("Saving respawn time requires a positive respawn tick interval."));
            return;
        }
        var path = route.Text.Trim();
        if (path.Length != 0 && path.Split('/', '\\').Any(part => string.IsNullOrWhiteSpace(part) || part.EndsWith('.') || part.EndsWith(' ') || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            Reject(route, GameLanguage.Get("Enter a relative route path inside the routes folder."));
            return;
        }
        Respawn = new RespawnInfo
        {
            MonsterIndex = monsterIndex, Location = new Point(px, py), Count = (ushort)qty, Spread = (ushort)radius,
            Delay = (ushort)minutes, RandomDelay = (ushort)variation, Direction = (byte)facing, RoutePath = path,
            RespawnTicks = (ushort)interval, SaveRespawnTime = saveTime.Checked
        };
        SelectedMap = selected;
        DialogResult = DialogResult.OK;
    }
}
