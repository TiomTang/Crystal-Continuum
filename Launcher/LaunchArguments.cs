using System.Diagnostics;
using System.Globalization;

namespace Launcher;

/// <summary>
/// Builds the client command line. Only operator overrides that were actually
/// configured are sent, plus the test-configuration switch; everything else is
/// read by the client from its own saved configuration. The client owns the
/// argument parser and validation, so the launcher must not add defaults here.
/// </summary>
internal static class LaunchArguments
{
    public static ProcessStartInfo StartInfo(string directory)
    {
        string root = Path.GetFullPath(directory);
        string client = Path.Combine(root, "Client.exe");
        if (!File.Exists(client)) throw new IOException("Client.exe is missing from the installation directory.");
        var info = new ProcessStartInfo(client) { WorkingDirectory = root, UseShellExecute = false };
        foreach (string argument in Build()) info.ArgumentList.Add(argument);
        return info;
    }

    /// <summary>Builds and validates the explicit override list.</summary>
    public static void Validate() => Build();

    private static string[] Build()
    {
        var arguments = new List<string>();
        void Add(string key, string value) { if (value != null) { arguments.Add(key); arguments.Add(value); } }

        Add("--host", Settings.OverrideHost);
        Add("--port", Settings.OverridePort?.ToString(CultureInfo.InvariantCulture));
        Add("--resolution", Settings.OverrideResolution);
        Add("--display-mode", Settings.OverrideMode switch
        {
            ClientDisplayMode.FullScreen => "fullscreen",
            ClientDisplayMode.Maximized => "maximized",
            ClientDisplayMode.Windowed => "windowed",
            _ => null,
        });
        Add("--fps-cap", Settings.OverrideFpsCap?.ToString());
        Add("--topmost", Settings.OverrideTopmost?.ToString());
        if (Settings.UseTestConfig) arguments.Add("--test-config");

        // Keep the pair invariant: every emitted switch is followed by one value.
        for (int i = 0; i < arguments.Count;)
        {
            string key = arguments[i];
            if (key == "--test-config") { i++; continue; }
            if (!key.StartsWith("--", StringComparison.Ordinal) || i + 1 >= arguments.Count || string.IsNullOrWhiteSpace(arguments[i + 1]))
                throw new InvalidDataException("Invalid launcher argument at position " + i + ".");
            i += 2;
        }

        return arguments.ToArray();
    }
}
