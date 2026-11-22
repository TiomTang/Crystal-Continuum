namespace Launcher;

/// <summary>
/// Resolution choices offered by the launcher settings window. This is a display
/// convenience only: the client owns argument validation and the authoritative
/// list, so a value outside this list can still be valid for the game.
/// </summary>
internal static class ResolutionOptions
{
    public readonly struct Option
    {
        public Option(int width, int height) { Width = width; Height = height; }
        public int Width { get; }
        public int Height { get; }
        public string Label => Width + "x" + Height;
    }

    public static readonly Option[] All =
    {
        new Option(1024, 768),
        new Option(1280, 800),
        new Option(1366, 768),
        new Option(1280, 720),
        new Option(1920, 1080),
    };

    /// <summary>Falls back to the first option with the same width, then to the smallest option.</summary>
    public static int IndexOf(int width, int height)
    {
        for (int i = 0; i < All.Length; i++) if (All[i].Width == width && All[i].Height == height) return i;
        for (int i = 0; i < All.Length; i++) if (All[i].Width == width) return i;
        return 0;
    }
}
