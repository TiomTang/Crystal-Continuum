using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Mir2.Launcher.Runtime;

// Everything the runtime prerequisite needs, independent of the caller's command line.
// The setup gate parses its own switches into this shape; the portable entry fills it
// from its diagnostics options. Defaults are the shipping Microsoft source.
internal sealed class RuntimeEnsureOptions
{
    public string InstallerPath;
    public string Url = RuntimeDefaults.RuntimeUrl;
    public string Sha256;
    public string DotnetRoot;
    public string WorkDirectory;
    public bool VerifyOnly;
    public bool SkipSignature;
    public bool ForceMissing;
    public bool InheritToken;
}

// Probes, downloads, verifies and installs the .NET 8 Desktop Runtime. This is the one
// implementation shared by first-run setup and the portable entry, so both reach the same
// trust decision: Microsoft-signed bytes only, never executed otherwise.
internal static class RuntimeInstaller
{
    const int InstallTimeoutMilliseconds = 30 * 60 * 1000;

    public static int Ensure(RuntimeEnsureOptions options, Action<string, int?> report)
    {
        report?.Invoke(RuntimeText.Text("Checking the .NET 8 Desktop Runtime", "正在检查 .NET 8 桌面运行时"), null);
        var installed = RuntimeProbe.InstalledVersions(options.DotnetRoot, options.ForceMissing);
        if (installed.Count > 0)
        {
            GateLog.Write("ready: " + RuntimeDefaults.RuntimeProduct + " " + string.Join(", ", installed) + " is already installed");
            return RuntimeExitCodes.Ready;
        }
        GateLog.Write("missing: " + RuntimeDefaults.RuntimeProduct);

        string installer = options.InstallerPath;
        bool downloaded = false;
        if (string.IsNullOrEmpty(installer))
        {
            string target = Path.Combine(GateLog.WorkDirectory, "cache", "windowsdesktop-runtime-8-x64.exe");
            DownloadResult download = Downloader.Fetch(options.Url, target, options.Sha256,
                (percent, bytes) => report?.Invoke(DescribeDownload(percent, bytes), percent));
            if (!download.Ok)
            {
                GateLog.Write("download failed: " + download.Detail);
                return RuntimeExitCodes.DownloadFailed;
            }
            GateLog.Write("downloaded: " + download.Path + " (" + download.Bytes + " bytes)");
            installer = download.Path;
            downloaded = true;
        }

        if (!File.Exists(installer))
        {
            GateLog.Write("installer not found: " + installer);
            return RuntimeExitCodes.DownloadFailed;
        }

        if (options.SkipSignature)
        {
            GateLog.Write("signature: check skipped by test hooks build");
        }
        else
        {
            report?.Invoke(RuntimeText.Text("Verifying the installer signature", "正在验证安装包签名"), null);
            SignatureResult signature = AuthenticodeVerifier.Verify(installer, RuntimeDefaults.SignerSubject);
            GateLog.Write("signature: " + signature);
            if (!signature.Ok)
            {
                Discard(downloaded, installer);
                return RuntimeExitCodes.UntrustedInstaller;
            }
        }

        if (options.VerifyOnly)
        {
            GateLog.Write("verify-only: installer accepted, execution skipped");
            return RuntimeExitCodes.VerifiedNotInstalled;
        }

        report?.Invoke(RuntimeText.Text("Installing the .NET 8 Desktop Runtime (this can take a few minutes)",
            "正在安装 .NET 8 桌面运行时（可能需要几分钟）"), null);
        string installLog = Path.Combine(GateLog.WorkDirectory, "logs", "dotnet-runtime-install.log");
        LaunchResult launch = ElevationRunner.Run(installer,
            "/install /quiet /norestart /log \"" + installLog + "\"",
            Path.GetDirectoryName(installer), InstallTimeoutMilliseconds, options.InheritToken);
        if (!launch.Started)
        {
            GateLog.Write("installer did not start: " + launch.Detail);
            return launch.ElevationCancelled ? RuntimeExitCodes.Cancelled : RuntimeExitCodes.InstallFailed;
        }
        GateLog.Write("installer exit code " + launch.ExitCode + " (installer log " + installLog + ")");

        switch (launch.ExitCode)
        {
            case 0:
            case 1638: // a newer 8.0.x build is already installed: the requirement is met
                return WaitForRuntime(options) ? RuntimeExitCodes.Ready : RuntimeExitCodes.NotInstalled;
            case 3010: // success, reboot required
            case 1641: // success, reboot already started
                GateLog.Write("reboot required before the runtime is fully usable");
                Discard(downloaded, installer);
                return RuntimeExitCodes.RebootRequired;
            case 1602: // user cancelled the installer UI
            case 1223: // consent prompt refused
                GateLog.Write("cancelled by the user");
                return RuntimeExitCodes.Cancelled;
            case 1618: // another MSI style installation holds the mutex
                GateLog.Write("another installation is already in progress");
                return RuntimeExitCodes.InstallInProgress;
            default:
                GateLog.Write("installer reported failure " + launch.ExitCode + "; see " + installLog);
                return RuntimeExitCodes.InstallFailed;
        }
    }

    // Registration and file layout can lag behind the installer's exit code, so confirm the
    // requirement is really satisfied before a caller reports success.
    static bool WaitForRuntime(RuntimeEnsureOptions options)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            var versions = RuntimeProbe.InstalledVersions(options.DotnetRoot, false);
            if (versions.Count > 0)
            {
                GateLog.Write("confirmed: " + RuntimeDefaults.RuntimeProduct + " " + string.Join(", ", versions));
                return true;
            }
            Thread.Sleep(2000);
        }
        GateLog.Write("installer reported success but no Microsoft.WindowsDesktop.App 8.x was found");
        return false;
    }

    // The caller cannot repaint while it waits (setup blocks its wizard thread), so the
    // prerequisite brings its own window. Only Ensure runs here: check and verify finish
    // in milliseconds and never need progress.
    public static int RunWithProgress(string title, string preparing, RuntimeEnsureOptions options)
    {
        Application.EnableVisualStyles();
        using var window = new ProgressWindow(title, preparing);
        int code = RuntimeExitCodes.Unexpected;
        var worker = new Thread(() =>
        {
            try
            {
                code = Ensure(options, window.Report);
            }
            catch (Exception ex)
            {
                GateLog.Write("unexpected: " + ex);
                code = RuntimeExitCodes.Unexpected;
            }
            finally
            {
                window.Finish();
            }
        })
        {
            IsBackground = true,
            Name = "runtime-ensure"
        };
        // Start only after the window exists, so early progress reports are not dropped.
        window.Shown += (sender, args) => worker.Start();
        Application.Run(window);
        worker.Join(5000);
        return code;
    }

    static void Discard(bool downloaded, string installer)
    {
        if (!downloaded) return;
        try
        {
            File.Delete(installer);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    static string DescribeDownload(int? percent, long bytes)
    {
        string size = (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
        return percent.HasValue
            ? RuntimeText.Text("Downloading the .NET 8 Desktop Runtime (" + percent + "%)", "正在下载 .NET 8 桌面运行时（" + percent + "%）")
            : RuntimeText.Text("Downloading the .NET 8 Desktop Runtime (" + size + ")", "正在下载 .NET 8 桌面运行时（" + size + "）");
    }
}
