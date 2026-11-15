using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>The three shapes a bracketed key line can take.</summary>
    public enum ScriptPageKeyKind
    {
        /// <summary>An ordinary page such as [@MAIN]; a button may supply arguments as [@SUB(1,2)].</summary>
        Page,
        /// <summary>A page that asks the player for a line of text before it runs: [@@Name].</summary>
        Input,
        /// <summary>An automatic script marker such as [@_MAPCOORD(0,347,188)].</summary>
        Auto,
    }

    /// <summary>One key shape: the prefix that identifies it and whether it carries arguments.</summary>
    public sealed class ScriptPageKeyEntry
    {
        public readonly ScriptPageKeyKind Kind;
        public readonly string Prefix;
        public readonly bool UsesArguments;

        public ScriptPageKeyEntry(ScriptPageKeyKind kind, string prefix, bool usesArguments)
        {
            Kind = kind;
            Prefix = prefix;
            UsesArguments = usesArguments;
        }
    }

    /// <summary>
    /// The shapes of the bracketed keys that start a page. The loader, the automatic scripts and the
    /// editor read this table instead of spelling "[", "[@_" and "[@@" in their own code.
    /// </summary>
    public static class ScriptPageKeyCatalog
    {
        public static readonly ScriptPageKeyEntry Page = new ScriptPageKeyEntry(ScriptPageKeyKind.Page, "[", true);
        public static readonly ScriptPageKeyEntry Input = new ScriptPageKeyEntry(ScriptPageKeyKind.Input, "[@@", false);
        public static readonly ScriptPageKeyEntry Auto = new ScriptPageKeyEntry(ScriptPageKeyKind.Auto, "[@_", false);

        /// <summary>Longest prefix first, so "[@@" is not read as an ordinary page.</summary>
        public static readonly ScriptPageKeyEntry[] Entries = { Input, Auto, Page };

        /// <summary>
        /// A bracketed key line such as "[@MAIN]" or "[@SUB(1,2)]". The line is matched as written,
        /// without trimming, because the loader has always treated a key with trailing whitespace as
        /// text; the editor normalises its own view separately.
        /// </summary>
        public static bool TryParseHeader(string line, out string key)
        {
            key = null;

            if (string.IsNullOrEmpty(line)) return false;

            if (line.Length < 3 || line[0] != '[' || line[line.Length - 1] != ']') return false;

            key = line;

            return true;
        }

        /// <summary>Key shape for a key or a whole line, or null when it is not a key.</summary>
        public static ScriptPageKeyEntry Find(string key)
        {
            string text = key ?? string.Empty;

            foreach (ScriptPageKeyEntry entry in Entries)
            {
                if (text.StartsWith(entry.Prefix, StringComparison.OrdinalIgnoreCase)) return entry;
            }

            return null;
        }

        /// <summary>
        /// Reads an automatic script marker: "[@_MAPCOORD(0,347,188)]" answers kind MAPCOORD with
        /// { 0, 347, 188 }; a marker without arguments such as "[@_LevelUp]" answers an empty set.
        /// </summary>
        public static bool TryParseAuto(string line, out string marker, out string[] args)
        {
            marker = null;
            args = Empty;

            string key;

            if (!TryParseHeader(line, out key)) return false;

            if (Find(key) != Auto) return false;

            string body = key.Substring(Auto.Prefix.Length, key.Length - Auto.Prefix.Length - 1);

            int open = body.IndexOf('(');

            if (open < 0)
            {
                marker = body.Trim().ToUpperInvariant();

                return marker.Length > 0;
            }

            int close = body.LastIndexOf(')');

            if (close < open) return false;

            marker = body.Substring(0, open).Trim().ToUpperInvariant();

            if (marker.Length == 0) return false;

            string[] values = body.Substring(open + 1, close - open - 1).Split(',');

            for (int i = 0; i < values.Length; i++) values[i] = values[i].Trim();

            args = values;

            return true;
        }

        /// <summary>
        /// Splits "…(a,b)" off a page key. The key keeps an empty "()" so it still matches the header
        /// line the script wrote, exactly like the old parser.
        /// </summary>
        public static string SplitArguments(string key, out string[] args)
        {
            args = Empty;

            if (string.IsNullOrEmpty(key)) return key;

            int open = key.IndexOf('(');
            int close = key.LastIndexOf(')');

            if (open < 0 || close <= open) return key;

            string inner = key.Substring(open + 1, close - open - 1);

            args = inner.Split(',');

            return key.Substring(0, open) + "()" + key.Substring(close + 1);
        }

        private static readonly string[] Empty = new string[0];
    }
}
