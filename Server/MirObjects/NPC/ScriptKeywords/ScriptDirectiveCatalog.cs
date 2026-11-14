using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.MirObjects
{
    /// <summary>One loader directive: the tag, the order it runs in, and what it does to the lines.</summary>
    public sealed class ScriptDirectiveEntry
    {
        public readonly string Tag;

        /// <summary>Lowest runs first; #INSERT feeds #INCLUDE, so it stays ahead of it.</summary>
        public readonly int Order;

        public readonly Action<List<string>> Apply;

        public ScriptDirectiveEntry(string tag, int order, Action<List<string>> apply)
        {
            Tag = tag;
            Order = order;
            Apply = apply;
        }
    }

    /// <summary>
    /// The directives the loader handles before a page is split. The loader, the monster name rewriter
    /// and the editor's validation all read this table, so the tags are spelled once. A directive is
    /// identified by its entry, not by an enum, because nothing switches on the kind any more.
    /// </summary>
    public static class ScriptDirectiveCatalog
    {
        public static readonly ScriptDirectiveEntry Insert =
            new ScriptDirectiveEntry("#INSERT", 10, ScriptDirectiveHandlers.Insert);

        public static readonly ScriptDirectiveEntry Include =
            new ScriptDirectiveEntry("#INCLUDE", 20, ScriptDirectiveHandlers.Include);

        public static readonly ScriptDirectiveEntry[] Entries = { Insert, Include };

        /// <summary>Directives in the order the loader runs them.</summary>
        public static readonly ScriptDirectiveEntry[] Ordered = Entries.OrderBy(e => e.Order).ToArray();

        private static readonly Dictionary<string, ScriptDirectiveEntry> Index = BuildIndex();

        private static Dictionary<string, ScriptDirectiveEntry> BuildIndex()
        {
            var index = new Dictionary<string, ScriptDirectiveEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptDirectiveEntry entry in Entries) index[entry.Tag.Substring(1)] = entry;

            return index;
        }

        /// <summary>Directive tag such as "#INSERT".</summary>
        public static string Tag(ScriptDirectiveEntry entry)
        {
            return entry == null ? string.Empty : entry.Tag;
        }

        /// <summary>Directive for a tag, a name or a whole line, or null.</summary>
        public static ScriptDirectiveEntry Find(string text)
        {
            ScriptDirectiveEntry entry;

            return TryParse(text, out entry) ? entry : null;
        }

        /// <summary>Reads a directive from a token ("INSERT") with its '#', or a whole line.</summary>
        public static bool TryParse(string text, out ScriptDirectiveEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(text)) return false;

            string tag = text.TrimStart();

            if (tag[0] != '#') return false;

            tag = tag.Substring(1);

            int end = tag.IndexOfAny(new[] { ' ', '\t' });

            if (end >= 0) tag = tag.Substring(0, end);

            return Index.TryGetValue(tag, out entry);
        }

        /// <summary>
        /// True when the line is exactly this directive, with the leading '#' and ignoring indentation,
        /// so a longer tag such as "#INCLUDEX" is not expanded as "#INCLUDE".
        /// </summary>
        public static bool Is(string line, ScriptDirectiveEntry entry)
        {
            ScriptDirectiveEntry found;

            return entry != null && TryParse(line, out found) && ReferenceEquals(found, entry);
        }
    }
}
