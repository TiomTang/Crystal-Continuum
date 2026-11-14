using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>Which part of a page a line belongs to. Page means "outside any block".</summary>
    public enum ScriptBlockKind
    {
        Page,
        Check,
        Say,
        Act,
        ElseSay,
        ElseAct,
    }

    /// <summary>One block tag, as written in a script.</summary>
    public sealed class ScriptBlockEntry
    {
        public readonly ScriptBlockKind Kind;
        public readonly string Text;

        public ScriptBlockEntry(ScriptBlockKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }

    /// <summary>
    /// The tags that split a page into blocks. The loader, the editor's parser, the structure palette and
    /// the monster name rewriter all read this table, so each tag is spelled in one place.
    /// </summary>
    public static class ScriptBlockCatalog
    {
        public static readonly ScriptBlockEntry[] Entries =
        {
            new ScriptBlockEntry(ScriptBlockKind.Check, "#IF"),
            new ScriptBlockEntry(ScriptBlockKind.Say, "#SAY"),
            new ScriptBlockEntry(ScriptBlockKind.Act, "#ACT"),
            new ScriptBlockEntry(ScriptBlockKind.ElseSay, "#ELSESAY"),
            new ScriptBlockEntry(ScriptBlockKind.ElseAct, "#ELSEACT"),
        };

        private static readonly Dictionary<string, ScriptBlockKind> Index = BuildIndex();
        private static readonly Dictionary<ScriptBlockKind, string> Texts = BuildTexts();

        private static Dictionary<string, ScriptBlockKind> BuildIndex()
        {
            var index = new Dictionary<string, ScriptBlockKind>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptBlockEntry tag in Entries) index[tag.Text.Substring(1)] = tag.Kind;

            return index;
        }

        private static Dictionary<ScriptBlockKind, string> BuildTexts()
        {
            var texts = new Dictionary<ScriptBlockKind, string>();

            foreach (ScriptBlockEntry tag in Entries) texts[tag.Kind] = tag.Text;

            return texts;
        }

        /// <summary>Tag text such as "#IF", or an empty string for a line outside any block.</summary>
        public static string Tag(ScriptBlockKind kind)
        {
            string text;

            return Texts.TryGetValue(kind, out text) ? text : string.Empty;
        }

        /// <summary>
        /// Reads a tag from a token ("IF"), a tag ("#IF") or a whole line ("#IF CHECKLEVEL 1").
        /// </summary>
        public static bool TryParse(string text, out ScriptBlockKind kind)
        {
            kind = ScriptBlockKind.Page;

            if (string.IsNullOrWhiteSpace(text)) return false;

            string tag = text.Trim();

            if (tag[0] == '#') tag = tag.Substring(1);

            int end = tag.IndexOfAny(new[] { ' ', '\t' });

            if (end >= 0) tag = tag.Substring(0, end);

            return Index.TryGetValue(tag, out kind);
        }

        /// <summary>True when the line starts with the tag, the way the loader splits segments.</summary>
        public static bool Is(string line, ScriptBlockKind kind)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string text = Tag(kind);

            return text.Length > 0 && line.StartsWith(text, StringComparison.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<ScriptBlockKind, ScriptBlockEntry> ByKind = BuildKindIndex();

        private static Dictionary<ScriptBlockKind, ScriptBlockEntry> BuildKindIndex()
        {
            var index = new Dictionary<ScriptBlockKind, ScriptBlockEntry>();

            foreach (ScriptBlockEntry entry in Entries) index[entry.Kind] = entry;

            return index;
        }

        /// <summary>Block for a tag such as "#IF", or null when it is not a block tag.</summary>
        public static ScriptBlockEntry Find(string tag)
        {
            ScriptBlockKind kind;

            return TryParse(tag, out kind) ? Find(kind) : null;
        }

        /// <summary>Block for a kind.</summary>
        public static ScriptBlockEntry Find(ScriptBlockKind kind)
        {
            ScriptBlockEntry entry;

            return ByKind.TryGetValue(kind, out entry) ? entry : null;
        }    }
}
