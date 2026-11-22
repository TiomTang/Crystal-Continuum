using System.Globalization;

namespace Launcher;

/// <summary>
/// Operator configuration (Launcher.ini) and launcher-local player preferences
/// (Launcher.User.ini). Game settings belong to the client configuration and are
/// edited by the settings window, never cached here. Operator values are
/// overrides only: a key the operator did not fill in is not passed on, so the
/// client keeps its own saved value.
/// </summary>
internal static class Settings
{
    public const string PreferencesName = "Launcher.User.ini";
    // Retained for the update helper's dependency list. The launcher strings
    // are compiled resources now, so this folder is no longer part of output.
    public const string LocalizationFolder = "LauncherLocalization";

    private static IniFile preferences;
    private static bool preferencesUsable = true;

    public static bool PatchNeedsLogin;
    public static string PatchLogin = "", PatchPassword = "";
    // Without an operator configuration, never contact a public update site.
    public static string P_Host = "", P_PatchFileName = "PList.gz", P_ServerName = "", P_BrowserAddress = "";

    // Explicit operator game overrides. Null means "not configured"; the
    // launcher then omits the argument entirely.
    public static string OverrideHost;
    public static int? OverridePort;
    public static string OverrideResolution;
    public static ClientDisplayMode? OverrideMode;
    public static bool? OverrideFpsCap, OverrideTopmost;

    public static string P_Client => AppContext.BaseDirectory;
    public static bool AutoStart, UseTestConfig;

    public static string ClientConfigPath => Path.Combine(P_Client, UseTestConfig ? "Mir2Test.ini" : "Mir2Config.ini");

    public static PatchSource Snapshot() => new(P_Host, P_PatchFileName, PatchNeedsLogin, PatchLogin, PatchPassword);
    public static HttpClient HttpClient() => Snapshot().CreateClient();

    /// <summary>Re-reads the operator file. Invalid explicit values fail instead of silently using defaults.</summary>
    public static void ReloadPublished()
    {
        var reader = IniFile.Read(Path.Combine(P_Client, "Launcher.ini"));
        PatchNeedsLogin = reader.GetBool("Launcher", "NeedLogin", false);
        PatchLogin = reader.GetString("Launcher", "Login");
        PatchPassword = reader.GetString("Launcher", "Password");
        P_Host = reader.GetString("Launcher", "Host");
        P_PatchFileName = reader.GetString("Launcher", "PatchFile", "PList.gz");
        P_ServerName = reader.GetString("Launcher", "ServerName");
        P_BrowserAddress = reader.GetString("Launcher", "Browser");

        OverrideHost = Explicit(reader, "Network", "IPAddress");
        if (OverrideHost != null && (Uri.CheckHostName(OverrideHost) == UriHostNameType.Unknown || OverrideHost.Any(char.IsWhiteSpace)))
            throw new InvalidDataException("Launcher.ini Network/IPAddress is not a valid host name.");

        OverridePort = null;
        if (Explicit(reader, "Network", "Port") is string portText)
        {
            if (!int.TryParse(portText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port) || port < 1 || port > 65535)
                throw new InvalidDataException("Launcher.ini Network/Port must be 1..65535.");
            OverridePort = port;
        }

        string width = Explicit(reader, "Graphics", "Resolution");
        string height = Explicit(reader, "Graphics", "ResolutionHeight");
        if ((width == null) != (height == null))
            throw new InvalidDataException("Launcher.ini Graphics/Resolution and ResolutionHeight must be set together.");
        OverrideResolution = null;
        if (width != null)
        {
            if (!int.TryParse(width, NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) || !int.TryParse(height, NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) || w <= 0 || h <= 0)
                throw new InvalidDataException("Launcher.ini Graphics resolution must be a positive width and height.");
            OverrideResolution = w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture);
        }

        OverrideMode = null;
        if (Explicit(reader, "Graphics", "DisplayMode") is string modeText)
        {
            if (!int.TryParse(modeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mode) || mode is < 0 or > 2)
                throw new InvalidDataException("Launcher.ini Graphics/DisplayMode must be 0 (windowed), 1 (maximized) or 2 (fullscreen).");
            OverrideMode = (ClientDisplayMode)mode;
        }

