using System;
using System.Collections.Generic;
using Mir2.Launcher.Runtime;

namespace Mir2.Installer.RuntimeGate;

internal enum GateMode
{
    Help,
    Check,
    Ensure,
    Verify
}

internal sealed class GateOptions
{
    public GateMode Mode = GateMode.Help;
    public string InstallerPath;
    public string Url = RuntimeDefaults.RuntimeUrl;
    public string Sha256;
    public string DotnetRoot;
    public string LogPath;
    public string WorkDirectory;
    public bool VerifyOnly;
    public bool Quiet;
    public bool Progress;
    public bool SkipSignature = false;
    public bool ForceMissing = false;
    public bool InheritToken = false;
}

internal sealed class UsageException : Exception
{
    public UsageException(string message) : base(message) { }
}

internal static class Arguments
{
    public const string Usage =
        "Mir2 runtime gate\r\n" +
        "\r\n" +
        "  --check                 report whether the .NET 8 Desktop Runtime (x64) is installed\r\n" +
        "  --ensure                install it when missing, then report readiness\r\n" +
        "  --verify --installer P  check only the Authenticode signature of P\r\n" +
        "\r\n" +
        "Options:\r\n" +
        "  --installer <path>      use this installer instead of downloading one\r\n" +
        "  --url <url>             download URL (default: Microsoft aka.ms redirector)\r\n" +
        "  --sha256 <hex>          require this SHA-256 for the downloaded file\r\n" +
        "  --dotnet-root <dir>     probe <dir>\\shared instead of the machine wide install\r\n" +
        "  --verify-only           download and verify, but never execute the installer\r\n" +
        "  --progress              show a progress window (setup passes this)\r\n" +
        "  --log <file>            log file (default: %LOCALAPPDATA%\\Mir2Setup\\logs\\runtime-gate.log)\r\n" +
        "  --workdir <dir>         working directory for logs and the download cache\r\n" +
        "  --quiet                 do not echo to the console\r\n" +
        "\r\n" +
        "Exit codes: 0 ready, 1 unexpected, 2 usage, 3 download failed, 4 untrusted installer,\r\n" +
        "5 install failed, 6 cancelled, 7 reboot required, 8 install already in progress,\r\n" +
        "9 runtime missing, 10 verified but not installed (--verify-only), 11 unsupported.\r\n";

    public static GateOptions Parse(string[] args)
    {
        var options = new GateOptions();
        int index = 0;
        while (index < args.Length)
        {
            string argument = args[index++];
            switch (argument.ToLowerInvariant())
            {
                case "--check": options.Mode = GateMode.Check; break;
                case "--ensure": options.Mode = GateMode.Ensure; break;
                case "--verify": options.Mode = GateMode.Verify; break;
                case "--help":
                case "-h":
                case "/?": options.Mode = GateMode.Help; break;
                case "--installer": options.InstallerPath = Value(args, ref index, argument); break;
                case "--url": options.Url = Value(args, ref index, argument); break;
                case "--sha256": options.Sha256 = Value(args, ref index, argument); break;
                case "--dotnet-root": options.DotnetRoot = Value(args, ref index, argument); break;
                case "--log": options.LogPath = Value(args, ref index, argument); break;
                case "--workdir": options.WorkDirectory = Value(args, ref index, argument); break;
                case "--verify-only": options.VerifyOnly = true; break;
                case "--progress": options.Progress = true; break;
                case "--quiet": options.Quiet = true; break;
#if GATE_TEST_HOOKS
                case "--skip-signature": options.SkipSignature = true; break;
                case "--force-missing": options.ForceMissing = true; break;
                case "--inherit-token": options.InheritToken = true; break;
#endif
                default: throw new UsageException("Unknown argument: " + argument);
            }
        }
        if (options.Mode != GateMode.Help && options.Mode != GateMode.Ensure && options.Mode != GateMode.Check && options.Mode != GateMode.Verify)
            throw new UsageException("No mode selected");
        if (options.Mode == GateMode.Verify && string.IsNullOrEmpty(options.InstallerPath))
            throw new UsageException("--verify requires --installer <path>");
        if (!string.IsNullOrEmpty(options.Sha256)) options.Sha256 = NormalizeSha256(options.Sha256);
        return options;
    }

    static string Value(IReadOnlyList<string> args, ref int index, string option)
    {
        if (index >= args.Count) throw new UsageException(option + " requires a value");
        string value = args[index++];
        if (string.IsNullOrWhiteSpace(value)) throw new UsageException(option + " requires a value");
        return value;
    }

    public static string NormalizeSha256(string value)
    {
        string normalized = value.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
        if (normalized.Length != 64) throw new UsageException("--sha256 must be a 64 character hex digest");
        foreach (char c in normalized)
            if (!Uri.IsHexDigit(c)) throw new UsageException("--sha256 must be a 64 character hex digest");
        return normalized;
    }
}
