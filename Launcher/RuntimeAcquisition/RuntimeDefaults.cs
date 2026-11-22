using System;

namespace Mir2.Launcher.Runtime;

// Shared constants for the runtime prerequisite. They live in Launcher/ so the portable
// entry, the standalone setup gate and any other consumer agree on one download source and
// one trust anchor. Identity comes from the Authenticode check, never from the URL.
internal static class RuntimeDefaults
{
    public const string Version = "1.1.0";

    // Microsoft's redirector for the current .NET 8 Windows Desktop Runtime (x64).
    // The redirect keeps working across servicing releases; operators who must pin an
    // exact build pass --url and --sha256 (setup exposes them as /RUNTIMEURL= and
    // /RUNTIMESHA256=).
    public const string RuntimeUrl = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe";

    // Subject fragment of the certificate that must have signed the installer.
    public const string SignerSubject = "Microsoft Corporation";

    public const string RuntimeProduct = ".NET 8 Desktop Runtime (x64)";
}

// Exit codes are the contract with every consumer (setup script, portable entry, health
// checks); adding or renumbering one means updating the usage text and the setup mapping.
internal static class RuntimeExitCodes
{
    public const int Ready = 0;
    public const int Unexpected = 1;
    public const int Usage = 2;
    public const int DownloadFailed = 3;
    public const int UntrustedInstaller = 4;
    public const int InstallFailed = 5;
    public const int Cancelled = 6;
    public const int RebootRequired = 7;
    public const int InstallInProgress = 8;
    public const int NotInstalled = 9;
    public const int VerifiedNotInstalled = 10;
    public const int Unsupported = 11;

    public static string Describe(int code)
    {
        switch (code)
        {
            case Ready: return "ready";
            case Unexpected: return "unexpected error";
            case Usage: return "usage";
            case DownloadFailed: return "download failed";
            case UntrustedInstaller: return "untrusted installer";
            case InstallFailed: return "install failed";
            case Cancelled: return "cancelled";
            case RebootRequired: return "reboot required";
            case InstallInProgress: return "install already in progress";
            case NotInstalled: return "runtime missing";
            case VerifiedNotInstalled: return "verified, not installed";
            case Unsupported: return "unsupported environment";
            default: return "unknown";
        }
    }
}

internal static class RuntimeText
{
    public static bool Chinese
    {
        get { return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase); }
    }

    public static string Text(string english, string chinese)
    {
        return Chinese ? chinese : english;
    }
}
