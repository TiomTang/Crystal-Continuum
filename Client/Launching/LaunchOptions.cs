using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Client.Resolution;

namespace Client.Launching
{
    /// <summary>
    /// Arguments the game understands on its own. The client runs without a launcher, so a
    /// shortcut, script or update tool can override the saved settings for one start.
    /// Parsing validates every value and only changes memory; Settings.SaveLaunchOverrides
    /// writes the explicit values afterwards.
    /// </summary>
    internal sealed class LaunchOptions
    {
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);

        private LaunchOptions()
        {
        }

        public string Host { get; private set; }

        public int? Port { get; private set; }

        public ResolutionOptions.Option? Resolution { get; private set; }

        public DisplayMode? DisplayMode { get; private set; }

        public bool? FpsCap { get; private set; }

        public bool? Topmost { get; private set; }

        public bool TestConfig { get; private set; }

        /// <summary>True when at least one argument has to survive into the settings file.</summary>
        public bool HasOverrides => Host != null || Port.HasValue || Resolution.HasValue
                                    || DisplayMode.HasValue || FpsCap.HasValue || Topmost.HasValue;

        public static LaunchOptions Parse(string[] args)
        {
            var options = new LaunchOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i].ToLowerInvariant();
                if (key == "-tc") key = "--test-config";
                if (!options._seen.Add(key)) throw new ArgumentException("Duplicate option: " + key);

                if (key == "--test-config")
                {
                    options.TestConfig = true;
                    continue;
                }

                if (i + 1 == args.Length) throw new ArgumentException("Missing value: " + key);

                string value = args[++i];

                switch (key)
                {
                    case "--host":
                        if (Uri.CheckHostName(value) == UriHostNameType.Unknown || value.Any(char.IsWhiteSpace))
                            throw new ArgumentException("Invalid host: " + value);
                        options.Host = value;
                        break;

                    case "--port":
                        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port) || port < 1 || port > 65535)
                            throw new ArgumentException("Invalid port (1..65535): " + value);
                        options.Port = port;
                        break;

                    case "--resolution":
                        if (!ResolutionOptions.TryParse(value, out ResolutionOptions.Option resolution))
                            throw new ArgumentException("Unsupported resolution: " + value);
                        options.Resolution = resolution;
                        break;

                    case "--display-mode":
                        options.DisplayMode = value switch
                        {
                            "windowed" => Client.DisplayMode.Windowed,
                            "maximized" => Client.DisplayMode.Maximized,
                            "fullscreen" => Client.DisplayMode.FullScreen,
                            _ => throw new ArgumentException("Invalid display-mode: " + value),
                        };
                        break;

                    case "--fps-cap":
                        options.FpsCap = Boolean(value, key);
                        break;

                    case "--topmost":
                        options.Topmost = Boolean(value, key);
                        break;

                    default:
                        throw new ArgumentException("Unknown option: " + key);
                }
            }

            return options;
        }

        private static bool Boolean(string value, string key)
        {
            if (bool.TryParse(value, out bool result)) return result;

            throw new ArgumentException("Invalid boolean: " + key);
        }
    }
}
