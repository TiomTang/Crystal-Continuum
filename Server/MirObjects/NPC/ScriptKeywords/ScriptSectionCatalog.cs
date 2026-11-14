using System;
using System.Collections.Generic;

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

    /// <summary>One data section: the header the loader matches, plus the kind the checks switch on.</summary>
    public sealed class ScriptSectionEntry
    {
        public readonly ScriptSectionKind Kind;
        public readonly string Key;

        public ScriptSectionEntry(ScriptSectionKind kind, string key)
        {
            Kind = kind;
            Key = key;
        }
    }

    /// <summary>
    /// Data sections the loader understands. The parser, the palette and the checks all read this list,
    /// so a section added here is offered, parsed and validated without touching three places.
    /// </summary>
    public static class ScriptSectionCatalog
    {
        public static readonly ScriptSectionEntry[] Entries =
        {
            new ScriptSectionEntry(ScriptSectionKind.Types, NPCScript.TypeKey),
            new ScriptSectionEntry(ScriptSectionKind.UsedTypes, NPCScript.UsedTypeKey),
            new ScriptSectionEntry(ScriptSectionKind.Trade, NPCScript.TradeKey),
            new ScriptSectionEntry(ScriptSectionKind.Recipe, NPCScript.RecipeKey),
            new ScriptSectionEntry(ScriptSectionKind.Quests, NPCScript.QuestKey),
            new ScriptSectionEntry(ScriptSectionKind.Speech, NPCScript.SpeechKey),
        };

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
    }
}
