using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
    /// Executors for &lt;$VALUE&gt; replacements. Each body was moved out of the switch in ReplaceValue
    /// without changing a statement: break leaves the do/while(false) block and the value built so far is
    /// returned, while an explicit return inside a body still answers directly.
    /// </summary>
    public partial class NPCSegment
    {
        internal static string ExecValue_MONSTERCOUNT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            Map map = Envir.GetMapByNameAndInstance(ctx.OneValueMatch.Groups[2].Captures[0].Value.ToUpper());
            newValue = map == null ? "N/A" : map.MonsterCount.ToString();
            return newValue;
        }

        internal static string ExecValue_CONQUESTGUARD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            int intVal1, intVal2;
            ConquestObject Conquest;
            ConquestGuildArcherInfo Archer;

            if (player == null) return string.Empty;

            var val1 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[3].Captures[0].Value.ToUpper());
            var val2 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[4].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1) && int.TryParse(val2.Replace("%", ""), out intVal2))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return GameLanguage.Get("Not Found");
                Archer = Conquest.ArcherList.FirstOrDefault(x => x.Index == intVal2);
                if (Archer == null) return GameLanguage.Get("Not Found");
                if (Archer.Info.Name == "" || Archer.Info.Name == null)
                    newValue = GameLanguage.Get("Conquest Guard");
                else
                    newValue = Archer.Info.Name;
                if (Archer.GetRepairCost() == 0)
                    newValue += GameLanguage.Get(" - [ Still Alive ]");
                else
                    newValue += GameLanguage.Get(" - [ {0} gold ]", Archer.GetRepairCost().ToString("#,##0"));
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTGATE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1, val2;
            int intVal1, intVal2;
            ConquestObject Conquest;
            ConquestGuildGateInfo Gate;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[3].Captures[0].Value.ToUpper());
            val2 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[4].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1) && int.TryParse(val2.Replace("%", ""), out intVal2))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return GameLanguage.Get("Not Found");
                Gate = Conquest.GateList.FirstOrDefault(x => x.Index == intVal2);
                if (Gate == null) return GameLanguage.Get("Not Found");
                if (Gate.Info.Name == "" || Gate.Info.Name == null)
                    newValue = GameLanguage.Get("Conquest Gate");
                else
                    newValue = Gate.Info.Name;
                if (Gate.GetRepairCost() == 0)
                    newValue += GameLanguage.Get(" - [ No Repair Required ]");
                else
                    newValue += GameLanguage.Get(" - [ {0} gold ]", Gate.GetRepairCost().ToString("#,##0"));
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTWALL(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1, val2;
            int intVal1, intVal2;
            ConquestObject Conquest;
            ConquestGuildWallInfo Wall;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[3].Captures[0].Value.ToUpper());
            val2 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[4].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1) && int.TryParse(val2.Replace("%", ""), out intVal2))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return GameLanguage.Get("Not Found");
                Wall = Conquest.WallList.FirstOrDefault(x => x.Index == intVal2);
                if (Wall == null) return GameLanguage.Get("Not Found");
                if (Wall.Info.Name == "" || Wall.Info.Name == null)
                    newValue = GameLanguage.Get("Conquest Wall");
                else
                    newValue = Wall.Info.Name;
                if (Wall.GetRepairCost() == 0)
                    newValue += GameLanguage.Get(" - [ No Repair Required ]");
                else
                    newValue += GameLanguage.Get(" - [ {0} gold ]", Wall.GetRepairCost().ToString("#,##0"));
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTSIEGE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1, val2;
            int intVal1, intVal2;
            ConquestObject Conquest;
            ConquestGuildSiegeInfo Siege;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[3].Captures[0].Value.ToUpper());
            val2 = seg.FindVariable(player, "%" + ctx.TwoValueMatch.Groups[4].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1) && int.TryParse(val2.Replace("%", ""), out intVal2))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return GameLanguage.Get("Not Found");
                Siege = Conquest.SiegeList.FirstOrDefault(x => x.Index == intVal2);
                if (Siege == null) return GameLanguage.Get("Not Found");
                if (Siege.Info.Name == "" || Siege.Info.Name == null)
                    newValue = GameLanguage.Get("Conquest Siege");
                else
                    newValue = Siege.Info.Name;
                if (Siege.GetRepairCost() == 0)
                    newValue += GameLanguage.Get(" - [ Still Alive ]");
                else
                    newValue += GameLanguage.Get(" - [ {0} gold ]", Siege.GetRepairCost().ToString("#,##0"));
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTOWNER(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1;
            int intVal1;
            ConquestObject Conquest;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.OneValueMatch.Groups[2].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return string.Empty;
                if (Conquest.Guild == null) return GameLanguage.Get("No Owner");
                newValue = Conquest.Guild.Name;
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTGOLD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1;
            int intVal1;
            ConquestObject Conquest;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.OneValueMatch.Groups[2].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return string.Empty;
                newValue = Conquest.GuildInfo.GoldStorage.ToString();
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTRATE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1;
            int intVal1;
            ConquestObject Conquest;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.OneValueMatch.Groups[2].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return string.Empty;
                newValue = Conquest.GuildInfo.NPCRate.ToString() + "%";
            }
            return newValue;
        }

        internal static string ExecValue_CASTLEWARDATE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            {
                ConquestObject nextWar = Envir.Conquests
                .Where(x => x.WarStartTime > Envir.Now)
                .OrderBy(x => x.WarStartTime)
                .FirstOrDefault();
                newValue = nextWar == null
                ? GameLanguage.Get("No War Scheduled")
                : nextWar.WarStartTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            return newValue;
        }

        internal static string ExecValue_CONQUESTSCHEDULE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;
            string val1;
            int intVal1;
            ConquestObject Conquest;

            if (player == null) return string.Empty;

            val1 = seg.FindVariable(player, "%" + ctx.OneValueMatch.Groups[2].Captures[0].Value.ToUpper());
            if (int.TryParse(val1.Replace("%", ""), out intVal1))
            {
                Conquest = Envir.Conquests.FirstOrDefault(x => x.Info.Index == intVal1);
                if (Conquest == null) return GameLanguage.Get("Conquest Not Found");
                if (Conquest.GuildInfo.AttackerID == -1) return GameLanguage.Get("No War Scheduled");
                if (Envir.Guilds.FirstOrDefault(x => x.Guildindex == Conquest.GuildInfo.AttackerID) == null)
                {
                    newValue = GameLanguage.Get("No War Scheduled");
                }
                else
                {
                    newValue = (Envir.Guilds.FirstOrDefault(x => x.Guildindex == Conquest.GuildInfo.AttackerID).Name);
                }
            }
            return newValue;
        }

        internal static string ExecValue_OUTPUT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = seg.FindVariable(player, "%" + ctx.VariableMatch.Groups[2].Captures[0].Value.ToUpper());
            return newValue;
        }

        internal static string ExecValue_NPCNAME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            for (int i = 0; i < player.CurrentMap.NPCs.Count; i++)
            {
                NPCObject ob = player.CurrentMap.NPCs[i];
                if (ob.ObjectID != player.NPCObjectID) continue;
                newValue = ob.Name.Replace("_", " ");
            }
            return newValue;
        }

        internal static string ExecValue_USERNAME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Name;
            return newValue;
        }

        internal static string ExecValue_LEVEL(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Level.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_CLASS(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Class.ToString();
            return newValue;
        }

        internal static string ExecValue_MAP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.CurrentMap.Info.FileName;
            return newValue;
        }

        internal static string ExecValue_X_COORD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.CurrentLocation.X.ToString();
            return newValue;
        }

        internal static string ExecValue_Y_COORD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.CurrentLocation.Y.ToString();
            return newValue;
        }

        internal static string ExecValue_HP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.HP.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_MAXHP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Stats[Stat.HP].ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_MP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.MP.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_MAXMP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Stats[Stat.MP].ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_GAMEGOLD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Account.Gold.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_CREDIT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Account.Credit.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_ARMOUR(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Armour] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Armour].FriendlyName : GameLanguage.Get("No Armour");
            return newValue;
        }

        internal static string ExecValue_USERWEAPON(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Weapon] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Weapon].FriendlyName : GameLanguage.Get("No Weapon");
            return newValue;
        }

        internal static string ExecValue_RING_L(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.RingL] != null ?
            player.Info.Equipment[(int)EquipmentSlot.RingL].FriendlyName : GameLanguage.Get("No Ring");
            return newValue;
        }

        internal static string ExecValue_RING_R(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.RingR] != null ?
            player.Info.Equipment[(int)EquipmentSlot.RingR].FriendlyName : GameLanguage.Get("No Ring");
            return newValue;
        }

        internal static string ExecValue_BRACELET_L(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.BraceletL] != null ?
            player.Info.Equipment[(int)EquipmentSlot.BraceletL].FriendlyName : GameLanguage.Get("No Bracelet");
            return newValue;
        }

        internal static string ExecValue_BRACELET_R(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.BraceletR] != null ?
            player.Info.Equipment[(int)EquipmentSlot.BraceletR].FriendlyName : GameLanguage.Get("No Bracelet");
            return newValue;
        }

        internal static string ExecValue_NECKLACE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Necklace] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Necklace].FriendlyName : GameLanguage.Get("No Necklace");
            return newValue;
        }

        internal static string ExecValue_BELT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Belt] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Belt].FriendlyName : GameLanguage.Get("No Belt");
            return newValue;
        }

        internal static string ExecValue_BOOTS(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Boots] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Boots].FriendlyName : GameLanguage.Get("No Boots");
            return newValue;
        }

        internal static string ExecValue_HELMET(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Helmet] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Helmet].FriendlyName : GameLanguage.Get("No Helmet");
            return newValue;
        }

        internal static string ExecValue_AMULET(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Amulet] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Amulet].FriendlyName : GameLanguage.Get("No Amulet");
            return newValue;
        }

        internal static string ExecValue_STONE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Stone] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Stone].FriendlyName : GameLanguage.Get("No Stone");
            return newValue;
        }

        internal static string ExecValue_TORCH(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.Info.Equipment[(int)EquipmentSlot.Torch] != null ?
            player.Info.Equipment[(int)EquipmentSlot.Torch].FriendlyName : GameLanguage.Get("No Torch");
            return newValue;
        }

        internal static string ExecValue_DATE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = Envir.Now.ToShortDateString();
            return newValue;
        }

        internal static string ExecValue_MAPLIGHT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = Envir.Lights.ToString();
            return newValue;
        }

        internal static string ExecValue_USERCOUNT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = Envir.PlayerCount.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecValue_PKPOINT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.PKPoints.ToString();
            return newValue;
        }

        internal static string ExecValue_GUILDWARTIME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = Settings.Guild_WarTime.ToString();
            return newValue;
        }

        internal static string ExecValue_GUILDWARFEE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = Settings.Guild_WarCost.ToString();
            return newValue;
        }

        internal static string ExecValue_PARCELAMOUNT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.GetMailAwaitingCollectionAmount().ToString();
            return newValue;
        }

        internal static string ExecValue_GUILDNAME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            if (player.MyGuild == null) return GameLanguage.Get("No Guild");
            else
                newValue = string.Format("{0} {1}", player.MyGuild.Name, GameLanguage.Get("Guid"));
            return newValue;
        }

        internal static string ExecValue_ROLLRESULT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = player.NPCData.TryGetValue("NPCRollResult", out object _rollResult) ? _rollResult.ToString() : GameLanguage.Get("Not Rolled");
            return newValue;
        }

        internal static string ExecValue_MOUNTLOYALTY(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            if (!player.Mount.HasMount)
            {
                newValue = GameLanguage.Get("NoMount");
            }
            else
            {
                newValue = $"{player.Info.Equipment[(int)EquipmentSlot.Mount].CurrentDura} ({player.Info.Equipment[(int)EquipmentSlot.Mount].MaxDura})";
            }
            return newValue;
        }

        internal static string ExecValue_MOUNT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            if (player.Mount.HasMount)
            {
                newValue = player.Info.Equipment[(int)EquipmentSlot.Mount].FriendlyName;
            }
            else
            {
                newValue = GameLanguage.Get("NoMount");
            }
            return newValue;
        }

        internal static string ExecValue_GUILDEXTENDFEE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            if (player.MyGuild != null && player.MyGuild.HasGT)
            {
                newValue = GameLanguage.Get("Expire On: {0}, Extend fee: {1}", player.MyGuild.GTRent.ToString(), Settings.ExtendGT.ToString())
                ;
            }
            else
            {
                newValue = GameLanguage.Get("None");
            }
            return newValue;
        }

        internal static string ExecValue_GUILDRENTFEE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            newValue = Settings.BuyGTGold.ToString();
            return newValue;
        }

        internal static string ExecValue_GUILDGTRENTALDAYSLEFT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            if (player.MyGuild == null)
            {
                newValue = "0";
                return newValue;
            }
            newValue = (player.MyGuild.GTRent - Envir.Now).Days.ToString();
            return newValue;
        }

        internal static string ExecValue_AGITGUILDNAME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            string newValue = string.Empty;

            if (player == null) return string.Empty;

            if (player.MyGuild == null)
            {
                newValue = GameLanguage.Get("NO GUILD");
                return newValue;
            }
            newValue = (player.MyGuild.Name);
            return newValue;
        }

        internal static string ExecMonsterValue_OUTPUT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = seg.FindVariable(monster, "%" + ctx.VariableMatch.Groups[2].Captures[0].Value.ToUpper());
            return newValue;
        }

        internal static string ExecMonsterValue_USERNAME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.Name;
            return newValue;
        }

        internal static string ExecMonsterValue_LEVEL(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.Level.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_MAP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.CurrentMap.Info.FileName;
            return newValue;
        }

        internal static string ExecMonsterValue_X_COORD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.CurrentLocation.X.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_Y_COORD(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.CurrentLocation.Y.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_HP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.HP.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_MAXHP(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.Stats[Stat.HP].ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_DATE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Envir.Now.ToShortDateString();
            return newValue;
        }

        internal static string ExecMonsterValue_MAPLIGHT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Envir.Lights.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_USERCOUNT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Envir.PlayerCount.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_GUILDWARTIME(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Settings.Guild_WarTime.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_GUILDWARFEE(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Settings.Guild_WarCost.ToString();
            return newValue;
        }
    }
}
