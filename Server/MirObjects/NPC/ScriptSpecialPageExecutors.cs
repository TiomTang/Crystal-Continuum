using Server.Scripting;
using System;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects
{
    /// <summary>
    /// What the engine does on its own when a page key asks for a panel. Each handler is registered on
    /// the page in ScriptSpecialPageCatalog, so the editor offers exactly the pages the loader reacts to.
    /// </summary>
    public partial class NPCScript
    {
        internal static void Special_Buy(NPCScript script, PlayerObject player, ScriptPage page)
        {
            var sentGoods = new List<UserItem>(script.Goods);

            for (int i = 0; i < script.Goods.Count; i++)
                player.CheckItem(script.Goods[i]);

            if (Settings.GoodsOn)
            {
                var callingNPC = NPCObject.Get(player.NPCObjectID);

                if (callingNPC != null)
                {
                    for (int i = 0; i < callingNPC.UsedGoods.Count; i++)
                        player.CheckItem(callingNPC.UsedGoods[i]);

                    sentGoods.AddRange(callingNPC.UsedGoods);
                }
            }

            player.SendNPCGoods(sentGoods, script.PriceRate(player), PanelType.Buy, Settings.GoodsHideAddedStats);

            if (ScriptSpecialPageCatalog.IsPage(page.Key, ScriptSpecialPageKind.BuySell))
            {
                player.Enqueue(new S.NPCSell());
            }
        }

        internal static void Special_BuyNew(NPCScript script, PlayerObject player, ScriptPage page)
        {
            var sentGoods = new List<UserItem>(script.Goods);

            for (int i = 0; i < script.Goods.Count; i++)
                player.CheckItem(script.Goods[i]);

            player.SendNPCGoods(sentGoods, script.PriceRate(player), PanelType.Buy, Settings.GoodsHideAddedStats);

            if (ScriptSpecialPageCatalog.IsPage(page.Key, ScriptSpecialPageKind.BuySellNew))
            {
                player.Enqueue(new S.NPCSell());
            }
        }

        internal static void Special_Sell(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCSell());
        }

        internal static void Special_Repair(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCRepair { Rate = script.PriceRate(player) });
        }

        internal static void Special_SRepair(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCSRepair { Rate = script.PriceRate(player) });
        }

        internal static void Special_Craft(NPCScript script, PlayerObject player, ScriptPage page)
        {
            for (int i = 0; i < script.CraftGoods.Count; i++)
                player.CheckItemInfo(script.CraftGoods[i].Item.Info);

            player.SendNPCGoods((from x in script.CraftGoods where x.CanCraft(player) select x.Item).ToList(), script.PriceRate(player), PanelType.Craft);
        }

        internal static void Special_Refine(NPCScript script, PlayerObject player, ScriptPage page)
        {
            if (player.Info.CurrentRefine != null)
            {
                player.ReceiveChat(GameLanguage.Get("You're already refining an item."), ChatType.System);
                player.Enqueue(new S.NPCRefine { Rate = (Settings.RefineCost), Refining = true });
                return;
            }

            player.Enqueue(new S.NPCRefine { Rate = (Settings.RefineCost), Refining = false });
        }

        internal static void Special_RefineCheck(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCCheckRefine());
        }

        internal static void Special_RefineCollect(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.CollectRefine();
        }

        internal static void Special_ReplaceWeddingRing(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCReplaceWedRing { Rate = Settings.ReplaceWedRingCost });
        }

        internal static void Special_Storage(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.ResetStorageUnlock();
            player.SendStorage();
            player.Enqueue(new S.NPCStorage());
        }

        internal static void Special_BuyBack(NPCScript script, PlayerObject player, ScriptPage page)
        {
            if (!Settings.GoodsOn) return;

            var callingNPC = NPCObject.Get(player.NPCObjectID);

            if (callingNPC == null) return;

            if (!callingNPC.BuyBack.ContainsKey(player.Name)) callingNPC.BuyBack[player.Name] = new List<UserItem>();

            for (int i = 0; i < callingNPC.BuyBack[player.Name].Count; i++)
            {
                player.CheckItem(callingNPC.BuyBack[player.Name][i]);
            }

            player.SendNPCGoods(callingNPC.BuyBack[player.Name], script.PriceRate(player), PanelType.Buy);
        }

        internal static void Special_BuyUsed(NPCScript script, PlayerObject player, ScriptPage page)
        {
            if (!Settings.GoodsOn) return;

            var callingNPC = NPCObject.Get(player.NPCObjectID);

            if (callingNPC == null) return;

            for (int i = 0; i < callingNPC.UsedGoods.Count; i++)
                player.CheckItem(callingNPC.UsedGoods[i]);

            player.SendNPCGoods(callingNPC.UsedGoods, script.PriceRate(player), PanelType.BuySub, Settings.GoodsHideAddedStats);
        }

        internal static void Special_Consign(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCConsign());
        }

        internal static void Special_Market(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.UserMatch = false;
            player.GetMarket(string.Empty, ItemType.Nothing);
        }

        internal static void Special_GuildCreate(NPCScript script, PlayerObject player, ScriptPage page)
        {
            if (player.Info.Level < Settings.Guild_RequiredLevel)
            {
                player.ReceiveChat(GameLanguage.Get("You have to be at least level {0} to create a guild.", Settings.Guild_RequiredLevel), ChatType.System);
            }
            else if (player.MyGuild == null)
            {
                player.CanCreateGuild = true;
                player.Enqueue(new S.GuildNameRequest());
            }
            else
                player.ReceiveChat(GameLanguage.Get("You are already part of a guild."), ChatType.System);
        }

        internal static void Special_RequestWar(NPCScript script, PlayerObject player, ScriptPage page)
        {
            if (player.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("You are not in a guild."), ChatType.System);
                return;
            }

            if (player.MyGuildRank != player.MyGuild.Ranks[0])
            {
                player.ReceiveChat(GameLanguage.Get("You must be the leader to request a war."), ChatType.System);
                return;
            }

            player.Enqueue(new S.GuildRequestWar());
        }

        internal static void Special_SendParcel(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.MailSendRequest());
        }

        internal static void Special_CollectParcel(NPCScript script, PlayerObject player, ScriptPage page)
        {
            sbyte result = 0;

            if (player.GetMailAwaitingCollectionAmount() < 1)
            {
                result = -1;
            }
            else
            {
                foreach (var mail in player.Info.Mail)
                {
                    if (mail.Parcel) mail.Collected = true;
                }
            }

            player.Enqueue(new S.ParcelCollected { Result = result });
            player.GetMail();
        }

        internal static void Special_Awakening(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCAwakening());
        }

        internal static void Special_Disassemble(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCDisassemble());
        }

        internal static void Special_Downgrade(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCDowngrade());
        }

        internal static void Special_Reset(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.Enqueue(new S.NPCReset());
        }

        internal static void Special_PearlBuy(NPCScript script, PlayerObject player, ScriptPage page)
        {
            for (int i = 0; i < script.Goods.Count; i++)
                player.CheckItem(script.Goods[i]);

            player.Enqueue(new S.NPCPearlGoods { List = script.Goods, Rate = script.PriceRate(player), Type = PanelType.Buy });
        }

        internal static void Special_HeroCreate(NPCScript script, PlayerObject player, ScriptPage page)
        {
            if (player.Info.Level < Settings.Hero_RequiredLevel)
            {
                player.ReceiveChat(GameLanguage.Get("You have to be at least level {0} to create a hero.", Settings.Hero_RequiredLevel), ChatType.System);
                return;
            }

            player.CanCreateHero = true;
            player.Enqueue(new S.HeroCreateRequest()
            {
                CanCreateClass = Settings.Hero_CanCreateClass
            });
        }

        internal static void Special_HeroManage(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.ManageHeroes();
        }

        internal static void Special_GuildTerritory(NPCScript script, PlayerObject player, ScriptPage page)
        {
            player.GetGuildTerritories(0);
        }
    }
}
