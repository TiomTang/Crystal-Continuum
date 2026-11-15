using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirNetwork;
using Server.Library.MirDatabase;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.Scripting
{
    /// The player-facing actions: movement, money, items, progress, identity, page state, skills,
    /// pets and heroes.
    public partial class ScriptSegment
    {
        // ---------------------------------------------------------------- movement


        internal static ScriptOutcome Exec_Move(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                Map targetmap = Envir.GetMapByNameAndInstance(param[0]);
                if (targetmap == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out int x)) return ScriptOutcome.InvalidArguments;
                if (!int.TryParse(param[2], out int y)) return ScriptOutcome.InvalidArguments;

                var coords = new Point(x, y);

                if (coords.X > 0 && coords.Y > 0) player.Teleport(targetmap, coords);
                else player.TeleportRandom(200, 0, targetmap);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_InstanceMove(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int instanceId)) return ScriptOutcome.InvalidArguments;
                if (!int.TryParse(param[2], out int x)) return ScriptOutcome.InvalidArguments;
                if (!int.TryParse(param[3], out int y)) return ScriptOutcome.InvalidArguments;

                var targetmap = Envir.GetMapByNameAndInstance(param[0], instanceId);
                if (targetmap == null) return ScriptOutcome.Abort;
                player.Teleport(targetmap, new Point(x, y));
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupRecall(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.GroupMembers == null) return ScriptOutcome.Abort;

                for (int j = 0; j < player.GroupMembers.Count(); j++)
                {
                    player.GroupMembers[j].Teleport(player.CurrentMap, player.CurrentLocation);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupTeleport(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.GroupMembers == null) return ScriptOutcome.Abort;
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.InvalidArguments;
                if (!int.TryParse(param[2], out int x)) return ScriptOutcome.InvalidArguments;
                if (!int.TryParse(param[3], out int y)) return ScriptOutcome.InvalidArguments;

                var targetmap = Envir.GetMapByNameAndInstance(param[0], tempInt);
                if (targetmap == null) return ScriptOutcome.Abort;

                for (int j = 0; j < player.GroupMembers.Count(); j++)
                {
                    if (x == 0 || y == 0)
                    {
                        player.GroupMembers[j].TeleportRandom(200, 0, targetmap);
                    }
                    else
                    {
                        player.GroupMembers[j].Teleport(targetmap, new Point(x, y));
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TeleportGT(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            GTMap GTmap = null;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("You need to be in a Guild!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!player.MyGuild.HasGT)
            {
                player.ReceiveChat(GameLanguage.Get("You dont own a Guild Territory!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            GTmap = null;
            foreach (var gt in Envir.GTMapList)
            {
                if (gt.Index == player.MyGuild.GTIndex)
                {
                    GTmap = gt;
                    break;
                }
            }

            if (GTmap == null)
            {
                player.ReceiveChat(GameLanguage.Get("No GT's found, contact GM!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!(GTmap.Maps.Count > 0))
            {
                player.ReceiveChat(GameLanguage.Get("No GT Maps found, contact GM!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.GTBegin > Envir.Now)
            {
                player.ReceiveChat(GameLanguage.Get("Recent sale still pending."), ChatType.System);
                return ScriptOutcome.Abort;
            }

            var map = GTmap.Maps[0];

            if (map == null || map.Info.SafeZones == null || map.Info.SafeZones.Count == 0)
            {
                player.ReceiveChat(GameLanguage.Get("No Safezone found, contact GM!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            player.Teleport(map, map.Info.SafeZones[0].Location);

            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- currency


        internal static ScriptOutcome Exec_GiveGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.InvalidArguments;

                if (gold + player.Account.Gold >= uint.MaxValue)
                    gold = uint.MaxValue - player.Account.Gold;

                player.GainGold(gold);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.InvalidArguments;

                if (gold >= player.Account.Gold) gold = player.Account.Gold;

                player.Account.Gold -= gold;
                player.Enqueue(new S.LoseGold { Gold = gold });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveGuildGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.InvalidArguments;

                if (gold + player.MyGuild.Gold >= uint.MaxValue)
                    gold = uint.MaxValue - player.MyGuild.Gold;

                player.MyGuild.Gold += gold;
                player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 3, Amount = gold });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeGuildGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.InvalidArguments;

                if (gold >= player.MyGuild.Gold) gold = player.MyGuild.Gold;

                player.MyGuild.Gold -= gold;
                player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = gold });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveCredit(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint credit)) return ScriptOutcome.InvalidArguments;

                if (credit + player.Account.Credit >= uint.MaxValue)
                    credit = uint.MaxValue - player.Account.Credit;

                player.GainCredit(credit);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeCredit(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint credit)) return ScriptOutcome.InvalidArguments;

                if (credit >= player.Account.Credit) credit = player.Account.Credit;

                player.Account.Credit -= credit;
                player.Enqueue(new S.LoseCredit { Credit = credit });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GivePearls(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint pearls)) return ScriptOutcome.InvalidArguments;

                if (pearls + player.Info.PearlCount >= int.MaxValue)
                    pearls = (uint)(int.MaxValue - player.Info.PearlCount);

                player.IntelligentCreatureGainPearls((int)pearls);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakePearls(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint pearls)) return ScriptOutcome.InvalidArguments;

                if (pearls >= player.Info.PearlCount) pearls = (uint)player.Info.PearlCount;

                player.IntelligentCreatureLosePearls((int)pearls);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeConquestGold(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (player.MyGuild != null && player.MyGuild.Guildindex == conquest.GuildInfo.Owner)
                {
                    player.MyGuild.Gold += conquest.GuildInfo.GoldStorage;
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 3, Amount = conquest.GuildInfo.GoldStorage });
                    conquest.GuildInfo.GoldStorage = 0;
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- items


        internal static ScriptOutcome Exec_GiveItem(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (param.Count < 2 || !ushort.TryParse(param[1], out ushort count)) count = 1;

                var info = Envir.GetItemInfo(param[0]);

                if (info == null)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Failed to get ItemInfo: {0}, Page: {1}", param[0], seg.Key));
                    return ScriptOutcome.Pass;
                }

                while (count > 0)
                {
                    UserItem item = Envir.CreateFreshItem(info);

                    if (item == null)
                    {
                        MessageQueue.Enqueue(GameLanguage.Get("Failed to create UserItem: {0}, Page: {1}", param[0], seg.Key));
                        return ScriptOutcome.Abort;
                    }

                    if (item.Info.StackSize > count)
                    {
                        item.Count = count;
                        count = 0;
                    }
                    else
                    {
                        count -= item.Info.StackSize;
                        item.Count = item.Info.StackSize;
                    }

                    if (player.CanGainItem(item))
                        player.GainItem(item);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeItem(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (param.Count < 2 || !ushort.TryParse(param[1], out ushort count)) count = 1;
                var info = Envir.GetItemInfo(param[0]);

                ushort dura;
                bool checkDura = ushort.TryParse(param[2], out dura);

                if (info == null)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Failed to get ItemInfo: {0}, Page: {1}", param[0], seg.Key));
                    return ScriptOutcome.Pass;
                }

                for (int j = 0; j < player.Info.Inventory.Length; j++)
                {
                    UserItem item = player.Info.Inventory[j];
                    if (item == null) continue;
                    if (item.Info != info) continue;

                    if (checkDura)
                    {
                        if (item.CurrentDura < (dura * 1000)) continue;
                    }

                    if (count > item.Count)
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        player.Info.Inventory[j] = null;

                        count -= item.Count;
                        continue;
                    }

                    player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                    if (count == item.Count)
                        player.Info.Inventory[j] = null;
                    else
                        item.Count -= count;
                    break;
                }
                player.RefreshStats();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_UnequipItem(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var slot = param[0];

                for (int e = 0; e < player.Info.Equipment.Length; e++)
                {
                    var item = player.Info.Equipment[e];

                    if (item == null) continue;

                    var slotName = (EquipmentSlot)e;

                    if (!string.IsNullOrEmpty(slot) && slot.ToLower() != slotName.ToString().ToLower()) continue;

                    if (!player.CanRemoveItem(MirGridType.Inventory, item) || item.Cursed || item.WeddingRing != -1) continue;

                    for (int k = 0; k < player.Info.Inventory.Length; k++)
                    {
                        var freeSlot = player.Info.Inventory[k];

                        if (freeSlot != null) continue;

                        player.Info.Equipment[e] = null;
                        player.Info.Inventory[k] = item;

                        player.Report.ItemMoved(item, MirGridType.Equipment, MirGridType.Inventory, e, k);

                        break;
                    }
                }

                S.UserSlotsRefresh packet = new S.UserSlotsRefresh
                {
                    Inventory = new UserItem[player.Info.Inventory.Length],
                    Equipment = new UserItem[player.Info.Equipment.Length],
                };

                player.Info.Inventory.CopyTo(packet.Inventory, 0);
                player.Info.Equipment.CopyTo(packet.Equipment, 0);

                player.Enqueue(packet);

                player.RefreshStats();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Drop(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var path = param[0];
                var drops = new List<DropInfo>();
                DropInfo.Load(drops, "NPC", path, 0, false);

                foreach (var drop in drops)
                {
                    var reward = drop.AttemptDrop(player?.Stats[Stat.ItemDropRatePercent] ?? 0, player?.Stats[Stat.GoldDropRatePercent] ?? 0);

                    if (reward != null)
                    {
                        if (reward.Gold > 0)
                        {
                            player.GainGold(reward.Gold);
                        }

                        foreach (var dropItem in reward.Items)
                        {
                            UserItem item = Envir.CreateDropItem(dropItem);

                            if (item == null) continue;

                            if (player != null && player.Race == ObjectType.Player)
                            {
                                PlayerObject ob = (PlayerObject)player;

                                if (ob.CheckGroupQuestItem(item))
                                {
                                    continue;
                                }
                            }

                            if (drop.QuestRequired) continue;

                            if (player.CanGainItem(item))
                            {
                                player.GainItem(item);
                            }
                        }
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- progress


        internal static ScriptOutcome Exec_GiveExp(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                uint tempUint;
                if (!uint.TryParse(param[0], out tempUint)) return ScriptOutcome.InvalidArguments;
                player.GainExp(tempUint);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveHP(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                ChangeObjectHP(obj, tempInt);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveMP(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                player.ChangeMP(tempInt);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ChangeLevel(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!ushort.TryParse(param[0], out ushort tempuShort)) return ScriptOutcome.InvalidArguments;
                tempuShort = Math.Min(ushort.MaxValue, tempuShort);

                player.Level = tempuShort;
                player.Experience = 0;
                player.LevelUp();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RefreshEffects(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.SetLevelEffects();
                var p = new S.ObjectLevelEffects { ObjectID = player.ObjectID, LevelEffects = player.LevelEffects };
                player.Enqueue(p);
                player.Broadcast(p);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CanGainExp(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                bool.TryParse(param[0], out bool tempBool);
                player.CanGainExp = tempBool;
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- identity


        internal static ScriptOutcome Exec_SetPkPoint(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                player.PKPoints = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ReducePkPoint(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;

                player.PKPoints -= tempInt;
                if (player.PKPoints < 0) player.PKPoints = 0;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_IncreasePkPoint(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;
                player.PKPoints += tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ChangeGender(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                switch (player.Info.Gender)
                {
                    case MirGender.Male:
                        player.Info.Gender = MirGender.Female;
                        break;
                    case MirGender.Female:
                        player.Info.Gender = MirGender.Male;
                        break;
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ChangeHair(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (param.Count < 1)
                {
                    player.Info.Hair = (byte)Envir.Random.Next(0, 9);
                }
                else
                {
                    byte.TryParse(param[0], out byte tempByte);

                    if (tempByte >= 0 && tempByte <= 9)
                    {
                        player.Info.Hair = tempByte;
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ChangeClass(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out MirClass mirClass)) return ScriptOutcome.InvalidArguments;

                switch (mirClass)
                {
                    case MirClass.Warrior:
                        player.Info.Class = MirClass.Warrior;
                        break;
                    case MirClass.Taoist:
                        player.Info.Class = MirClass.Taoist;
                        break;
                    case MirClass.Wizard:
                        player.Info.Class = MirClass.Wizard;
                        break;
                    case MirClass.Assassin:
                        player.Info.Class = MirClass.Assassin;
                        break;
                    case MirClass.Archer:
                        player.Info.Class = MirClass.Archer;
                        break;
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- page state


        internal static ScriptOutcome Exec_Set(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                int flagIndex;
                uint onCheck;
                if (!int.TryParse(param[0], out flagIndex)) return ScriptOutcome.InvalidArguments;
                if (!uint.TryParse(param[1], out onCheck)) return ScriptOutcome.InvalidArguments;

                if (flagIndex < 0 || flagIndex >= Globals.FlagIndexCount) return ScriptOutcome.Abort;
                var flagIsOn = Convert.ToBoolean(onCheck);

                player.Info.Flags[flagIndex] = flagIsOn;

                for (int f = player.CurrentMap.NPCs.Count - 1; f >= 0; f--)
                {
                    if (Functions.InRange(player.CurrentMap.NPCs[f].CurrentLocation, player.CurrentLocation, Globals.DataRange))
                        player.CurrentMap.NPCs[f].CheckVisible(player);
                }

                if (flagIsOn) player.CheckNeedQuestFlag(flagIndex);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Param1(ScriptExecuteContext ctx)
        {
            ScriptPageState state = ctx.Call?.PageState;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null || state == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.InvalidArguments;

                state.Param1 = param[0];
                state.Param1Instance = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Param2(ScriptExecuteContext ctx)
        {
            ScriptPageState state = ctx.Call?.PageState;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null || state == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;

                state.Param2 = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Param3(ScriptExecuteContext ctx)
        {
            ScriptPageState state = ctx.Call?.PageState;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null || state == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.InvalidArguments;

                state.Param3 = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Mov(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                string value = param[0];
                seg.AddVariable(obj, value, param[1]);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Calc(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                int left;
                int right;

                bool resultLeft = int.TryParse(param[0], out left);
                bool resultRight = int.TryParse(param[2], out right);

                if (resultLeft && resultRight)
                {
                    try
                    {
                        int result = Calculate(param[1], left, right);
                        seg.AddVariable(obj, param[3].Replace("-", ""), result.ToString());
                    }
                    catch (ArgumentException)
                    {
                        MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[1], seg.Key));
                    }
                }
                else
                {
                    seg.AddVariable(obj, param[3].Replace("-", ""), param[0] + param[2]);
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- skills


        internal static ScriptOutcome Exec_GiveSkill(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                byte spellLevel = 0;

                Spell skill;
                if (!Enum.TryParse(param[0], true, out skill)) return ScriptOutcome.InvalidArguments;

                if (player.Info.Magics.Any(e => e.Spell == skill)) return ScriptOutcome.Pass;

                if (param.Count > 1)
                    spellLevel = byte.TryParse(param[1], out spellLevel) ? Math.Min((byte)3, spellLevel) : (byte)0;

                var magic = new UserMagic(skill) { Level = spellLevel };

                if (magic.Info == null) return ScriptOutcome.Abort;

                player.Info.Magics.Add(magic);
                player.SendMagicInfo(magic);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RemoveSkill(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out Spell skill)) return ScriptOutcome.InvalidArguments;

                if (!player.Info.Magics.Any(e => e.Spell == skill)) return ScriptOutcome.Pass;

                for (var j = player.Info.Magics.Count - 1; j >= 0; j--)
                {
                    if (player.Info.Magics[j].Spell != skill) continue;

                    player.Info.Magics.RemoveAt(j);
                    player.SendRemoveMagic(j);
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- pets


        internal static ScriptOutcome Exec_GivePet(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                byte petcount = 0;
                byte petlevel = 0;

                var monInfo = Envir.GetMonsterInfo(param[0]);
                if (monInfo == null) return ScriptOutcome.Abort;

                if (param.Count > 1)
                    petcount = byte.TryParse(param[1], out petcount) ? Math.Min((byte)5, petcount) : (byte)1;

                if (param.Count > 2)
                    petlevel = byte.TryParse(param[2], out petlevel) ? Math.Min((byte)7, petlevel) : (byte)0;

                for (int j = 0; j < petcount; j++)
                {
                    MonsterObject monster = MonsterObject.GetMonster(monInfo);
                    if (monster == null) return ScriptOutcome.Abort;
                    monster.PetLevel = petlevel;
                    monster.Master = player;
                    monster.MaxPetLevel = 7;
                    monster.Direction = player.Direction;
                    monster.ActionTime = Envir.Time + 1000;
                    monster.Spawn(player.CurrentMap, player.CurrentLocation);
                    player.Pets.Add(monster);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RemovePet(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                for (int c = player.Pets.Count - 1; c >= 0; c--)
                {
                    if (string.Compare(player.Pets[c].Info.Name, param[0], true) != 0) continue;

                    player.Pets[c].Die();
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ClearPets(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                for (int c = player.Pets.Count - 1; c >= 0; c--)
                {
                    player.Pets[c].DieNextTurn = true;
                }
            }
            return ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- heroes


        internal static ScriptOutcome Exec_ReviveHero(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            player.ReviveHero();
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SealHero(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            player.SealHero();
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DeleteHero(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            player.DeleteHero();
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HeroGiveSkill(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.Hero == null || player.Hero.Info == null) return ScriptOutcome.Abort;

                byte spellLevel = 0;

                Spell skill;
                if (!Enum.TryParse(param[0], true, out skill)) return ScriptOutcome.InvalidArguments;

                if (player.Hero.Info.Magics.Any(e => e.Spell == skill)) return ScriptOutcome.Pass;

                if (param.Count > 1)
                    spellLevel = byte.TryParse(param[1], out spellLevel) ? Math.Min((byte)3, spellLevel) : (byte)0;

                var magic = new UserMagic(skill) { Level = spellLevel };

                if (magic.Info == null) return ScriptOutcome.Abort;

                player.Hero.Info.Magics.Add(magic);
                player.Hero.SendMagicInfo(magic);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HeroRemoveSkill(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.Hero == null || player.Hero.Info == null) return ScriptOutcome.Abort;

                if (!Enum.TryParse(param[0], true, out Spell skill)) return ScriptOutcome.InvalidArguments;

                if (!player.Hero.Info.Magics.Any(e => e.Spell == skill)) return ScriptOutcome.Pass;

                for (var j = player.Hero.Info.Magics.Count - 1; j >= 0; j--)
                {
                    if (player.Hero.Info.Magics[j].Spell != skill) continue;

                    player.Hero.Info.Magics.RemoveAt(j);
                    player.Hero.SendRemoveMagic(j);
                }
            }
            return ScriptOutcome.Pass;
        }
    }
}
