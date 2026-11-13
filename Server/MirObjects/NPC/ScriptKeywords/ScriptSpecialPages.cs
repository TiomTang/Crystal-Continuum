using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>
    /// A page key the engine reacts to on its own: opening a panel, starting a guild request and so on.
    /// The key comes from the NPCScript constants and the handler from NPCScript.Specials.cs, so the
    /// editor, the docs and the loader read one list instead of keeping a switch of their own.
    /// </summary>
    public sealed class ScriptSpecialPage
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string Description;

        /// <summary>Null when the page needs no extra work beyond showing its text.</summary>
        public readonly Action<NPCScript, PlayerObject, NPCPage> Executor;

        public ScriptSpecialPage(string key, string name, string description, Action<NPCScript, PlayerObject, NPCPage> executor = null)
        {
            Key = key;
            Name = name;
            Description = description;
            Executor = executor;
        }
    }

    public static class ScriptSpecialPages
    {
        public static readonly ScriptSpecialPage[] Entries =
        {
            new ScriptSpecialPage(NPCScript.MainKey, "Main", "Entry page of the script."),
            new ScriptSpecialPage(NPCScript.BuyKey, "Buy", "Open the buy panel with the goods listed in [Trade].", NPCScript.Special_Buy),
            new ScriptSpecialPage(NPCScript.BuySellKey, "BuySell", "Open the buy panel and the sell panel together.", NPCScript.Special_Buy),
            new ScriptSpecialPage(NPCScript.SellKey, "Sell", "Open the sell panel.", NPCScript.Special_Sell),
            new ScriptSpecialPage(NPCScript.BuyNewKey, "BuyNew", "Open the buy panel without asking for player goods.", NPCScript.Special_BuyNew),
            new ScriptSpecialPage(NPCScript.BuySellNewKey, "BuySellNew", "Open the buy panel and the sell panel without asking for player goods.", NPCScript.Special_BuyNew),
            new ScriptSpecialPage(NPCScript.BuyBackKey, "BuyBack", "Open the buy back list.", NPCScript.Special_BuyBack),
            new ScriptSpecialPage(NPCScript.BuyUsedKey, "BuyUsed", "Open the used goods panel.", NPCScript.Special_BuyUsed),
            new ScriptSpecialPage(NPCScript.RepairKey, "Repair", "Open the repair panel.", NPCScript.Special_Repair),
            new ScriptSpecialPage(NPCScript.SRepairKey, "SRepair", "Open the special repair panel.", NPCScript.Special_SRepair),
            new ScriptSpecialPage(NPCScript.RefineKey, "Refine", "Open the refine panel.", NPCScript.Special_Refine),
            new ScriptSpecialPage(NPCScript.RefineCheckKey, "RefineCheck", "Check the outcome of a refinement.", NPCScript.Special_RefineCheck),
            new ScriptSpecialPage(NPCScript.RefineCollectKey, "RefineCollect", "Collect the refined item.", NPCScript.Special_RefineCollect),
            new ScriptSpecialPage(NPCScript.ReplaceWedRingKey, "ReplaceWeddingRing", "Replace the wedding ring.", NPCScript.Special_ReplaceWeddingRing),
            new ScriptSpecialPage(NPCScript.StorageKey, "Storage", "Open the storage panel.", NPCScript.Special_Storage),
            new ScriptSpecialPage(NPCScript.ConsignKey, "Consign", "Open the consignment panel.", NPCScript.Special_Consign),
            new ScriptSpecialPage(NPCScript.MarketKey, "Market", "Open the market panel.", NPCScript.Special_Market),
            new ScriptSpecialPage(NPCScript.CraftKey, "Craft", "Open the crafting panel with the recipes listed in [Recipe].", NPCScript.Special_Craft),
            new ScriptSpecialPage(NPCScript.PearlBuyKey, "PearlBuy", "Open the pearl shop.", NPCScript.Special_PearlBuy),
            new ScriptSpecialPage(NPCScript.GuildCreateKey, "CreateGuild", "Ask the player to create a guild.", NPCScript.Special_GuildCreate),
            new ScriptSpecialPage(NPCScript.RequestWarKey, "RequestWar", "Ask the player to request a castle war.", NPCScript.Special_RequestWar),
            new ScriptSpecialPage(NPCScript.GuildTerritoryKey, "GuildTerritory", "Open the guild territory page.", NPCScript.Special_GuildTerritory),
            new ScriptSpecialPage(NPCScript.SendParcelKey, "SendParcel", "Open the send parcel dialog.", NPCScript.Special_SendParcel),
            new ScriptSpecialPage(NPCScript.CollectParcelKey, "CollectParcel", "Collect a waiting parcel.", NPCScript.Special_CollectParcel),
            new ScriptSpecialPage(NPCScript.AwakeningKey, "Awakening", "Open the awakening panel.", NPCScript.Special_Awakening),
            new ScriptSpecialPage(NPCScript.DisassembleKey, "Disassemble", "Open the disassemble panel.", NPCScript.Special_Disassemble),
            new ScriptSpecialPage(NPCScript.DowngradeKey, "Downgrade", "Open the downgrade panel.", NPCScript.Special_Downgrade),
            new ScriptSpecialPage(NPCScript.ResetKey, "Reset", "Open the reset panel.", NPCScript.Special_Reset),
            new ScriptSpecialPage(NPCScript.HeroCreateKey, "CreateHero", "Ask the player to create a hero.", NPCScript.Special_HeroCreate),
            new ScriptSpecialPage(NPCScript.HeroManageKey, "ManageHero", "Open the hero management page.", NPCScript.Special_HeroManage),
        };

        private static readonly Dictionary<string, ScriptSpecialPage> Index = BuildIndex();

        private static Dictionary<string, ScriptSpecialPage> BuildIndex()
        {
            var index = new Dictionary<string, ScriptSpecialPage>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptSpecialPage page in Entries) index[page.Key] = page;

            return index;
        }

        /// <summary>Page the engine reacts to, or null when the key is an ordinary page.</summary>
        public static ScriptSpecialPage Find(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey)) return null;

            ScriptSpecialPage page;

            return Index.TryGetValue(pageKey.Trim(), out page) ? page : null;
        }
    }
}
