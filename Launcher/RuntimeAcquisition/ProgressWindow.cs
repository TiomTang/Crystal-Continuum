using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mir2.Launcher.Runtime;

// Setup cannot repaint its wizard while it waits for this gate, so a download that takes
// minutes would look like a hang behind a frozen window. The gate shows this window
// instead and keeps the wizard hidden behind it. The window has no close button: closing
// it mid-install would leave an unfinished prerequisite behind.
internal sealed class ProgressWindow : Form
{
    readonly Label statusLabel;
    readonly ProgressBar progressBar;

    public ProgressWindow(string title, string status)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(470, 112);
        Font = new Font("Segoe UI", 9F);

        statusLabel = new Label
        {
            AutoSize = false,
            Left = 16,
            Top = 16,
            Width = 438,
            Height = 46,
            Text = status
        };
        progressBar = new ProgressBar
        {
            Left = 16,
            Top = 68,
            Width = 438,
            Height = 22,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };
        Controls.Add(statusLabel);
        Controls.Add(progressBar);
    }

    public void Report(string status, int? percent)
    {
        if (IsDisposed) return;
        try
        {
            BeginInvoke(new Action(() => Apply(status, percent)));
        }
        catch (InvalidOperationException)
        {
            // The window is closing; the final result travels through the exit code.
        }
    }

    public void Finish()
    {
        if (IsDisposed) return;
        try
        {
            BeginInvoke(new Action(Close));
        }
        catch (InvalidOperationException) { }
    }

    void Apply(string status, int? percent)
    {
        if (IsDisposed) return;
        if (!string.IsNullOrEmpty(status)) statusLabel.Text = status;
        if (!percent.HasValue) return;
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = Math.Max(0, Math.Min(100, percent.Value));
    }
}
