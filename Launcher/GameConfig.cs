namespace Launcher;

/// <summary>
/// The client's own configuration file. The launcher displays these values and,
/// when the player saves the settings window, writes back only the keys that
/// actually changed. Values equal to a client default are removed again so the
/// client keeps owning its defaults.
/// </summary>
internal sealed class GameConfig
{
    // Client defaults (Client/Settings.cs). Falling back to them here only
    // describes what the client would do; the launcher does not override them.
    public const int DefaultWidth = 1024, DefaultHeight = 768;
    public const bool DefaultFpsCap = true, DefaultTopmost = true;
    public const ClientDisplayMode DefaultMode = ClientDisplayMode.FullScreen;

    private readonly IniFile file;

    private GameConfig(string path, IniFile file)
    {
        Path = path;
        this.file = file;
    }

    public string Path { get; }
    public bool Exists => file.Exists;

    public int Width { get; private set; } = DefaultWidth;
    public int Height { get; private set; } = DefaultHeight;
    public ClientDisplayMode Mode { get; private set; } = DefaultMode;
    public bool FpsCap { get; private set; } = DefaultFpsCap;
    public bool Topmost { get; private set; } = DefaultTopmost;
    // Client network settings are shown for information only; the settings
    // window never edits them.
    public bool UseConfig { get; private set; }
    public string Host { get; private set; } = "127.0.0.1";
    public int Port { get; private set; } = 7000;

    public static GameConfig Load(string path)
    {
        var file = IniFile.Read(path);
        var config = new GameConfig(file.Path, file);

        int mode = file.GetInt("Graphics", "DisplayMode", -1);
        config.Mode = mode is >= 0 and <= 2
            ? (ClientDisplayMode)mode
            : file.TryGet("Graphics", "FullScreen", out string legacy) && bool.TryParse(legacy, out bool full)
                ? full ? ClientDisplayMode.FullScreen : ClientDisplayMode.Windowed
                : DefaultMode;

        config.Width = file.GetInt("Graphics", "Resolution", DefaultWidth);
        config.Height = file.GetInt("Graphics", "ResolutionHeight", DefaultHeight);
        config.FpsCap = file.GetBool("Graphics", "FPSCap", DefaultFpsCap);
        config.Topmost = file.GetBool("Graphics", "AlwaysOnTop", DefaultTopmost);
        config.UseConfig = file.GetBool("Network", "UseConfig", false);
        config.Host = file.GetString("Network", "IPAddress", config.Host);
        config.Port = file.GetInt("Network", "Port", config.Port);
        return config;
    }

    /// <summary>
    /// Copies legacy personal graphics keys that the client configuration does
    /// not define yet. Existing client values win; the source file is not
    /// changed. Returns true when the target document was modified.
    /// </summary>
    public bool AdoptLegacy(IniFile legacy)
    {
        bool changed = false;
        if (!file.TryGet("Graphics", "Resolution", out _) && legacy.TryGet("Graphics", "Resolution", out string widthText) && int.TryParse(widthText, out int width) && width > 0 && width != DefaultWidth)
        { file.Set("Graphics", "Resolution", width); changed = true; }

        if (!file.TryGet("Graphics", "ResolutionHeight", out _) && legacy.TryGet("Graphics", "ResolutionHeight", out string heightText) && int.TryParse(heightText, out int height) && height > 0 && height != DefaultHeight)
        { file.Set("Graphics", "ResolutionHeight", height); changed = true; }

        if (!file.TryGet("Graphics", "DisplayMode", out _) && Mode == DefaultMode)
        {
            int? legacyMode = null;
            if (legacy.TryGet("Graphics", "DisplayMode", out string modeText) && int.TryParse(modeText, out int value) && value is >= 0 and <= 2) legacyMode = value;
            else if (legacy.TryGet("Graphics", "FullScreen", out string fullText) && bool.TryParse(fullText, out bool full)) legacyMode = full ? 2 : 0;
            if (legacyMode is int adopted && adopted != (int)DefaultMode) { file.Set("Graphics", "DisplayMode", adopted); changed = true; }
        }

        if (!file.TryGet("Graphics", "FPSCap", out _) && legacy.TryGet("Graphics", "FPSCap", out string capText) && bool.TryParse(capText, out bool cap) && cap != DefaultFpsCap)
        { file.Set("Graphics", "FPSCap", cap); changed = true; }

        if (!file.TryGet("Graphics", "AlwaysOnTop", out _) && legacy.TryGet("Graphics", "AlwaysOnTop", out string topText) && bool.TryParse(topText, out bool top) && top != DefaultTopmost)
        { file.Set("Graphics", "AlwaysOnTop", top); changed = true; }

        return changed;
    }

    /// <summary>
    /// Writes the player's changes. Keys that only repeat the previous value are
    /// left alone, and a changed value equal to the client default removes the
    /// key instead of pinning the default in the file.
    /// </summary>
    public void Save(int width, int height, ClientDisplayMode mode, bool fpsCap, bool topmost)
    {
        bool changed = false;

        if (width != Width)
            changed |= SetOrUnset("Graphics", "Resolution", width, DefaultWidth, width.ToString());
        if (height != Height)
            changed |= SetOrUnset("Graphics", "ResolutionHeight", height, DefaultHeight, height.ToString());

        if (mode != Mode)
        {
            if (mode == DefaultMode)
            {
                // Removing DisplayMode alone can leave a stale FullScreen entry
                // that the client would read instead. Clear both known keys.
                changed |= file.Remove("Graphics", "DisplayMode");
                changed |= file.Remove("Graphics", "FullScreen");
            }
            else
            {
                file.Set("Graphics", "DisplayMode", (int)mode);
                changed = true;
            }
        }

        if (fpsCap != FpsCap) changed |= SetOrUnset("Graphics", "FPSCap", fpsCap, DefaultFpsCap, fpsCap.ToString());
        if (topmost != Topmost) changed |= SetOrUnset("Graphics", "AlwaysOnTop", topmost, DefaultTopmost, topmost.ToString());

        if (!changed) return;
        file.Save();

        Width = width; Height = height; Mode = mode; FpsCap = fpsCap; Topmost = topmost;
    }

    /// <summary>Flushes changes already applied to the document (legacy adoption).</summary>
    public void Persist() => file.Save();

    private bool SetOrUnset(string section, string key, int value, int fallback, string text)
    {
        if (value == fallback) return file.Remove(section, key);
        file.Set(section, key, text); return true;
    }

    private bool SetOrUnset(string section, string key, bool value, bool fallback, string text)
    {
        if (value == fallback) return file.Remove(section, key);
        file.Set(section, key, text); return true;
    }
}

internal enum ClientDisplayMode { Windowed = 0, Maximized = 1, FullScreen = 2 }
