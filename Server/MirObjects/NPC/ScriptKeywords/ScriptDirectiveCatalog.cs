using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>Directives the loader reads before a page is split.</summary>
    public enum ScriptDirectiveKind
    {
        Include,
        Insert,
    }

    /// <summary>One directive, as written at the start of a line.</summary>
    public sealed class ScriptDirectiveEntry
    {
        public readonly ScriptDirectiveKind Kind;
        public readonly string Tag;

        public ScriptDirectiveEntry(ScriptDirectiveKind kind, string tag)
        {
            Kind = kind;
            Tag = tag;
        }
    }

    /// <summary>
    /// The directives the loader handles before a page is split. The loader, the monster name rewriter
    /// and the editor's validation all read this table, so the tags are spelled once.
    /// </summary>
    public static class ScriptDirectiveCatalog
    {
        public static readonly ScriptDirectiveEntry[] Entries =
        {
            new ScriptDirectiveEntry(ScriptDirectiveKind.Include, "#INCLUDE"),
            new ScriptDirectiveEntry(ScriptDirectiveKind.Insert, "#INSERT"),
        };

        private static readonly Dictionary<string, ScriptDirectiveKind> Index = BuildIndex();
        private static readonly Dictionary<ScriptDirectiveKind, ScriptDirectiveEntry> ByKind = BuildKindIndex();

        private static Dictionary<string, ScriptDirectiveKind> BuildIndex()
        {
            var index = new Dictionary<string, ScriptDirectiveKind>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptDirectiveEntry entry in Entries) index[entry.Tag.Substring(1)] = entry.Kind;

            return index;
        }

        private static Dictionary<ScriptDirectiveKind, ScriptDirectiveEntry> BuildKindIndex()
        {
            var index = new Dictionary<ScriptDirectiveKind, ScriptDirectiveEntry>();

            foreach (ScriptDirectiveEntry entry in Entries) index[entry.Kind] = entry;

            return index;
        }

        /// <summary>Directive tag such as "#INSERT".</summary>
        public static string Tag(ScriptDirectiveKind kind)
        {
            ScriptDirectiveEntry entry;

            return ByKind.TryGetValue(kind, out entry) ? entry.Tag : string.Empty;
        }

        /// <summary>Directive for a tag, a name or a whole line, or null.</summary>
        public static ScriptDirectiveEntry Find(string text)
        {
            ScriptDirectiveKind kind;

            return TryParse(text, out kind) ? Find(kind) : null;
        }

        /// <summary>Directive for a kind.</summary>
        public static ScriptDirectiveEntry Find(ScriptDirectiveKind kind)
        {
            ScriptDirectiveEntry entry;

            return ByKind.TryGetValue(kind, out entry) ? entry : null;
        }

        /// <summary>Reads a directive from a token ("INSERT"), a tag ("#INSERT") or a whole line.</summary>
        public static bool TryParse(string text, out ScriptDirectiveKind kind)
        {
            kind = ScriptDirectiveKind.Include;

            if (string.IsNullOrWhiteSpace(text)) return false;

            string tag = text.TrimStart();

            if (tag[0] != '#') return false;

            tag = tag.Substring(1);

            int end = tag.IndexOfAny(new[] { ' ', '\t' });

            if (end >= 0) tag = tag.Substring(0, end);

            return Index.TryGetValue(tag, out kind);
        }

        /// <summary>True when the line starts with the directive, the way the loader matches it.</summary>
        public static bool Is(string line, ScriptDirectiveKind kind)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string tag = Tag(kind);

            return tag.Length > 0 && line.TrimStart().StartsWith(tag, StringComparison.OrdinalIgnoreCase);
        }
    }
}
