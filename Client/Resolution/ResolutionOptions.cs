using System;
using System.Globalization;

namespace Client.Resolution
{
    /// <summary>
    /// Resolution choices the client understands. Settings stores the width and the height
    /// separately because the width alone cannot tell 1280x800 and 1280x720 apart.
    /// </summary>
    internal static class ResolutionOptions
    {
        public readonly struct Option
        {
            public Option(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public int Width { get; }

            public int Height { get; }

            public string Label => string.Format("{0}x{1} ({2})", Width, Height, Aspect(Width, Height));

            /// <summary>Text accepted by the <c>--resolution</c> argument and read by settings.</summary>
            public string Value => string.Format(CultureInfo.InvariantCulture, "{0}x{1}", Width, Height);
        }

        public static readonly Option[] All =
        {
            new Option(1024, 768),
            new Option(1280, 800),
            new Option(1366, 768),
            new Option(1280, 720),
            new Option(1920, 1080),
        };

        /// <summary>
        /// Find the option that matches the saved value. Fall back to the first option with the
        /// same width when the height does not match, and to the smallest option when nothing matches.
        /// </summary>
        public static int IndexOf(int width, int height)
        {
            int exact = ExactIndex(width, height);

            if (exact >= 0) return exact;

            for (int i = 0; i < All.Length; i++)
                if (All[i].Width == width)
                    return i;

            return 0;
        }

        /// <summary>
        /// The server sends the maximum allowed width. Above it, fall back to the largest option
        /// that does not exceed the limit.
        /// </summary>
        public static Option Resolve(int width, int height, int maxWidth)
        {
            Option option = All[IndexOf(width, height)];

            if (maxWidth <= 0 || option.Width <= maxWidth)
                return option;

            Option fallback = default;

            for (int i = 0; i < All.Length; i++)
                if (All[i].Width <= maxWidth && (fallback.Width == 0 || All[i].Width > fallback.Width))
                    fallback = All[i];

            return fallback.Width == 0 ? All[0] : fallback;
        }

        /// <summary>
        /// Read an exact <c>WIDTHxHEIGHT</c> choice. Unlike <see cref="IndexOf"/> this never
        /// substitutes a different height, so a bad argument is rejected instead of reshaped.
        /// </summary>
        public static bool TryParse(string value, out Option option)
        {
            option = default;

            if (string.IsNullOrWhiteSpace(value)) return false;

            string[] parts = value.Split('x', 'X');

            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int width)) return false;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int height)) return false;

            int index = ExactIndex(width, height);

            if (index < 0) return false;

            option = All[index];
            return true;
        }

        private static int ExactIndex(int width, int height)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Width == width && All[i].Height == height)
                    return i;

            return -1;
        }

        private static string Aspect(int width, int height)
        {
            double ratio = (double)width / height;

            if (Math.Abs(ratio - 16d / 9d) < 0.02) return "16:9";
            if (Math.Abs(ratio - 16d / 10d) < 0.02) return "16:10";
            if (Math.Abs(ratio - 4d / 3d) < 0.02) return "4:3";
            if (Math.Abs(ratio - 5d / 4d) < 0.02) return "5:4";

            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}:1", ratio);
        }
    }
}
