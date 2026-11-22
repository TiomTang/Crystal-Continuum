namespace Launcher;
public partial class Config : Form
{
    private bool syncing;
    private CheckBox testConfig;
    private Label clientServer;
    private Label launcherServer;
    private Label note;
    private Label status;
    private GameConfig game;
    private bool gameLoaded;
    private int width, height;
    private ClientDisplayMode mode;
    private bool fpsCap, topmost;

    public Config() { InitializeComponent(); }

    private void Config_Load(object sender, EventArgs e)
    {
        // The old account fields are gone: credentials exist only in the game,
        // and the launcher shows the connection instead of editing it.
        var server = new Panel { Location = new Point(16, 150), Size = new Size(225, 150), BackColor = Color.FromArgb(7, 10, 13) };
        server.Controls.Add(new Label { Text = LauncherStrings.Server, ForeColor = Color.White, Location = new Point(7, 4), AutoSize = true });
        clientServer = new Label { ForeColor = Color.Gray, Location = new Point(7, 28), Size = new Size(210, 16), AutoEllipsis = true };
        launcherServer = new Label { ForeColor = Color.Gray, Location = new Point(7, 48), Size = new Size(210, 16), AutoEllipsis = true };
        note = new Label { ForeColor = Color.Gainsboro, Location = new Point(7, 68), Size = new Size(210, 40), Font = new Font("Calibri", 7.5F) };
        testConfig = new CheckBox { Text = LauncherStrings.TestConfiguration, ForeColor = Color.Gray, Location = new Point(7, 112), AutoSize = true };
        testConfig.CheckedChanged += TestConfig_CheckedChanged;
        server.Controls.Add(clientServer); server.Controls.Add(launcherServer); server.Controls.Add(note); server.Controls.Add(testConfig);
        Controls.Add(server); server.BringToFront();

        status = new Label { ForeColor = Color.Salmon, Location = new Point(16, 305), Size = new Size(225, 44), Font = new Font("Calibri", 7.5F) };
        Controls.Add(status); status.BringToFront();

        Resolution_cb.Items.Clear();
        Resolution_cb.Items.AddRange(ResolutionOptions.All.Select(x => (object)x.Label).ToArray());
        DisplayMode_cb.Items.AddRange(new object[] { LauncherStrings.Fullscreen, LauncherStrings.BorderlessWindow, LauncherStrings.Windowed });
        LoadGame();
    }

    private void LoadGame()
    {
        game = null; gameLoaded = false; status.Text = "";
        try
        {
            game = GameConfig.Load(Settings.ClientConfigPath);
            gameLoaded = true;
        }
        catch (Exception ex)
        {
            // A file that exists but cannot be read must never be treated as an
            // empty configuration and overwritten later.
            status.Text = LauncherLanguage.Format(LauncherStrings.CannotReadGameSettings, ex.Message);
        }

        syncing = true;
        if (gameLoaded)
        {
            width = game.Width; height = game.Height; mode = game.Mode; fpsCap = game.FpsCap; topmost = game.Topmost;
            Resolution_cb.SelectedIndex = ResolutionOptions.IndexOf(width, height);
            DisplayMode_cb.SelectedIndex = mode == ClientDisplayMode.FullScreen ? 0 : mode == ClientDisplayMode.Maximized ? 1 : 2;
            if (!game.Exists) status.Text = LauncherStrings.NoGameSettings;
        }
        testConfig.Checked = Settings.UseTestConfig;
        SetEditable(gameLoaded);
        syncing = false;
        RefreshChecks();
        RefreshServer();
    }

    private void SetEditable(bool enabled)
    {
        Resolution_cb.Enabled = DisplayMode_cb.Enabled = FPScap_pb.Enabled = OnTop_pb.Enabled = enabled;
        FPScap_label.Enabled = OnTop_label.Enabled = enabled;
    }

    private void RefreshServer()
    {
        bool configured = gameLoaded && game.UseConfig;
        string clientHost = configured ? game.Host : "127.0.0.1";
        int clientPort = configured ? game.Port : 7000;
        bool overridden = Settings.OverrideHost != null || Settings.OverridePort != null;
        string effectiveHost = Settings.OverrideHost ?? clientHost;
        int effectivePort = Settings.OverridePort ?? clientPort;

        clientServer.Text = LauncherStrings.Client + ": " + clientHost + ":" + clientPort;
        launcherServer.Text = LauncherStrings.Launcher + ": " + (overridden ? effectiveHost + ":" + effectivePort : LauncherStrings.SameAsClient);
        note.Text = LauncherStrings.ServerNote;
        note.Visible = overridden;
    }

    private void Config_VisibleChanged(object sender, EventArgs e)
    {
        if (testConfig == null) return;
        if (!Visible)
        {
            Settings.UseTestConfig = testConfig.Checked;
            SaveGame();
            Settings.Save();
            return;
        }
        LoadGame();
    }

    private void TestConfig_CheckedChanged(object sender, EventArgs e)
    {
        if (syncing) return;
        // Save pending edits into the file that is leaving the view before the
        // switch, so the two configurations cannot leak into each other.
        if (gameLoaded && testConfig.Checked != Settings.UseTestConfig && !SaveGame())
        {
            syncing = true; testConfig.Checked = Settings.UseTestConfig; syncing = false;
            return;
        }
        Settings.UseTestConfig = testConfig.Checked;
        LoadGame();
    }

    private bool SaveGame()
    {
        if (!gameLoaded || game == null) return false;
        try
        {
            game.Save(width, height, mode, fpsCap, topmost);
            status.Text = "";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(LauncherLanguage.Format(LauncherStrings.CouldNotSaveGameSettings, ex.Message), LauncherStrings.GameSettings);
            return false;
        }
    }

    private void RefreshChecks()
    {
        FPScap_pb.Image = fpsCap ? Resources.Images.Config_Check_On : Resources.Images.Config_Check_Off1;
        OnTop_pb.Image = topmost ? Resources.Images.Config_Check_On : Resources.Images.Config_Check_Off1;
        AutoStart_pb.Image = Settings.AutoStart ? Resources.Images.Config_Check_On : Resources.Images.Config_Check_Off1;
    }

    private void Resolution_cb_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (syncing || Resolution_cb.SelectedIndex < 0) return;
        var option = ResolutionOptions.All[Resolution_cb.SelectedIndex];
        width = option.Width; height = option.Height;
    }

    private void DisplayMode_cb_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (syncing || DisplayMode_cb.SelectedIndex < 0) return;
        mode = DisplayMode_cb.SelectedIndex == 0 ? ClientDisplayMode.FullScreen : DisplayMode_cb.SelectedIndex == 1 ? ClientDisplayMode.Maximized : ClientDisplayMode.Windowed;
    }

    private void FPScap_pb_Click(object sender, EventArgs e) { if (!gameLoaded) return; fpsCap = !fpsCap; RefreshChecks(); }
    private void OnTop_pb_Click(object sender, EventArgs e) { if (!gameLoaded) return; topmost = !topmost; RefreshChecks(); }
    private void AutoStart_pb_Click(object sender, EventArgs e) { Settings.AutoStart = !Settings.AutoStart; RefreshChecks(); }
    private void Config_Click(object sender, EventArgs e) { ActiveControl = label4; }
}
