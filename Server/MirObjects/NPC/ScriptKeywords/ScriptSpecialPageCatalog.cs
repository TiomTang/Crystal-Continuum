using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>Which panel or request a page key stands for.</summary>
    public enum ScriptSpecialPageKind
    {
        Main,
        Buy,
        BuySell,
        Sell,
        BuyNew,
        BuySellNew,
        BuyBack,
        BuyUsed,
        Repair,
        SRepair,
        Refine,
        RefineCheck,
        RefineCollect,
        ReplaceWeddingRing,
        Storage,
        Consign,
        Market,
        Craft,
        PearlBuy,
        CreateGuild,
        RequestWar,
        GuildTerritory,
        SendParcel,
        CollectParcel,
        Awakening,
        Disassemble,
        Downgrade,
        Reset,
        CreateHero,
        ManageHero,
    }

    /// <summary>
    /// A page key the engine reacts to on its own: opening a panel, starting a guild request and so on.
    /// The key comes from the NPCScript constants and the handler from NPCScript.Specials.cs, so the
    /// editor, the docs and the loader read one list instead of keeping a switch of their own.
    /// </summary>
    public sealed class ScriptSpecialPageEntry
    {
        public readonly ScriptSpecialPageKind Kind;
        public readonly string Key;
        public readonly string Description;

        /// <summary>Null when the page needs no extra work beyond showing its text.</summary>
        public readonly Action<NPCScript, PlayerObject, NPCPage> Executor;

        public ScriptSpecialPageEntry(ScriptSpecialPageKind kind, string key, string description, Action<NPCScript, PlayerObject, NPCPage> executor = null)
        {
            Kind = kind;
            Key = key;
            Description = description;
            Executor = executor;
        }
    }

    public static class ScriptSpecialPageCatalog
    {
        public static readonly ScriptSpecialPageEntry[] Entries =
        {
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Main, NPCScript.MainKey, "Entry page of the script."),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Buy, NPCScript.BuyKey, "Open the buy panel with the goods listed in [Trade].", NPCScript.Special_Buy),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.BuySell, NPCScript.BuySellKey, "Open the buy panel and the sell panel together.", NPCScript.Special_Buy),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Sell, NPCScript.SellKey, "Open the sell panel.", NPCScript.Special_Sell),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.BuyNew, NPCScript.BuyNewKey, "Open the buy panel without asking for player goods.", NPCScript.Special_BuyNew),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.BuySellNew, NPCScript.BuySellNewKey, "Open the buy panel and the sell panel without asking for player goods.", NPCScript.Special_BuyNew),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.BuyBack, NPCScript.BuyBackKey, "Open the buy back list.", NPCScript.Special_BuyBack),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.BuyUsed, NPCScript.BuyUsedKey, "Open the used goods panel.", NPCScript.Special_BuyUsed),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Repair, NPCScript.RepairKey, "Open the repair panel.", NPCScript.Special_Repair),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.SRepair, NPCScript.SRepairKey, "Open the special repair panel.", NPCScript.Special_SRepair),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Refine, NPCScript.RefineKey, "Open the refine panel.", NPCScript.Special_Refine),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.RefineCheck, NPCScript.RefineCheckKey, "Check the outcome of a refinement.", NPCScript.Special_RefineCheck),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.RefineCollect, NPCScript.RefineCollectKey, "Collect the refined item.", NPCScript.Special_RefineCollect),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.ReplaceWeddingRing, NPCScript.ReplaceWedRingKey, "Replace the wedding ring.", NPCScript.Special_ReplaceWeddingRing),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Storage, NPCScript.StorageKey, "Open the storage panel.", NPCScript.Special_Storage),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Consign, NPCScript.ConsignKey, "Open the consignment panel.", NPCScript.Special_Consign),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Market, NPCScript.MarketKey, "Open the market panel.", NPCScript.Special_Market),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Craft, NPCScript.CraftKey, "Open the crafting panel with the recipes listed in [Recipe].", NPCScript.Special_Craft),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.PearlBuy, NPCScript.PearlBuyKey, "Open the pearl shop.", NPCScript.Special_PearlBuy),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.CreateGuild, NPCScript.GuildCreateKey, "Ask the player to create a guild.", NPCScript.Special_GuildCreate),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.RequestWar, NPCScript.RequestWarKey, "Ask the player to request a castle war.", NPCScript.Special_RequestWar),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.GuildTerritory, NPCScript.GuildTerritoryKey, "Open the guild territory page.", NPCScript.Special_GuildTerritory),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.SendParcel, NPCScript.SendParcelKey, "Open the send parcel dialog.", NPCScript.Special_SendParcel),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.CollectParcel, NPCScript.CollectParcelKey, "Collect a waiting parcel.", NPCScript.Special_CollectParcel),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Awakening, NPCScript.AwakeningKey, "Open the awakening panel.", NPCScript.Special_Awakening),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Disassemble, NPCScript.DisassembleKey, "Open the disassemble panel.", NPCScript.Special_Disassemble),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Downgrade, NPCScript.DowngradeKey, "Open the downgrade panel.", NPCScript.Special_Downgrade),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.Reset, NPCScript.ResetKey, "Open the reset panel.", NPCScript.Special_Reset),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.CreateHero, NPCScript.HeroCreateKey, "Ask the player to create a hero.", NPCScript.Special_HeroCreate),
            new ScriptSpecialPageEntry(ScriptSpecialPageKind.ManageHero, NPCScript.HeroManageKey, "Open the hero management page.", NPCScript.Special_HeroManage),
        };

        private static readonly Dictionary<string, ScriptSpecialPageEntry> Index = BuildIndex();
        private static readonly Dictionary<ScriptSpecialPageKind, ScriptSpecialPageEntry> ByKind = BuildKindIndex();

        private static Dictionary<string, ScriptSpecialPageEntry> BuildIndex()
        {
            var index = new Dictionary<string, ScriptSpecialPageEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptSpecialPageEntry page in Entries)
            {
                if (index.ContainsKey(page.Key))
                {
#if DEBUG
                    throw new InvalidOperationException("Duplicate NPC special page key: " + page.Key);
#else
                    continue;
#endif
                }

                index.Add(page.Key, page);
            }

            return index;
        }

        private static Dictionary<ScriptSpecialPageKind, ScriptSpecialPageEntry> BuildKindIndex()
        {
            var index = new Dictionary<ScriptSpecialPageKind, ScriptSpecialPageEntry>();

            foreach (ScriptSpecialPageEntry page in Entries)
            {
                if (index.ContainsKey(page.Kind))
                {
#if DEBUG
                    throw new InvalidOperationException("Duplicate NPC special page kind: " + page.Kind);
#else
                    continue;
#endif
                }

                index.Add(page.Kind, page);
            }

            return index;
        }

        /// <summary>Page the engine reacts to, or null when the key is an ordinary page.</summary>
        public static ScriptSpecialPageEntry Find(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey)) return null;

            ScriptSpecialPageEntry page;

            return Index.TryGetValue(pageKey.Trim(), out page) ? page : null;
        }

        /// <summary>Page for a kind, so callers can reach one entry without scanning the list.</summary>
        public static ScriptSpecialPageEntry Find(ScriptSpecialPageKind kind)
        {
            ScriptSpecialPageEntry page;

            return ByKind.TryGetValue(kind, out page) ? page : null;
        }
    }
}
