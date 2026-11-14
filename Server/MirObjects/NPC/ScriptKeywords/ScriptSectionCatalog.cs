using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.MirObjects
{
    /// <summary>Sections that carry data for the NPC, not clickable pages.</summary>
    public enum ScriptSectionKind
    {
        None,
        Types,
        UsedTypes,
        Trade,
        Recipe,
        Quests,
        Speech,
    }

    /// <summary>One body line of a data section, with the line number the loader had before.</summary>
    public struct ScriptSectionLine
    {
        public readonly int Index;
        public readonly string Text;

        public ScriptSectionLine(int index, string text)
        {
            Index = index;
            Text = text;
        }
    }

    /// <summary>
    /// One data section: the header the loader matches, the order it runs in, how its body ends, and
    /// the parser that consumes it. Adding a section adds a row here; the loader does not change.
    /// </summary>
    public sealed class ScriptSectionEntry
    {
        public readonly ScriptSectionKind Kind;
        public readonly string Key;
        public readonly int Order;

        /// <summary>True when the body ends at the next "[" header, as [Trade] and [Recipe] do.</summary>
        public readonly bool EndsAtHeader;

        /// <summary>Optional per-line test; the body ends at the first line that fails it.</summary>
        public readonly Func<string, bool> IsEntry;

        /// <summary>Consumes the section body; the header index is where the section starts.</summary>
        public readonly Action<NPCScript, IList<string>, int> Parse;

        public ScriptSectionEntry(ScriptSectionKind kind, string key, int order,
            Action<NPCScript, IList<string>, int> parse, bool endsAtHeader = false, Func<string, bool> isEntry = null)
        {
            Kind = kind;
            Key = key;
            Order = order;
            Parse = parse;
            EndsAtHeader = endsAtHeader;
            IsEntry = isEntry;
        }

        /// <summary>True when this line is the header of the section.</summary>
        public bool MatchesHeader(string line)
        {
            return !string.IsNullOrEmpty(line) && line.StartsWith(Key, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Data sections the loader understands. The loader, the palette and the checks all read this list,
    /// so a section added here is parsed, offered and validated without touching three places.
    /// </summary>
    public static class ScriptSectionCatalog
    {
        public static readonly ScriptSectionEntry[] Entries =
        {
            new ScriptSectionEntry(ScriptSectionKind.Types, NPCScript.TypeKey, 10,
                (script, lines, index) => script.ParseTypeSection(lines, index), isEntry: IsInteger),
            new ScriptSectionEntry(ScriptSectionKind.UsedTypes, NPCScript.UsedTypeKey, 20,
                (script, lines, index) => script.ParseUsedTypeSection(lines, index), isEntry: IsInteger),
            new ScriptSectionEntry(ScriptSectionKind.Trade, NPCScript.TradeKey, 30,
                (script, lines, index) => script.ParseGoodsSection(lines, index), endsAtHeader: true),
            new ScriptSectionEntry(ScriptSectionKind.Recipe, NPCScript.RecipeKey, 40,
                (script, lines, index) => script.ParseCraftingSection(lines, index), endsAtHeader: true),
            new ScriptSectionEntry(ScriptSectionKind.Quests, NPCScript.QuestKey, 50,
                (script, lines, index) => script.ParseQuestsSection(lines, index), endsAtHeader: true),
            new ScriptSectionEntry(ScriptSectionKind.Speech, NPCScript.SpeechKey, 60,
                (script, lines, index) => script.ParseSpeechSection(lines, index), isEntry: IsSpeech),
        };

        /// <summary>Sections in the order the loader runs them.</summary>
        public static readonly ScriptSectionEntry[] Ordered = Entries.OrderBy(e => e.Order).ToArray();

        private static readonly Dictionary<string, ScriptSectionEntry> Index = BuildIndex();
        private static readonly Dictionary<ScriptSectionKind, ScriptSectionEntry> ByKind = BuildKindIndex();

        private static Dictionary<string, ScriptSectionEntry> BuildIndex()
        {
            var index = new Dictionary<string, ScriptSectionEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptSectionEntry entry in Entries) index[entry.Key] = entry;

            return index;
        }

        private static Dictionary<ScriptSectionKind, ScriptSectionEntry> BuildKindIndex()
        {
            var index = new Dictionary<ScriptSectionKind, ScriptSectionEntry>();

            foreach (ScriptSectionEntry entry in Entries) index[entry.Kind] = entry;

            return index;
        }

        /// <summary>Section for a header line such as "[Trade]", or null for an ordinary page.</summary>
        public static ScriptSectionEntry Find(string key)
        {
            ScriptSectionEntry entry;

            return key != null && Index.TryGetValue(key.Trim(), out entry) ? entry : null;
        }

        /// <summary>Section for a kind, for callers that already know which one they mean.</summary>
        public static ScriptSectionEntry Find(ScriptSectionKind kind)
        {
            ScriptSectionEntry entry;

            return ByKind.TryGetValue(kind, out entry) ? entry : null;
        }

        /// <summary>
        /// The body lines that follow a header, with empty lines skipped and the section's own end
        /// rule applied: the next "[", the first line that is not an entry, or the end of the file.
        /// </summary>
        public static IEnumerable<ScriptSectionLine> Body(IList<string> lines, int headerIndex, ScriptSectionKind kind)
        {
            ScriptSectionEntry entry = Find(kind);

            if (entry == null) yield break;

            for (int i = headerIndex + 1; i < lines.Count; i++)
            {
                string line = lines[i];

                if (string.IsNullOrEmpty(line)) continue;

                if (entry.EndsAtHeader && line.StartsWith("[")) yield break;

                if (entry.IsEntry != null && !entry.IsEntry(line)) yield break;

                yield return new ScriptSectionLine(i, line);
            }
        }

        private static bool IsInteger(string line)
        {
            int value;

            return int.TryParse(line, out value);
        }

        private static bool IsSpeech(string line)
        {
            string[] parts = line.Split(' ');
            int weight;

            return parts.Length >= 2 && int.TryParse(parts[0], out weight);
        }
    }
}
