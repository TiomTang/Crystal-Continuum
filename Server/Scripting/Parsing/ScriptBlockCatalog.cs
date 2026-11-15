using System;
using System.Collections.Generic;

namespace Server.Scripting
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

    /// <summary>
    /// What a block collects. Several tags can share a role, which is how a new tag is added without
    /// teaching the segment parser a new destination.
    /// </summary>
    public enum ScriptBlockRole
    {
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
        public readonly ScriptBlockRole Role;
        public readonly string Description;

        /// <summary>True when this tag starts a new segment inside a page.</summary>
        public readonly bool SplitsSegment;

        public ScriptBlockEntry(ScriptBlockKind kind, string text, ScriptBlockRole role, bool splitsSegment = false, string description = "")
        {
            Kind = kind;
            Text = text;
            Role = role;
            SplitsSegment = splitsSegment;
            Description = description;
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
            new ScriptBlockEntry(ScriptBlockKind.Check, "#IF", ScriptBlockRole.Check, true, description: "Start a condition block."),
            new ScriptBlockEntry(ScriptBlockKind.Say, "#SAY", ScriptBlockRole.Say, description: "Text the NPC says."),
            new ScriptBlockEntry(ScriptBlockKind.Act, "#ACT", ScriptBlockRole.Act, description: "Actions that run when the checks pass."),
            new ScriptBlockEntry(ScriptBlockKind.ElseSay, "#ELSESAY", ScriptBlockRole.ElseSay, description: "Text when the checks fail."),
            new ScriptBlockEntry(ScriptBlockKind.ElseAct, "#ELSEACT", ScriptBlockRole.ElseAct, description: "Actions when the checks fail."),
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
        /// Reads a tag from a token ("IF"), a tag ("#IF") or a whole line ("#IF LEVEL >= 1").
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

        /// <summary>
        /// True when the line is exactly this block tag, with the leading '#' and ignoring indentation,
        /// so a misspelled tag such as "#IFX" is left for the parser to report instead of splitting the
        /// page the way "#IF" would.
        /// </summary>
        public static bool Is(string line, ScriptBlockKind kind)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string text = line.TrimStart();

            if (text.Length == 0 || text[0] != '#') return false;

            ScriptBlockKind found;

            return TryParse(text, out found) && found == kind;
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
        }

        /// <summary>Block for a line, or null when the line is not a block tag.</summary>
        public static ScriptBlockEntry TryFind(string line, out ScriptBlockKind kind)
        {
            return TryParse(line, out kind) ? Find(kind) : null;
        }

        /// <summary>
        /// True when the line is a tag that starts a new segment, as #IF does. The leading '#' is
        /// required: dialogue such as "If you..." is text, not a tag.
        /// </summary>
        public static bool SplitsSegment(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string text = line.TrimStart();

            if (text.Length == 0 || text[0] != '#') return false;

            ScriptBlockKind kind;
            ScriptBlockEntry entry = TryFind(text, out kind);

            return entry != null && entry.SplitsSegment;
        }
    }
}
