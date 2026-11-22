using System;
using Mir2.Launcher.Runtime;
using ExitCode = Mir2.Launcher.Runtime.RuntimeExitCodes;

namespace Mir2.Installer.RuntimeGate;

// Command line front end for the shared runtime acquisition sources in
// Launcher/RuntimeAcquisition. Setup runs it before the launcher is usable, so the gate
// itself may not depend on .NET 8 (see RuntimeGate.csproj).
internal static class Program
{
    public const string Version = RuntimeDefaults.Version;

    [STAThread]
    static int Main(string[] args)
    {
        GateOptions options;
        try
        {
            options = Arguments.Parse(args);
        }
        catch (UsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Arguments.Usage);
            return ExitCode.Usage;
        }

        if (options.Mode == GateMode.Help)
        {
            Console.WriteLine(Arguments.Usage);
            return ExitCode.Ready;
        }

        GateLog.Open(options.LogPath, options.WorkDirectory, options.Quiet);
        try
        {
            GateLog.Write("mode=" + options.Mode + " url=" + options.Url +
                (string.IsNullOrEmpty(options.InstallerPath) ? string.Empty : " installer=" + options.InstallerPath) +
                (string.IsNullOrEmpty(options.DotnetRoot) ? string.Empty : " dotnetRoot=" + options.DotnetRoot) +
                (options.VerifyOnly ? " verify-only=true" : string.Empty));
            if (!Environment.Is64BitProcess)
            {
                GateLog.Write("unsupported: the gate must run as a 64-bit process");
                return ExitCode.Unsupported;
            }
            RuntimeEnsureOptions ensure = ToEnsureOptions(options);
            int code = options.Mode != GateMode.Ensure
                ? Execute(options)
                : options.Progress
                    ? RuntimeInstaller.RunWithProgress(
                        RuntimeText.Text("Mir2 setup", "Mir2 安装程序"),
                        RuntimeText.Text("Preparing the .NET 8 Desktop Runtime", "正在准备 .NET 8 桌面运行时"),
                        ensure)
                    : RuntimeInstaller.Ensure(ensure, null);
            GateLog.Write("result=" + code + " (" + ExitCode.Describe(code) + ")");
            return code;
        }
        catch (Exception ex)
        {
            GateLog.Write("unexpected: " + ex);
            return ExitCode.Unexpected;
        }
        finally
        {
            GateLog.Close();
        }
    }

    static int Execute(GateOptions options)
    {
        return options.Mode == GateMode.Check ? Check(options) : Verify(options);
    }

    static RuntimeEnsureOptions ToEnsureOptions(GateOptions options)
    {
        return new RuntimeEnsureOptions
        {
            InstallerPath = options.InstallerPath,
            Url = options.Url,
            Sha256 = options.Sha256,
            DotnetRoot = options.DotnetRoot,
            WorkDirectory = GateLog.WorkDirectory,
            VerifyOnly = options.VerifyOnly,
            SkipSignature = options.SkipSignature,
            ForceMissing = options.ForceMissing,
            InheritToken = options.InheritToken
        };
    }

    static int Check(GateOptions options)
    {
        var versions = RuntimeProbe.InstalledVersions(options.DotnetRoot, options.ForceMissing);
        if (versions.Count == 0)
        {
            GateLog.Write("missing: " + RuntimeDefaults.RuntimeProduct);
            return ExitCode.NotInstalled;
        }
        GateLog.Write("present: " + RuntimeDefaults.RuntimeProduct + " " + string.Join(", ", versions));
        return ExitCode.Ready;
    }

    static int Verify(GateOptions options)
    {
        if (options.SkipSignature)
        {
            GateLog.Write("verify: signature check skipped by test hooks build");
            return ExitCode.Ready;
        }
        SignatureResult signature = AuthenticodeVerifier.Verify(options.InstallerPath, RuntimeDefaults.SignerSubject);
        GateLog.Write("verify: " + signature);
        return signature.Ok ? ExitCode.Ready : ExitCode.UntrustedInstaller;
    }
}
