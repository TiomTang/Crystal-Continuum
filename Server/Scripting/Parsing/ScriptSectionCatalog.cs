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
        public readonly string Description;
        public readonly string Usage;

        /// <summary>True when the body ends at the next "[" header, as [Trade] and [Recipe] do.</summary>
        public readonly bool EndsAtHeader;

        /// <summary>Optional per-line test; the body ends at the first line that fails it.</summary>
        public readonly Func<string, bool> IsEntry;

        /// <summary>Consumes the section body; the header index is where the section starts.</summary>
        internal readonly Action<NPCScript, ScriptCompilation, int> Parse;

        internal ScriptSectionEntry(ScriptSectionKind kind, string key, int order,
            Action<NPCScript, ScriptCompilation, int> parse, bool endsAtHeader = false, Func<string, bool> isEntry = null,
            string description = "", string usage = "")
        {
            Kind = kind;
            Key = key;
            Order = order;
            Parse = parse;
            EndsAtHeader = endsAtHeader;
            IsEntry = isEntry;
            Description = description;
            Usage = usage;
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
            new ScriptSectionEntry(ScriptSectionKind.Types, "[TYPES]", 10,
                (script, compilation, index) => script.ParseTypeSection(compilation, index), isEntry: IsInteger,
                description: "Item type numbers accepted by this NPC for selling and services.", usage: "<item type number>"),
            new ScriptSectionEntry(ScriptSectionKind.UsedTypes, "[USEDTYPES]", 20,
                (script, compilation, index) => script.ParseUsedTypeSection(compilation, index), isEntry: IsInteger,
                description: "Item type numbers this NPC accepts as used goods.", usage: "<item type number>"),
            new ScriptSectionEntry(ScriptSectionKind.Trade, "[TRADE]", 30,
                (script, compilation, index) => script.ParseGoodsSection(compilation, index), endsAtHeader: true,
                description: "Goods this NPC sells: item name and optional stock count.", usage: "<item> [count]"),
            new ScriptSectionEntry(ScriptSectionKind.Recipe, "[RECIPE]", 40,
                (script, compilation, index) => script.ParseCraftingSection(compilation, index), endsAtHeader: true,
                description: "Craftable item names resolved against the recipe definitions.", usage: "<crafted item>"),
            new ScriptSectionEntry(ScriptSectionKind.Quests, "[QUESTS]", 50,
                (script, compilation, index) => script.ParseQuestsSection(compilation, index), endsAtHeader: true,
                description: "Quest indexes: positive starts a quest here, negative finishes it here.", usage: "<signed quest index>"),
            new ScriptSectionEntry(ScriptSectionKind.Speech, "[SPEECH]", 60,
                (script, compilation, index) => script.ParseSpeechSection(compilation, index), isEntry: IsSpeech,
                description: "Ambient speech lines: relative weight followed by the spoken text.", usage: "<weight> <text>"),
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
        /// <summary>Header text of a section; the table is the only place it is written.</summary>
        public static string Key(ScriptSectionKind kind)
        {
            ScriptSectionEntry section = Find(kind);

            return section == null ? null : section.Key;
        }

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
