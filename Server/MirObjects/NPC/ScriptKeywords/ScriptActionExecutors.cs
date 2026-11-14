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

namespace Server.MirObjects
{
    /// <summary>
    /// What each #ACT action does. Keyed by ScriptActionKind, registered on the entry in
    /// ScriptActionCatalog; Pass moves on to the next action, Fail keeps going, Abort stops the list.
    /// </summary>
    public partial class NPCSegment
    {
        /// <summary>Both player and monster expose ChangeHP through their own base, not MapObject.</summary>
        private static void ChangeObjectHP(MapObject obj, int amount)
        {
            var player = obj as PlayerObject;

            if (player != null) player.ChangeHP(amount);
            else ((MonsterObject)obj).ChangeHP(amount);
        }

        internal static ScriptOutcome Exec_Move(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                Map targetmap = Envir.GetMapByNameAndInstance(param[0]);
                if (targetmap == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out int x)) return ScriptOutcome.Abort;
                if (!int.TryParse(param[2], out int y)) return ScriptOutcome.Abort;

                var coords = new Point(x, y);

                if (coords.X > 0 && coords.Y > 0) player.Teleport(targetmap, coords);
                else player.TeleportRandom(200, 0, targetmap);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_InstanceMove(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int instanceId)) return ScriptOutcome.Abort;
                if (!int.TryParse(param[2], out int x)) return ScriptOutcome.Abort;
                if (!int.TryParse(param[3], out int y)) return ScriptOutcome.Abort;

