using System;

namespace Server.MirObjects
{
    /// <summary>
    /// A page key the engine reacts to on its own: opening a panel, starting a guild request and so on.
    /// The keys come straight from the NPCScript constants so the editor, the docs and the loader can
    /// never drift apart.
    /// </summary>
    public sealed class ScriptSpecialPage
    {
        public readonly string Key;
        public readonly string Name;
        public readonly string Description;

        public ScriptSpecialPage(string key, string name, string description)
        {
            Key = key;
            Name = name;
            Description = description;
        }
    }

    public static class ScriptSpecialPages
    {
        public static readonly ScriptSpecialPage[] Entries =
        {
            new ScriptSpecialPage(NPCScript.MainKey, "Main", "Entry page of the script."),
            new ScriptSpecialPage(NPCScript.BuyKey, "Buy", "Open the buy panel with the goods listed in [Trade]."),
            new ScriptSpecialPage(NPCScript.BuySellKey, "BuySell", "Open the buy panel and the sell panel together."),
            new ScriptSpecialPage(NPCScript.SellKey, "Sell", "Open the sell panel."),
            new ScriptSpecialPage(NPCScript.BuyNewKey, "BuyNew", "Open the buy panel without asking for player goods."),
            new ScriptSpecialPage(NPCScript.BuySellNewKey, "BuySellNew", "Open the buy panel and the sell panel without asking for player goods."),
            new ScriptSpecialPage(NPCScript.BuyBackKey, "BuyBack", "Open the buy back list."),
            new ScriptSpecialPage(NPCScript.BuyUsedKey, "BuyUsed", "Open the used goods panel."),
            new ScriptSpecialPage(NPCScript.RepairKey, "Repair", "Open the repair panel."),
            new ScriptSpecialPage(NPCScript.SRepairKey, "SRepair", "Open the special repair panel."),
            new ScriptSpecialPage(NPCScript.RefineKey, "Refine", "Open the refine panel."),
            new ScriptSpecialPage(NPCScript.RefineCheckKey, "RefineCheck", "Check the outcome of a refinement."),
            new ScriptSpecialPage(NPCScript.RefineCollectKey, "RefineCollect", "Collect the refined item."),
            new ScriptSpecialPage(NPCScript.ReplaceWedRingKey, "ReplaceWeddingRing", "Replace the wedding ring."),
            new ScriptSpecialPage(NPCScript.StorageKey, "Storage", "Open the storage panel."),
            new ScriptSpecialPage(NPCScript.ConsignKey, "Consign", "Open the consignment panel."),
            new ScriptSpecialPage(NPCScript.MarketKey, "Market", "Open the market panel."),
            new ScriptSpecialPage(NPCScript.CraftKey, "Craft", "Open the crafting panel with the recipes listed in [Recipe]."),
            new ScriptSpecialPage(NPCScript.PearlBuyKey, "PearlBuy", "Open the pearl shop."),
            new ScriptSpecialPage(NPCScript.GuildCreateKey, "CreateGuild", "Ask the player to create a guild."),
            new ScriptSpecialPage(NPCScript.RequestWarKey, "RequestWar", "Ask the player to request a castle war."),
            new ScriptSpecialPage(NPCScript.GuildTerritoryKey, "GuildTerritory", "Open the guild territory page."),
            new ScriptSpecialPage(NPCScript.SendParcelKey, "SendParcel", "Open the send parcel dialog."),
            new ScriptSpecialPage(NPCScript.CollectParcelKey, "CollectParcel", "Collect a waiting parcel."),
            new ScriptSpecialPage(NPCScript.AwakeningKey, "Awakening", "Open the awakening panel."),
            new ScriptSpecialPage(NPCScript.DisassembleKey, "Disassemble", "Open the disassemble panel."),
            new ScriptSpecialPage(NPCScript.DowngradeKey, "Downgrade", "Open the downgrade panel."),
            new ScriptSpecialPage(NPCScript.ResetKey, "Reset", "Open the reset panel."),
            new ScriptSpecialPage(NPCScript.HeroCreateKey, "CreateHero", "Ask the player to create a hero."),
            new ScriptSpecialPage(NPCScript.HeroManageKey, "ManageHero", "Open the hero management page."),
        };
    }
}
