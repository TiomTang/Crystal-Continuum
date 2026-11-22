namespace Launcher;

// Progress window shown by the external update helper. A launcher update has to run from
// a copy outside the installation, so the main window closes first; without this window
// the backup/copy phase of a large map update looks like the launcher crashed.
internal static class ApplyProgress
{
    public static void Run(string root, int parentId)
    {
        LoadLanguage(root);
        using var form = new ApplyForm();
        Exception failure = null;
        var worker = new Thread(() =>
        {
            try { DeferredUpdate.Apply(root, parentId, restart: false, form.Report); }
            catch (Exception ex) { failure = ex; }
            finally { form.Finish(); }
        }) { IsBackground = true, Name = "apply-update" };
        form.Shown += (_, _) => worker.Start();
        Application.Run(form);
        if (failure != null) throw new Exception(failure.Message, failure);
        // A session shutdown also closes the form; never start a new launcher then.
        if (!form.Completed) return;
        // The window is gone before the new launcher opens, so the player never sees two.
        DeferredUpdate.Restart(root);
    }
    // The helper copy carries the launcher resources but not the operator or
    // preference files of the installation, so they are read from the target.
    // Selection order matches the normal launcher: [Launcher] Language, legacy
    // [Game] Language, then the player preference; otherwise the UI culture.
    static void LoadLanguage(string root)
    {
        try
        {
            string folder = Path.GetFullPath(root);
            IniFile published = IniFile.Read(Path.Combine(folder, "Launcher.ini"));
            IniFile personal = IniFile.Read(Path.Combine(folder, Settings.PreferencesName));
            LauncherLanguage.Load(
                published.GetString("Launcher", "Language"),
                published.GetString("Game", "Language"),
                personal.GetString("Game", "Language"));
        }
        catch { }
    }
}

internal sealed class ApplyForm : Form
{
    private readonly Label status = new();
    private readonly Label file = new();
    private readonly ProgressBar bar = new();
    private readonly Label hint = new();
    private bool finished;

    public bool Completed => finished;

    public ApplyForm()
    {
        Text = LauncherStrings.CrystalLauncher;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; MinimizeBox = false; ControlBox = false;
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.Gainsboro;
        ClientSize = new Size(430, 120);

        status.SetBounds(16, 14, 398, 20);
        status.Font = new Font(Font, FontStyle.Bold);
        file.SetBounds(16, 40, 398, 18);
        file.AutoEllipsis = true;
        file.ForeColor = Color.Silver;
        bar.SetBounds(16, 64, 398, 18);
        bar.Maximum = 1000; bar.Style = ProgressBarStyle.Continuous;
        hint.SetBounds(16, 92, 398, 16);
        hint.ForeColor = Color.Gray;
        Controls.Add(status); Controls.Add(file); Controls.Add(bar); Controls.Add(hint);
        status.Text = LauncherStrings.WaitingLauncherClose;
        hint.Text = LauncherStrings.DoNotCloseWindow;
    }

    public void Report(ApplyPhase phase, int done, int total, string name) => Post(() =>
    {
        status.Text = phase switch
        {
            ApplyPhase.Applying => LauncherLanguage.Format(LauncherStrings.ApplyingUpdateFiles, done, total),
            ApplyPhase.Verifying => LauncherStrings.VerifyingUpdate,
            ApplyPhase.Restarting => LauncherStrings.RestartingLauncher,
            _ => LauncherStrings.WaitingLauncherClose,
        };
        file.Text = name ?? "";
        int value = phase is ApplyPhase.Verifying or ApplyPhase.Restarting ? bar.Maximum
            : total <= 0 ? 0 : (int)Math.Clamp((long)done * bar.Maximum / total, 0, bar.Maximum);
        if (value > bar.Value) bar.Value = value;
    });

    public void Finish()
    {
        if (!IsHandleCreated || IsDisposed) return;
        try { BeginInvoke(new Action(() => { finished = true; Close(); })); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!finished && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        base.OnFormClosing(e);
    }

    private void Post(Action action)
    {
        if (finished || IsDisposed) return;
        try { BeginInvoke(action); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }
}
