using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        public void Buy(PlayerObject player, ulong index, ushort count)
        {
            UserItem goods = null;

            for (int i = 0; i < Goods.Count; i++)
            {
                if (Goods[i].UniqueID != index) continue;
                goods = Goods[i];
                break;
            }

            bool isUsed = false;
            bool isBuyBack = false;

            var callingNPC = NPCObject.Get(player.NPCObjectID);

            if (callingNPC != null)
            {
                if (goods == null)
                {
                    for (int i = 0; i < callingNPC.UsedGoods.Count; i++)
                    {
                        if (callingNPC.UsedGoods[i].UniqueID != index) continue;
                        goods = callingNPC.UsedGoods[i];
                        isUsed = true;
                        break;
                    }
                }

                if (goods == null)
                {
                    if (!callingNPC.BuyBack.ContainsKey(player.Name)) callingNPC.BuyBack[player.Name] = new List<UserItem>();
                    for (int i = 0; i < callingNPC.BuyBack[player.Name].Count; i++)
                    {
                        if (callingNPC.BuyBack[player.Name][i].UniqueID != index) continue;
                        goods = callingNPC.BuyBack[player.Name][i];
                        isBuyBack = true;
                        break;
                    }
                }
            }

            if (goods == null || count == 0 || count > goods.Info.StackSize) return;

            if (isBuyBack || isUsed)
                count = Math.Min(count, goods.Count);
            if (count == 0) return;
            if ((isBuyBack || isUsed) && count < goods.Count && goods.Slots.Any(slot => slot != null)) return;

            // Price a copy so an unsuccessful purchase cannot change shared stock.
            UserItem pricedItem = goods.Clone();
            pricedItem.Count = count;

            double totalCost = pricedItem.Price() * (double)PriceRate(player);
            if (totalCost < 0 || totalCost > uint.MaxValue || double.IsNaN(totalCost)) return;
            uint cost = (uint)totalCost;
            uint baseCost = (uint)Math.Clamp(pricedItem.Price() * (double)PriceRate(player, true), 0, uint.MaxValue);

            if (player.NPCPage.Key.ToUpper() == PearlBuyKey)//pearl currency
            {
                if (cost > player.Info.PearlCount) return;
            }
            else if (cost > player.Account.Gold) return;

            UserItem item = (isBuyBack || isUsed) ? pricedItem : Envir.CreateFreshItem(goods.Info);
            if ((isBuyBack || isUsed) && count < goods.Count)
                item.UniqueID = Envir.CreateFreshItem(goods.Info).UniqueID;
            item.Count = count;

            if (!player.CanGainItem(item)) return;

            if (player.NPCPage.Key.ToUpper() == PearlBuyKey)
            {
                player.IntelligentCreatureLosePearls((int)cost);
            }
            else
            {
                player.Account.Gold -= cost;
                player.Enqueue(new S.LoseGold { Gold = cost });

                if (callingNPC != null && callingNPC.Conq != null)
                {
                    callingNPC.Conq.GuildInfo.GoldStorage += (cost - baseCost);
                }
            }

            player.GainItem(item);

            if (isUsed)
            {
                goods.Count -= count;
                if (goods.Count == 0) callingNPC.UsedGoods.Remove(goods);

                List<UserItem> newGoodsList = new List<UserItem>();
                newGoodsList.AddRange(Goods);
                newGoodsList.AddRange(callingNPC.UsedGoods);

                callingNPC.NeedSave = true;

                player.SendNPCGoods(newGoodsList, PriceRate(player), player.NPCPage.Key.ToUpper() == BuyUsedKey ? PanelType.BuySub : PanelType.Buy, Settings.GoodsHideAddedStats);
            }

            if (isBuyBack)
            {
                goods.Count -= count;
                if (goods.Count == 0) callingNPC.BuyBack[player.Name].Remove(goods);
                player.SendNPCGoods(callingNPC.BuyBack[player.Name], PriceRate(player), PanelType.Buy);
            }
        }
        public void Sell(PlayerObject player, UserItem item)
        {
            /* Handle Item Sale */
        }
        public void Craft(PlayerObject player, ulong index, ushort count, int[] slots)
        {
            S.CraftItem p = new S.CraftItem();

            RecipeInfo recipe = null;

            for (int i = 0; i < CraftGoods.Count; i++)
            {
                if (CraftGoods[i].Item.UniqueID != index) continue;
                recipe = CraftGoods[i];
                break;
            }

            if (recipe == null || !recipe.CanCraft(player) || slots == null || slots.Length > player.Info.Inventory.Length ||
                slots.Any(slot => slot < 0 || slot >= player.Info.Inventory.Length) ||
                slots.Distinct().Count() != slots.Length)
            {
                player.Enqueue(p);
                return;
            }

            UserItem goods = recipe.Item;

            if (goods == null || goods.Count == 0 || count == 0 || count > goods.Info.StackSize)
            {
                player.Enqueue(p);
                return;
            }

            ulong totalCost = (ulong)recipe.Gold * count;
            if (totalCost > player.Account.Gold)
            {
                player.Enqueue(p);
                return;
            }

            bool hasItems = true;

            List<int> usedSlots = new List<int>();

            //Check Tools
            foreach (var tool in recipe.Tools)
            {
                bool found = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != tool.Info) continue;

                    usedSlots.Add(slot);

                    if ((uint)Math.Floor(item.CurrentDura / 1000M) < count)
                    {
                        hasItems = false;
                        break;
                    }

                    found = true;
                    break;
                }

                if (!found) hasItems = false;
                if (!hasItems)
                {
                    break;
                }
            }

            //Check Ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Count * count > ingredient.Info.StackSize)
                {
                    player.Enqueue(p);
                    return;
                }

                ushort amount = (ushort)(ingredient.Count * count);

                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != ingredient.Info || item.RentalInformation != null) continue;

                    usedSlots.Add(slot);

                    if (ingredient.CurrentDura < ingredient.MaxDura && ingredient.CurrentDura > item.CurrentDura)
                    {
                        hasItems = false;
                        break;
                    }

                    if (amount > item.Count)
                    {
                        hasItems = false;
                        break;
                    }

                    amount = 0;
                    break;
                }

                if (amount > 0)
                {
                    hasItems = false;
                    break;
                }
            }

            if (!hasItems || usedSlots.Count != (recipe.Tools.Count + recipe.Ingredients.Count))
            {
                player.Enqueue(p);
                return;
            }

            if (count > (goods.Info.StackSize / goods.Count) || count < 1)
            {
                player.Enqueue(p);
                return;
            }

            UserItem craftedItem = Envir.CreateFreshItem(goods.Info);
            craftedItem.Count = (ushort)(goods.Count * count);

            if (!player.CanGainItem(craftedItem))
            {
                player.Enqueue(p);
                return;
            }

            List<int> usedSlots2 = new List<int>();

            //Use Tool Durability
            foreach (var tool in recipe.Tools)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots2.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != tool.Info) continue;

                    usedSlots2.Add(slot);

                    player.DamageItem(item, (int)(count * 1000), true);

                    break;
                }
            }

            //Take Ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                ushort amount = (ushort)(ingredient.Count * count);

                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots2.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != ingredient.Info || item.RentalInformation != null) continue;

                    usedSlots2.Add(slot);

                    if (item.Count > amount)
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = amount });
                        player.Info.Inventory[slot].Count -= amount;
                        break;
                    }
                    else
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        amount -= item.Count;
                        player.Info.Inventory[slot] = null;
                    }

                    break;
                }
            }

            //Take Gold
            player.Account.Gold -= (uint)totalCost;
            player.Enqueue(new S.LoseGold { Gold = (uint)totalCost });

            if (Envir.Random.Next(100) >= recipe.Chance + player.Stats[Stat.CraftRatePercent])
            {
                player.ReceiveChat(GameLanguage.Get("Crafting attempt failed."), ChatType.System);
            }
            else
            {
                //Give Item
                player.GainItem(craftedItem);
            }

            p.Success = true;
            player.Enqueue(p);
        }
    }
}