        OverrideFpsCap = ExplicitBoolean(reader, "Graphics", "FPSCap");
        OverrideTopmost = ExplicitBoolean(reader, "Graphics", "AlwaysOnTop");

        if (P_Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) P_Host = "http://" + P_Host;
        if (P_Host.Length > 0 && !P_Host.EndsWith("/", StringComparison.Ordinal)) P_Host += "/";
    }

    /// <summary>
    /// Loads launcher preferences and, on a normal start, migrates legacy
    /// personal graphics keys into the selected client configuration once.
    /// Health probes pass <paramref name="migrate"/> = false so they never write
    /// files while the client configuration may be in use.
    /// </summary>
    public static void Load(bool migrate = false)
    {
        ReloadPublished();

        try
        {
            preferences = IniFile.Read(Path.Combine(P_Client, PreferencesName));
            preferencesUsable = true;
        }
        catch (Exception ex)
        {
            preferences = null;
            preferencesUsable = false;
            Program.Trace("preferences-unreadable type=" + ex.GetType().Name);
        }

        if (preferences != null)
        {
            UseTestConfig = preferences.GetBool("Launcher", "TestConfig", false);
            AutoStart = preferences.GetBool("Launcher", "AutoStart", false);
            if (migrate) MigrateLegacyGraphics(preferences);
        }

        LoadLanguage();
        LaunchArguments.Validate();
    }

    /// <summary>
    /// Selects the launcher UI culture. Called before the installation lease so
    /// that startup failures are already shown in the configured language, and
    /// again on the normal path. Only the operator and preference files are read:
    /// an explicit <c>[Launcher] Language</c> wins, then the legacy
    /// <c>[Game] Language</c> keys, then the UI culture chain, then English.
    /// </summary>
    public static void LoadLanguage()
    {
        IniFile published = null;
        try
        {
            published = IniFile.Read(Path.Combine(P_Client, "Launcher.ini"));
        }
        catch (Exception ex)
        {
            Program.Trace("language-operator-unreadable type=" + ex.GetType().Name);
        }

        IniFile personal = preferences;
        if (personal == null && preferencesUsable)
        {
            try
            {
                personal = IniFile.Read(Path.Combine(P_Client, PreferencesName));
            }
            catch (Exception ex)
            {
                Program.Trace("language-preferences-unreadable type=" + ex.GetType().Name);
            }
        }

        // The launcher owns these resources; the client's Localization folder is
        // never read, so the two artifacts cannot overwrite each other.
        LauncherLanguage.Load(
            published?.GetString("Launcher", "Language"),
            published?.GetString("Game", "Language"),
            personal?.GetString("Game", "Language"));
    }

    public static void Save()
    {
        if (!preferencesUsable || preferences == null)
        {
            Program.Trace("preferences-save-skipped");
            return;
        }
        try
        {
            preferences.Set("Launcher", "AutoStart", AutoStart);
            preferences.Set("Launcher", "TestConfig", UseTestConfig);
            preferences.Save();
        }
        catch (Exception ex)
        {
            // A read-only preference file must not block the game launch; the
            // current session still uses the in-memory values.
            Program.Trace("preferences-save-failed type=" + ex.GetType().Name);
        }
    }

    private static void MigrateLegacyGraphics(IniFile legacy)
    {
        if (!legacy.Exists || legacy.GetBool("Launcher", "Migrated", false)) return;
        try
        {
            var game = GameConfig.Load(ClientConfigPath);
            if (game.AdoptLegacy(legacy)) game.Persist();
            legacy.Set("Launcher", "Migrated", "True");
            // The old graphics keys stay in the preference file for rollback;
            // only the client configuration is authoritative from now on.
            legacy.Save();
        }
        catch (Exception ex)
        {
            // Unreadable client configuration: keep the old file untouched and
            // retry on the next start without marking migration as complete.
            Program.Trace("migration-skipped type=" + ex.GetType().Name);
        }
    }

    private static string Explicit(IniFile reader, string section, string key)
    {
        return reader.TryGet(section, key, out string value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    }

    private static bool? ExplicitBoolean(IniFile reader, string section, string key)
    {
        if (Explicit(reader, section, key) is not string text) return null;
        if (!bool.TryParse(text, out bool value)) throw new InvalidDataException("Launcher.ini " + section + "/" + key + " must be True or False.");
        return value;
    }
}
