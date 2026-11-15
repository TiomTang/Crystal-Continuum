using System;
using System.Collections.Generic;

namespace Server.Scripting
{
    /// <summary>Sections a quest definition file can carry.</summary>
    /// <remarks>
    /// Member order is the order the loader has always consumed the sections in, and the order of
    /// <see cref="QuestScriptSectionCatalog.Entries"/>. Adding a section means adding a member, a row in the
    /// table, and a branch in the runtime consumer.
    /// </remarks>
    public enum QuestScriptSectionKind
    {
        Description,
        TaskDescription,
        Completion,
        CarryItems,
        KillTasks,
        ItemTasks,
        FlagTasks,
        FixedRewards,
        SelectRewards,
        ExpReward,
        GoldReward,
        CreditReward,
        ReturnDescription,
    }

    /// <summary>How one body line of a quest section is read.</summary>
    /// <remarks>
    /// The two item shapes differ on purpose: <see cref="Item"/> reads an optional quoted message, while
    /// <see cref="Reward"/> never did and also resolves "(M)"/"(F)" variants in the runtime.
    /// </remarks>
    public enum QuestScriptValueKind
    {
        Text,
        Item,
        Monster,
        Flag,
        Reward,
        Number,
    }

    /// <summary>One quest section: the header text, how its body is read, and the description the editor shows.</summary>
    public sealed class QuestScriptSectionEntry
    {
        public QuestScriptSectionKind Kind { get; }

        /// <summary>The header as it is written in quest files, e.g. <c>[@Description]</c>.</summary>
        public string Key { get; }

        public string Description { get; }

        public QuestScriptValueKind ValueKind { get; }

        internal QuestScriptSectionEntry(QuestScriptSectionKind kind, string key, QuestScriptValueKind valueKind, string description)
        {
            Kind = kind;
            Key = key;
            ValueKind = valueKind;
            Description = description;
        }
    }

    /// <summary>
    /// The sections a quest file is made of. The runtime loader, the editor palette and the editor checks
    /// all read this table, so a section added here is parsed, offered and validated without a second list.
    /// </summary>
    public static class QuestScriptSectionCatalog
    {
        /// <summary>Every section, in the order the runtime has always consumed them.</summary>
        public static readonly QuestScriptSectionEntry[] Entries =
        {
            new QuestScriptSectionEntry(QuestScriptSectionKind.Description, "[@Description]", QuestScriptValueKind.Text,
                "Lines the client shows on the quest detail page: one entry per line."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.TaskDescription, "[@TaskDescription]", QuestScriptValueKind.Text,
                "Lines shown while the quest is active: one entry per line."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.Completion, "[@Completion]", QuestScriptValueKind.Text,
                "Lines shown when the quest can be handed in: one entry per line."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.CarryItems, "[@CarryItems]", QuestScriptValueKind.Item,
                "Items the player must carry to accept the quest: item name and optional count."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.KillTasks, "[@KillTasks]", QuestScriptValueKind.Monster,
                "Monsters the player must kill: monster name, optional count, optional quoted message."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.ItemTasks, "[@ItemTasks]", QuestScriptValueKind.Item,
                "Items the player must collect: item name, optional count, optional quoted message."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.FlagTasks, "[@FlagTasks]", QuestScriptValueKind.Flag,
                "Progress flags: flag number and optional quoted message."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.FixedRewards, "[@FixedRewards]", QuestScriptValueKind.Reward,
                "Rewards every completion gives: item name and optional count."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.SelectRewards, "[@SelectRewards]", QuestScriptValueKind.Reward,
                "Rewards the player picks one of: item name and optional count."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.ExpReward, "[@ExpReward]", QuestScriptValueKind.Number,
                "Experience granted on completion: a single non-negative number."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.GoldReward, "[@GoldReward]", QuestScriptValueKind.Number,
                "Gold granted on completion: a single non-negative number."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.CreditReward, "[@CreditReward]", QuestScriptValueKind.Number,
                "Game shop credit granted on completion: a single non-negative number."),
            new QuestScriptSectionEntry(QuestScriptSectionKind.ReturnDescription, "[@ReturnDescription]", QuestScriptValueKind.Text,
                "Lines shown after the player returns from the task: one entry per line."),
        };

        private static readonly Dictionary<string, QuestScriptSectionEntry> Index = BuildIndex();
        private static readonly Dictionary<QuestScriptSectionKind, QuestScriptSectionEntry> ByKind = BuildKindIndex();

        private static Dictionary<string, QuestScriptSectionEntry> BuildIndex()
        {
            var index = new Dictionary<string, QuestScriptSectionEntry>(StringComparer.OrdinalIgnoreCase);

            // Add, not an indexer: two rows for one header is a table defect and must fail here.
            foreach (QuestScriptSectionEntry entry in Entries) index.Add(entry.Key, entry);

            return index;
        }

        private static Dictionary<QuestScriptSectionKind, QuestScriptSectionEntry> BuildKindIndex()
        {
            var index = new Dictionary<QuestScriptSectionKind, QuestScriptSectionEntry>();

            foreach (QuestScriptSectionEntry entry in Entries) index.Add(entry.Kind, entry);

            return index;
        }

        /// <summary>
        /// The section whose header is exactly this line, or null. Matching is the loader's: the whole line,
        /// case-insensitive, with no trimming, so <c>[@Description] </c> is not a header.
        /// </summary>
        public static QuestScriptSectionEntry Find(string rawHeader)
        {
            QuestScriptSectionEntry entry;

            return rawHeader != null && Index.TryGetValue(rawHeader, out entry) ? entry : null;
        }

        /// <summary>The section for a kind, for callers that already know which one they mean.</summary>
        public static QuestScriptSectionEntry Find(QuestScriptSectionKind kind)
        {
            QuestScriptSectionEntry entry;

            return ByKind.TryGetValue(kind, out entry) ? entry : null;
        }

        /// <summary>Header text of a section; the table is the only place it is written.</summary>
        public static string Key(QuestScriptSectionKind kind)
        {
            QuestScriptSectionEntry entry = Find(kind);

            return entry == null ? null : entry.Key;
        }
    }
}