                var targetmap = Envir.GetMapByNameAndInstance(param[0], instanceId);
                if (targetmap == null) return ScriptOutcome.Abort;
                player.Teleport(targetmap, new Point(x, y));
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_BuyGT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null || player.MyGuildRank == null) return ScriptOutcome.Abort;
            if (player.MyGuildRank != player.MyGuild.Ranks[0])
            {
                player.ReceiveChat(GameLanguage.Get("You need to be Guild Leader!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.Gold < Settings.BuyGTGold)
            {
                player.ReceiveChat(GameLanguage.Get("Insufficient Guild funds!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.HasGT)
            {
                player.ReceiveChat(GameLanguage.Get("You already own a Guild Territory!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            GTMap GTmap = null;
            foreach (var gt in Envir.GTMapList)
            {
                if (gt.Owner == "None")
                {
                    GTmap = gt;
                    break;
                }
            }

            if (GTmap == null)
            {
                player.ReceiveChat(GameLanguage.Get("No Guild Territory's available!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            player.MyGuild.Gold -= (uint)Settings.BuyGTGold;
            player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)Settings.BuyGTGold });
            player.ReceiveChat(GameLanguage.Get("You rented a Guild Territory!"), ChatType.System);
            player.MyGuild.GTIndex = GTmap.Index;
            player.MyGuild.GTRent = DateTime.Now.AddDays(Settings.GTDays);
            player.MyGuild.GTKey = Envir.Random.Next(100, int.MaxValue - 100);
            player.MyGuild.GTPrice = 0;
            GTmap.Owner = player.MyGuild.Name;
            GTmap.Leader = player.MyGuild.Ranks[0].Members[0].Name;
            if (player.MyGuild.Ranks[0].Members.Count > 1)
                GTmap.Leader = player.MyGuild.Ranks[0].Members[1].Name;
            GTmap.Price = 0;
            GTmap.Key = player.MyGuild.GTKey;

            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TeleportGT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_ExtendGT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            GTMap GTmap = null;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null || player.MyGuildRank == null) return ScriptOutcome.Abort;
            if (player.MyGuildRank != player.MyGuild.Ranks[0])
            {
                player.ReceiveChat(GameLanguage.Get("You need to be Guild Leader!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (player.MyGuild.Gold < Settings.ExtendGT)
            {
                player.ReceiveChat(GameLanguage.Get("Insufficient funds!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            GTmap = null;
            foreach (var gt in Envir.GTMapList)
            {
                if (gt.Owner == player.MyGuild.Name)
                {
                    GTmap = gt;
                    break;
                }
            }

            if (GTmap == null)
            {
                player.ReceiveChat(GameLanguage.Get("You dont own any GT!"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            player.MyGuild.Gold -= (uint)Settings.ExtendGT;
            player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)Settings.ExtendGT });

            player.MyGuild.GTRent = player.MyGuild.GTRent.AddDays(Settings.GTDays);
            GTmap.Price = 10000000;
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.Abort;

                if (gold + player.Account.Gold >= uint.MaxValue)
                    gold = uint.MaxValue - player.Account.Gold;

                player.GainGold(gold);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.Abort;

                if (gold >= player.Account.Gold) gold = player.Account.Gold;

                player.Account.Gold -= gold;
                player.Enqueue(new S.LoseGold { Gold = gold });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveGuildGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.Abort;

                if (gold + player.MyGuild.Gold >= uint.MaxValue)
                    gold = uint.MaxValue - player.MyGuild.Gold;

                player.MyGuild.Gold += gold;
                player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 3, Amount = gold });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeGuildGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint gold)) return ScriptOutcome.Abort;

                if (gold >= player.MyGuild.Gold) gold = player.MyGuild.Gold;

                player.MyGuild.Gold -= gold;
                player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = gold });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveCredit(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint credit)) return ScriptOutcome.Abort;

                if (credit + player.Account.Credit >= uint.MaxValue)
                    credit = uint.MaxValue - player.Account.Credit;

                player.GainCredit(credit);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeCredit(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint credit)) return ScriptOutcome.Abort;

                if (credit >= player.Account.Credit) credit = player.Account.Credit;

                player.Account.Credit -= credit;
                player.Enqueue(new S.LoseCredit { Credit = credit });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GivePearls(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint pearls)) return ScriptOutcome.Abort;

                if (pearls + player.Info.PearlCount >= int.MaxValue)
                    pearls = (uint)(int.MaxValue - player.Info.PearlCount);

                player.IntelligentCreatureGainPearls((int)pearls);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakePearls(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!uint.TryParse(param[0], out uint pearls)) return ScriptOutcome.Abort;

                if (pearls >= player.Info.PearlCount) pearls = (uint)player.Info.PearlCount;

                player.IntelligentCreatureLosePearls((int)pearls);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveItem(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_GiveExp(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                uint tempUint;
                if (!uint.TryParse(param[0], out tempUint)) return ScriptOutcome.Abort;
                player.GainExp(tempUint);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GivePet(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_AddNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                if (File.ReadAllLines(tempString).All(t => player.Name != t))
                {
                    using (var line = File.AppendText(tempString))
                    {
                        line.WriteLine(player.Name);
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                if (player.MyGuild == null) return ScriptOutcome.Pass;
                if (File.ReadAllLines(tempString).All(t => player.MyGuild.Name != t))
                {
                    using (var line = File.AppendText(tempString))
                    {
                        line.WriteLine(player.MyGuild.Name);
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DelNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                if (!File.Exists(tempString)) return ScriptOutcome.Pass;
                File.WriteAllLines(tempString, File.ReadLines(tempString).Where(l => l != player.Name).ToList());
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DelGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild == null) return ScriptOutcome.Pass;
                var tempString = param[0];
                if (!File.Exists(tempString)) return ScriptOutcome.Pass;
                File.WriteAllLines(tempString, File.ReadLines(tempString).Where(l => l != player.MyGuild.Name).ToList());
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ClearNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = param[0];
                File.WriteAllLines(tempString, new string[] { });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ClearGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild == null) return ScriptOutcome.Pass;
                var tempString = param[0];
                File.WriteAllLines(tempString, new string[] { });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveHP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                ChangeObjectHP(obj, tempInt);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveMP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                player.ChangeMP(tempInt);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ChangeLevel(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!ushort.TryParse(param[0], out ushort tempuShort)) return ScriptOutcome.Abort;
                tempuShort = Math.Min(ushort.MaxValue, tempuShort);

                player.Level = tempuShort;
                player.Experience = 0;
                player.LevelUp();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SetPkPoint(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                player.PKPoints = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ReducePkPoint(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;

                player.PKPoints -= tempInt;
                if (player.PKPoints < 0) player.PKPoints = 0;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_IncreasePkPoint(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                player.PKPoints += tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ChangeGender(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out MirClass mirClass)) return ScriptOutcome.Abort;

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

        internal static ScriptOutcome Exec_LocalMessage(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                ChatType chatType;
                if (!Enum.TryParse(param[1], true, out chatType)) return ScriptOutcome.Abort;
                player.ReceiveChat(param[0], chatType);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GlobalMessage(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[1], true, out ChatType chatType)) return ScriptOutcome.Abort;

                var p = new S.Chat { Message = param[0], Type = chatType };
                Envir.Broadcast(p);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GiveSkill(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                byte spellLevel = 0;

                Spell skill;
                if (!Enum.TryParse(param[0], true, out skill)) return ScriptOutcome.Abort;

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
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out Spell skill)) return ScriptOutcome.Abort;

                if (!player.Info.Magics.Any(e => e.Spell == skill)) return ScriptOutcome.Pass;

                for (var j = player.Info.Magics.Count - 1; j >= 0; j--)
                {
                    if (player.Info.Magics[j].Spell != skill) continue;

                    player.Info.Magics.RemoveAt(j);
                    player.Enqueue(new S.RemoveMagic { PlaceId = j });
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Goto(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                DelayedAction action = new DelayedAction(DelayedType.NPC, -1, player.NPCObjectID, player.NPCScriptID, "[" + param[0] + "]");
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Call(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int scriptID)) return ScriptOutcome.Abort;

                var action = new DelayedAction(DelayedType.NPC, -1, player.NPCObjectID, scriptID, "[@MAIN]");
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Break(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                seg.Page.BreakFromSegments = true;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Close(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.NPCDialogClose = true;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Set(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                int flagIndex;
                uint onCheck;
                if (!int.TryParse(param[0], out flagIndex)) return ScriptOutcome.Abort;
                if (!uint.TryParse(param[1], out onCheck)) return ScriptOutcome.Abort;

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
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.Abort;

                seg.Param1 = param[0];
                seg.Param1Instance = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Param2(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;

                seg.Param2 = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Param3(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;

                seg.Param3 = tempInt;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Mongen(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (seg.Param1 == null || seg.Param2 == 0 || seg.Param3 == 0) return ScriptOutcome.Abort;
                if (!byte.TryParse(param[1], out byte tempByte)) return ScriptOutcome.Abort;

                Map targetmap = Envir.GetMapByNameAndInstance(seg.Param1, seg.Param1Instance);
                if (targetmap == null) return ScriptOutcome.Abort;

                var monInfo = Envir.GetMonsterInfo(param[0]);
                if (monInfo == null) return ScriptOutcome.Abort;

                for (int j = 0; j < tempByte; j++)
                {
                    MonsterObject monster = MonsterObject.GetMonster(monInfo);
                    if (monster == null) return ScriptOutcome.Abort;
                    monster.Direction = 0;
                    monster.ActionTime = Envir.Time + 1000;
                    monster.Spawn(targetmap, new Point(seg.Param2, seg.Param3));
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TimeRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = "";
                if (!long.TryParse(param[0], out long tempLong)) return ScriptOutcome.Abort;

                if (param[1].Length > 0) tempString = "[" + param[1] + "]";

                Map tempMap = player.CurrentMap;
                Point tempPoint = player.CurrentLocation;

                var action = new DelayedAction(DelayedType.NPC, Envir.Time + (tempLong * 1000), player.NPCObjectID, player.NPCScriptID, tempString, tempMap, tempPoint);
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TimeRecallGroup(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var tempString = "";
                if (player.GroupMembers == null) return ScriptOutcome.Abort;
                if (!long.TryParse(param[0], out long tempLong)) return ScriptOutcome.Abort;
                if (param[1].Length > 0) tempString = "[" + param[1] + "]";

                for (int j = 0; j < player.GroupMembers.Count(); j++)
                {
                    var groupMember = player.GroupMembers[j];

                    var action = new DelayedAction(DelayedType.NPC, Envir.Time + (tempLong * 1000), player.NPCObjectID, player.NPCScriptID, tempString, player.CurrentMap, player.CurrentLocation);
                    groupMember.ActionList.Add(action);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_BreakTimeRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                foreach (DelayedAction ac in player.ActionList.Where(u => u.Type == DelayedType.NPC))
                {
                    ac.FlaggedToRemove = true;
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DelayGoto(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!long.TryParse(param[0], out long tempLong)) return ScriptOutcome.Abort;

                var action = new DelayedAction(DelayedType.NPC, Envir.Time + (tempLong * 1000), player.NPCObjectID, player.NPCScriptID, "[" + param[1] + "]");
                player.ActionList.Add(action);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_MonClear(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.Abort;

                var targetmap = Envir.GetMapByNameAndInstance(param[0], tempInt);
                if (targetmap == null) return ScriptOutcome.Abort;

                foreach (var cell in targetmap.Cells)
                {
                    if (cell == null || cell.Objects == null) continue;

                    for (int j = 0; j < cell.Objects.Count(); j++)
                    {
                        MapObject ob = cell.Objects[j];

                        if (ob.Race != ObjectType.Monster) continue;
                        if (ob.Dead) continue;

                        if (!string.IsNullOrEmpty(param[2]) && string.Compare(param[2], ((MonsterObject)ob).Info.Name, true) != 0)
                            continue;

                        ob.Die();
                    }
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.GroupMembers == null) return ScriptOutcome.Abort;
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.Abort;
                if (!int.TryParse(param[2], out int x)) return ScriptOutcome.Abort;
                if (!int.TryParse(param[3], out int y)) return ScriptOutcome.Abort;

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

        internal static ScriptOutcome Exec_Mov(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_GiveBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out BuffType buffType) || !Enum.IsDefined(typeof(BuffType), buffType) ||
                !int.TryParse(param[1], out int duration) || duration < 0 || duration > int.MaxValue / Settings.Second)
                {
                    MessageQueue.Enqueue($"GiveBuff: invalid type or duration, Page: {seg.Key}");
                    return ScriptOutcome.Pass;
                }

                var stats = new Stats();
                bool valid = true;
                // Legacy positional flags are retained for script compatibility. BuffInfo owns buff properties and stacking.
                foreach (string value in param.Skip(2).Where(value => value.Contains('=')))
                {
                    string[] pair = value.Split('=');
                    if (pair.Length != 2 || !Enum.TryParse(pair[0], true, out Stat stat) ||
                    !Enum.IsDefined(typeof(Stat), stat) || !int.TryParse(pair[1], out int amount))
                    {
                        MessageQueue.Enqueue($"GiveBuff: invalid stat '{value}', Page: {seg.Key}");
                        valid = false;
                        break;
                    }
                    stats[stat] = amount;
                }

                if (valid) player.AddBuff(buffType, player, Settings.Second * duration, stats);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RemoveBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!Enum.IsDefined(typeof(BuffType), param[0])) return ScriptOutcome.Abort;

                BuffType bType = (BuffType)(byte)Enum.Parse(typeof(BuffType), param[0]);

                obj.RemoveBuff(bType);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddToGuild(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild != null) return ScriptOutcome.Abort;

                GuildObject guild = Envir.GetGuild(param[0]);

                if (guild == null)
                {
                    MessageQueue.Enqueue($"AddToGuild: guild '{param[0]}' not found. Player: {player.Name}, Page: {seg.Page?.Key}");
                    player.ReceiveChat(GameLanguage.Get("Guild {0} is not available. Please contact an administrator.", param[0]), ChatType.System);
                    return ScriptOutcome.Abort;
                }

                player.PendingGuildInvite = guild;
                player.GuildInvite(true);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RemoveFromGuild(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.MyGuild == null) return ScriptOutcome.Abort;

                if (player.MyGuildRank == null) return ScriptOutcome.Abort;

                if (player.MyGuild.Name == Settings.NewbieGuild) player.RemoveBuff(BuffType.Newbie);
                if (player.HasBuff(BuffType.Guild)) player.RemoveBuff(BuffType.Guild);

                player.MyGuild.DeleteMember(player, player.Name);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RefreshEffects(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                bool.TryParse(param[0], out bool tempBool);
                player.CanGainExp = tempBool;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ComposeMail(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                ctx.State.Mail = new MailInfo(player.Info.Index, false)
                {
                    Sender = param[1],
                    Message = param[0]
                };
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddMailGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (ctx.State.Mail == null) return ScriptOutcome.Abort;

                uint.TryParse(param[0], out uint tempUint);

                ctx.State.Mail.Gold += tempUint;
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AddMailItem(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (ctx.State.Mail == null) return ScriptOutcome.Abort;
                if (ctx.State.Mail.Items.Count > 5) return ScriptOutcome.Abort;

                if (param.Count < 2 || !ushort.TryParse(param[1], out ushort count)) count = 1;

                var info = Envir.GetItemInfo(param[0]);

                if (info == null)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Failed to get ItemInfo: {0}, Page: {1}", param[0], seg.Key));
                    return ScriptOutcome.Pass;
                }

                while (count > 0 && ctx.State.Mail.Items.Count < 5)
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

                    ctx.State.Mail.Items.Add(item);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SendMail(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (ctx.State.Mail == null) return ScriptOutcome.Abort;

                ctx.State.Mail.Send();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupGoto(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.GroupMembers == null) return ScriptOutcome.Abort;

                for (int j = 0; j < player.GroupMembers.Count(); j++)
                {
                    var action = new DelayedAction(DelayedType.NPC, Envir.Time, player.NPCObjectID, player.NPCScriptID, "[" + param[0] + "]");
                    player.GroupMembers[j].ActionList.Add(action);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_EnterMap(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!player.NPCData.TryGetValue("NPCMoveMap", out object _npcMoveMap) || !player.NPCData.TryGetValue("NPCMoveCoord", out object _npcMoveCoord)) return ScriptOutcome.Abort;

                player.Teleport((Map)_npcMoveMap, (Point)_npcMoveCoord, false);

                player.NPCData.Remove("NPCMoveMap");
                player.NPCData.Remove("NPCMoveCoord");
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_MakeWeddingRing(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.MakeWeddingRing();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ForceDivorce(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.NPCDivorce();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_LoadValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                string val = param[0];
                string filePath = param[1];
                string header = param[2];
                string key = param[3];

                InIReader reader = new InIReader(filePath);
                string loadedString = reader.ReadString(header, key, "");

                if (loadedString == "") return ScriptOutcome.Pass;
                seg.AddVariable(player, val, loadedString);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SaveValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                string filePath = param[0];
                string header = param[1];
                string key = param[2];
                string val = param[3];

                InIReader reader = new InIReader(filePath);
                reader.Write(header, key, val);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestGuard(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.Abort;
                ConquestGuildArcherInfo conquestArcher = conquest.ArcherList.FirstOrDefault(z => z.Index == tempInt);
                if (conquestArcher == null) return ScriptOutcome.Abort;

                if (conquestArcher.ArcherMonster != null)
                    if (!conquestArcher.ArcherMonster.Dead) return ScriptOutcome.Abort;

                if (player.IsGM)
                {
                    conquestArcher.Spawn(true);
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestArcher.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= conquestArcher.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = conquestArcher.GetRepairCost() });

                    conquestArcher.Spawn(true);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.Abort;
                ConquestGuildGateInfo conquestGate = conquest.GateList.FirstOrDefault(z => z.Index == tempInt);
                if (conquestGate == null) return ScriptOutcome.Abort;

                if (player.IsGM)
                {
                    conquestGate.Repair();
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestGate.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= (uint)conquestGate.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)conquestGate.GetRepairCost() });

                    conquestGate.Repair();
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestWall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.Abort;
                ConquestGuildWallInfo conquestWall = conquest.WallList.FirstOrDefault(z => z.Index == tempInt);

                if (conquestWall == null) return ScriptOutcome.Abort;

                if (player.IsGM)
                {
                    conquestWall.Repair();
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestWall.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= (uint)conquestWall.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)conquestWall.GetRepairCost() });

                    conquestWall.Repair();
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestSiege(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.Abort;
                ConquestGuildSiegeInfo conquestSiege = conquest.SiegeList.FirstOrDefault(z => z.Index == tempInt);
                if (conquestSiege == null) return ScriptOutcome.Abort;

                if (conquestSiege.Gate != null)
                {
                    if (!conquestSiege.Gate.Dead) return ScriptOutcome.Abort;
                }

                if (player.IsGM)
                {
                    conquestSiege.Repair();
                }
                else
                {
                    if (player.MyGuild == null || player.MyGuild.Gold < conquestSiege.GetRepairCost()) return ScriptOutcome.Abort;

                    player.MyGuild.Gold -= (uint)conquestSiege.GetRepairCost();
                    player.MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Amount = (uint)conquestSiege.GetRepairCost() });

                    conquestSiege.Repair();
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_TakeConquestGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
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

        internal static ScriptOutcome Exec_SetConquestRate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!byte.TryParse(param[1], out byte tempByte)) return ScriptOutcome.Abort;
                if (player.MyGuild != null && player.MyGuild.Guildindex == conquest.GuildInfo.Owner)
                {
                    conquest.GuildInfo.NPCRate = tempByte;
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_StartConquest(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!conquest.WarIsOn)
                {
                    conquest.StartType = ConquestType.Forced;
                    conquest.StartWar(conquest.GameType);

                    MessageQueue.Enqueue(GameLanguage.Get("{0} War Started.", conquest.Info.Name));

                }
                else
                {
                    conquest.WarIsOn = false;

                    MessageQueue.Enqueue(GameLanguage.Get("{0} War Stopped.", conquest.Info.Name));
                }

                foreach (var pl in Envir.Players)
                {
                    if (conquest.WarIsOn)
                    {
                        pl.ReceiveChat(GameLanguage.Get("{0} War Started.", conquest.Info.Name), ChatType.System);
                    }
                    else
                    {
                        pl.ReceiveChat(GameLanguage.Get("{0} War Stopped.", conquest.Info.Name), ChatType.System);
                    }

                    pl.BroadcastInfo();
                }

            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ScheduleConquest(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (player.MyGuild != null && player.MyGuild.Guildindex != conquest.GuildInfo.Owner && !conquest.WarIsOn)
                {
                    conquest.GuildInfo.AttackerID = player.MyGuild.Guildindex;
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_OpenGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.Abort;
                ConquestGuildGateInfo OpenGate = Conquest.GateList.FirstOrDefault(z => z.Index == tempInt);
                if (OpenGate == null) return ScriptOutcome.Abort;
                if (OpenGate.Gate == null) return ScriptOutcome.Abort;
                OpenGate.Gate.OpenDoor();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CloseGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
                var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (conquest == null) return ScriptOutcome.Abort;

                if (!int.TryParse(param[1], out tempInt)) return ScriptOutcome.Abort;
                ConquestGuildGateInfo CloseGate = conquest.GateList.FirstOrDefault(z => z.Index == tempInt);
                if (CloseGate == null) return ScriptOutcome.Abort;
                if (CloseGate.Gate == null) return ScriptOutcome.Abort;
                CloseGate.Gate.CloseDoor();
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_OpenBrowser(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                player.Enqueue(new S.OpenBrowser { Url = param[0] });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GetRandomText(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                string randomTextPath = Path.Combine(Settings.NPCPath, param[0]);
                if (!File.Exists(randomTextPath))
                {
                    MessageQueue.Enqueue(GameLanguage.Get("the randomTextFile:{0} does not exist.", randomTextPath));
                }
                else
                {
                    var lines = File.ReadAllLines(randomTextPath);
                    int index = Envir.Random.Next(0, lines.Length);
                    string randomText = lines[index];
                    seg.AddVariable(player, param[1], randomText);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_PlaySound(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[0], out int soundID)) return ScriptOutcome.Abort;
                player.Enqueue(new S.PlaySound { Sound = soundID });
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SetTimer(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int seconds) || !byte.TryParse(param[2], out byte type)) return ScriptOutcome.Abort;

                bool.TryParse(param[3], out bool global);

                if (seconds < 0) seconds = 0;

                if (global)
                {
                    var timerKey = "_-" + param[0];

                    Envir.Timers[timerKey] = new Timer(timerKey, seconds, type);
                }
                else
                {
                    player.SetTimer(param[0], seconds, type);
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ExpireTimer(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                var globalTimerKey = "_-" + param[0];

                if (Envir.Timers.ContainsKey(globalTimerKey))
                {
                    Envir.Timers.Remove(globalTimerKey);
                }

                player.ExpireTimer(param[0]);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_UnequipItem(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_GTAllRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("To summon guild members, one first must be in a guild."), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (player.MyGuildRank.Index != 0)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may summon all members.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }
            //ok were confident the player has a guild and is rank 0 (which we assume is leader)
            foreach (PlayerObject ob in Envir.Players) //thers no accessable member list from the player.myguild....hmmm ok we will look at all players curently online
            {
                if (ob == null)
                {
                    continue;//ignore people that dont exist, shouldnt happen but lets check for it
                }
                if (ob.Connection == null)
                {
                    continue; //really shouldnt be needed but better safe than sorry
                }
                if (ob.Connection.Stage != GameStage.Game)
                {
                    continue; //shouldnt be needed but lets make sure the player is in game
                }
                if (ob.MyGuild == null)
                {
                    continue; //they aint in a guild? lets not check them further (oops)
                }
                if (ob.MyGuild.Name != player.MyGuild.Name)
                {
                    continue; //not in my guild?who wants you!?
                }
                if (ob.Name == player.Name)
                {
                    continue;//dont try to recall yourself
                }
                ob.Teleport(player.CurrentMap, new Point(player.CurrentLocation.X + Envir.Random.Next(4), player.CurrentLocation.Y + Envir.Random.Next(4)));
                ob.ReceiveChat(GameLanguage.Get("You have been summoned by ") + player.Name, ChatType.System);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTRecall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("To summon a guild member, one first must be in a guild."), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (player.MyGuildRank.Index != 0)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may summon members.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }
            PlayerObject guildmember = Envir.GetPlayer(param[0]);
            if (guildmember == null)
            {
                player.ReceiveChat(GameLanguage.Get("I can not summon {0} as they do not appear to exist", param[0]), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (guildmember.Connection == null)
            {
                player.ReceiveChat(GameLanguage.Get("I can not summon {0} at this time", param[0]), ChatType.System);
                return ScriptOutcome.Pass; //really shouldnt be needed but better safe than sorry
            }
            if (guildmember.Connection.Stage != GameStage.Game)
            {
                player.ReceiveChat(GameLanguage.Get("{0} is not currently in-game", param[0]), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (guildmember.MyGuild == null)
            {
                player.ReceiveChat(GameLanguage.Get("{0} is not in a guild", param[0]), ChatType.System);
                return ScriptOutcome.Abort;
            }
            if (guildmember.MyGuild.Name != player.MyGuild.Name)
            {
                player.ReceiveChat(GameLanguage.Get("{0} is not in {1}. They are in {2}", param[0], player.MyGuild.Name, guildmember.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }
            guildmember.Teleport(player.CurrentMap, new Point(player.CurrentLocation.X + Envir.Random.Next(4), player.CurrentLocation.Y + Envir.Random.Next(4)));
            guildmember.ReceiveChat(GameLanguage.Get("You have been summoned by {0}", player.Name), ChatType.System);
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTSale(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
                return ScriptOutcome.Abort;

            if (player.MyGuildRank.Index != 0 || player.CurrentMap.Info.GTIndex != player.MyGuild.GTIndex)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may initiate sale.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!int.TryParse(param[0], out int saleprice))
                return ScriptOutcome.Abort;


            if (saleprice < 2000000)
            {
                player.ReceiveChat(GameLanguage.Get("Minimum sale price must be 2,000,000"), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!player.MyGuild.GTForSale(player, saleprice))
                return ScriptOutcome.Abort;

            player.ReceiveChat(GameLanguage.Get("This territory has been listed for sale."), ChatType.System);
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GTCancelSale(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (player.MyGuild == null)
                return ScriptOutcome.Abort;

            if (player.MyGuildRank.Index != 0 || player.CurrentMap.Info.GTIndex != player.MyGuild.GTIndex)
            {
                player.ReceiveChat(GameLanguage.Get("Only the leader of {0} may cancel sale.", player.MyGuild.Name), ChatType.System);
                return ScriptOutcome.Abort;
            }

            if (!player.MyGuild.EndGTSale(player))
                return ScriptOutcome.Abort;

            player.ReceiveChat(GameLanguage.Get("This territory sale has been cancelled."), ChatType.System);
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RollDie(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                bool.TryParse(param[1], out bool autoRoll);

                var result = Envir.Random.Next(1, 7);

                S.Roll p = new S.Roll { Type = 0, Page = param[0], AutoRoll = autoRoll, Result = result };

                player.NPCData["NPCRollResult"] = result;
                player.Enqueue(p);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_RollYut(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                bool.TryParse(param[1], out bool autoRoll);

                var result = Envir.Random.Next(1, 7);

                S.Roll p = new S.Roll { Type = 1, Page = param[0], AutoRoll = autoRoll, Result = result };

                player.NPCData["NPCRollResult"] = result;
                player.Enqueue(p);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Drop(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_ReviveHero(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            player.ReviveHero();
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_SealHero(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            player.SealHero();
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_DeleteHero(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            player.DeleteHero();
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HeroGiveSkill(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.Hero == null || player.Hero.Info == null) return ScriptOutcome.Abort;

                byte spellLevel = 0;

                Spell skill;
                if (!Enum.TryParse(param[0], true, out skill)) return ScriptOutcome.Abort;

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
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (player.Hero == null || player.Hero.Info == null) return ScriptOutcome.Abort;

                if (!Enum.TryParse(param[0], true, out Spell skill)) return ScriptOutcome.Abort;

                if (!player.Hero.Info.Magics.Any(e => e.Spell == skill)) return ScriptOutcome.Pass;

                for (var j = player.Hero.Info.Magics.Count - 1; j >= 0; j--)
                {
                    if (player.Hero.Info.Magics[j].Spell != skill) continue;

                    player.Hero.Info.Magics.RemoveAt(j);
                    player.Hero.Enqueue(new S.RemoveMagic { PlaceId = j });
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestRepairAll(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;

            if (player == null) return ScriptOutcome.Pass;

            if (!player.IsGM)
            {
                player.ReceiveChat(GameLanguage.Get("You are not a GM and this command is not enabled for you."), ChatType.System);
                MessageQueue.Enqueue(GameLanguage.Get("GM Command @CONQUESTREPAIRALL invoked by non-GM player: {0}", player.Name));
                return ScriptOutcome.Abort;
            }

            if (!int.TryParse(param[0], out int tempInt)) return ScriptOutcome.Abort;
            var conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
            if (conquest == null) return ScriptOutcome.Abort;

            MessageQueue.Enqueue(GameLanguage.Get("@CONQUESTREPAIRALL invoked by GM: {0} on account index: {1}", player.Name, player.Info.AccountInfo.Index));
            MessageQueue.Enqueue(GameLanguage.Get("Conquest: {0}", conquest.Info.Name));

            if (conquest.Guild != null)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Owner: {0}", conquest.Guild.Name));
            }
            else
            {
                MessageQueue.Enqueue(GameLanguage.Get("No current owner."));
            }

            int _fixed = 0;
            foreach (ConquestGuildArcherInfo archer in conquest.ArcherList)
            {
                if (archer.ArcherMonster != null &&
                archer.ArcherMonster.Dead)
                {
                    archer.Spawn(true);
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Archers repaired: {0}/{1}", _fixed, conquest.ArcherList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Archers repaired: {0}/{1}", _fixed, conquest.ArcherList.Count));

            _fixed = 0;
            foreach (ConquestGuildGateInfo conquestGate in conquest.GateList)
            {
                if (conquestGate != null)
                {
                    conquestGate.Repair();
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Gates repaired: {0}/{1}", _fixed, conquest.GateList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Gates repaired: {0}/{1}", _fixed, conquest.GateList.Count));

            _fixed = 0;
            foreach (ConquestGuildWallInfo conquestWall in conquest.WallList)
            {
                if (conquestWall != null)
                {
                    conquestWall.Repair();
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Walls repaired: {0}/{1}", _fixed, conquest.WallList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Walls repaired: {0}/{1}", _fixed, conquest.WallList.Count));

            _fixed = 0;
            foreach (ConquestGuildSiegeInfo conquestSiege in conquest.SiegeList)
            {
                if (conquestSiege != null)
                {
                    conquestSiege.Repair();
                    _fixed++;
                }
            }
            player.ReceiveChat(GameLanguage.Get("Sieges repaired: {0}/{1}", _fixed, conquest.SiegeList.Count), ChatType.System);
            MessageQueue.Enqueue(GameLanguage.Get("Sieges repaired: {0}/{1}", _fixed, conquest.SiegeList.Count));

            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_GlobalMessage(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[1], true, out ChatType chatType)) return ScriptOutcome.Abort;

                var p = new S.Chat { Message = param[0], Type = chatType };
                Envir.Broadcast(p);
            }

            /* //mobs have no real "delayed" npc code so not added this yet
            case ScriptActionKind.Goto:
            DelayedAction action = new DelayedAction(DelayedType.NPC, -1, player.NPCID, "[" + param[0] + "]");
            player.ActionList.Add(action);
                break;
            */

            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_GiveBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (!Enum.IsDefined(typeof(BuffType), param[0])) return ScriptOutcome.Abort;

                int.TryParse(param[1], out int tempInt);
                bool.TryParse(param[2], out bool infinite);
                bool.TryParse(param[3], out bool visible);
                bool.TryParse(param[4], out bool stackable);

                monster.AddBuff((BuffType)(byte)Enum.Parse(typeof(BuffType), param[0], true), monster, Settings.Second * tempInt, new Stats(), visible);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_Mongen(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (seg.Param1 == null || seg.Param2 == 0 || seg.Param3 == 0) return ScriptOutcome.Abort;
                if (!byte.TryParse(param[1], out byte tempByte)) return ScriptOutcome.Abort;

                var map = Envir.GetMapByNameAndInstance(seg.Param1, seg.Param1Instance);
                if (map == null) return ScriptOutcome.Abort;

                var monInfo = Envir.GetMonsterInfo(param[0]);
                if (monInfo == null) return ScriptOutcome.Abort;

                for (int j = 0; j < tempByte; j++)
                {
                    MonsterObject mob = MonsterObject.GetMonster(monInfo);
                    if (mob == null) return ScriptOutcome.Abort;
                    mob.Direction = 0;
                    mob.ActionTime = Envir.Time + 1000;
                    mob.Spawn(map, new Point(seg.Param2, seg.Param3));
                }
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_LoadValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                string val = param[0];
                string filePath = param[1];
                string header = param[2];
                string key = param[3];

                var reader = new InIReader(filePath);
                string loadedString = reader.ReadString(header, key, "");

                if (loadedString == "") return ScriptOutcome.Pass;
                seg.AddVariable(monster, val, loadedString);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_SaveValue(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                string filePath = param[0];
                string header = param[1];
                string key = param[2];
                string val = param[3];

                var reader = new InIReader(filePath);
                reader.Write(header, key, val);
            }
            return ScriptOutcome.Pass;
        }

        internal static ScriptOutcome ExecMonster_MonClear(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MonsterObject monster = ctx.Monster;

            if (monster == null) return ScriptOutcome.Pass;

            {
                if (!int.TryParse(param[1], out int tempInt)) return ScriptOutcome.Abort;

                var map = Envir.GetMapByNameAndInstance(param[0], tempInt);
                if (map == null) return ScriptOutcome.Abort;

                foreach (var cell in map.Cells)
                {
                    if (cell == null || cell.Objects == null) continue;

                    for (int j = 0; j < cell.Objects.Count(); j++)
                    {
                        MapObject ob = cell.Objects[j];

                        if (ob.Race != ObjectType.Monster) continue;
                        if (ob.Dead) continue;
                        ob.Die();
                    }
                }
            }
            return ScriptOutcome.Pass;
        }
    }
}
