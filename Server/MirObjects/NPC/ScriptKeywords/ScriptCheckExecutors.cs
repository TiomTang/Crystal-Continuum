using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.MirObjects
{
    /// <summary>
    /// What each #IF condition does. Keyed by ScriptCheckKind, registered on the entry in
    /// ScriptCheckCatalog; player-only checks return Pass when there is no player, which is what the
    /// engine used to get by having no monster case for them.
    /// </summary>
    public partial class NPCSegment
    {
        private static int ObjectLevel(MapObject obj)
        {
            var player = obj as PlayerObject;

            return player != null ? player.Level : ((MonsterObject)obj).Level;
        }

        internal static ScriptOutcome Exec_Level(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;
            bool failed = false;

            if (obj == null) return ScriptOutcome.Pass;

            {
                if (!ushort.TryParse(param[1], out ushort level))
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                try
                {
                    failed = !Compare(param[0], ObjectLevel(obj), level);
                }
                catch (ArgumentException)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                    return ScriptOutcome.Abort;
                }
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!uint.TryParse(param[1], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.Account.Gold, tempUint);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckGuildGold(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!uint.TryParse(param[1], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.MyGuild.Gold, tempUint);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckCredit(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!uint.TryParse(param[1], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.Account.Credit, tempUint);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckItem(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            ushort count;
            ushort dura;

            if (!ushort.TryParse(param[1], out count))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            bool checkDura = ushort.TryParse(param[2], out dura);

            var info = Envir.GetItemInfo(param[0]);

            foreach (var item in player.Info.Inventory.Where(item => item != null && item.Info == info))
            {
                if (checkDura)
                    if (item.CurrentDura < (dura * 1000)) continue;

                if (count > item.Count)
                {
                    count -= item.Count;
                    continue;
                }

                if (count > item.Count) continue;
                count = 0;
                break;
            }
            if (count > 0)
                failed = true;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckGender(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirGender gender;

            if (!MirGender.TryParse(param[0], false, out gender))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = player.Gender != gender;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckClass(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirClass mirClass;

            if (!MirClass.TryParse(param[0], true, out mirClass))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = player.Class != mirClass;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckDay(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;

            var day = Envir.Now.DayOfWeek.ToString().ToUpper();
            var dayToCheck = param[0].ToUpper();

            failed = day != dayToCheck;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHour(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;
            uint tempUint = 0;

            if (!uint.TryParse(param[0], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            var hour = Envir.Now.Hour;
            var hourToCheck = tempUint;

            failed = hour != hourToCheck;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckMinute(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;
            uint tempUint = 0;

            if (!uint.TryParse(param[0], out tempUint))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            var minute = Envir.Now.Minute;
            var minuteToCheck = tempUint;

            failed = minute != minuteToCheck;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            if (!File.Exists(param[0]))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            var read = File.ReadAllLines(param[0]);
            failed = !read.Contains(player.Name);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckGuildNameList(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            string[] read = null;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            if (!File.Exists(param[0]))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            read = File.ReadAllLines(param[0]);
            failed = player.MyGuild == null || !read.Contains(player.MyGuild.Name);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_IsAdmin(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = !player.IsGM;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckPkPoint(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                failed = !Compare(param[0], player.PKPoints, tempInt);
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckRange(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;
            bool failed = false;

            if (obj == null) return ScriptOutcome.Pass;

            int x, y, range;
            if (!int.TryParse(param[0], out x) || !int.TryParse(param[1], out y) || !int.TryParse(param[2], out range))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            var target = new Point { X = x, Y = y };

            failed = !Functions.InRange(obj.CurrentLocation, target, range);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckMap(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            MapObject obj = (MapObject)ctx.Player ?? ctx.Monster;
            bool failed = false;

            if (obj == null) return ScriptOutcome.Pass;

            Map map = Envir.GetMapByNameAndInstance(param[0]);

            failed = obj.CurrentMap != map;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckMapLight(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;

            var playerLight = Envir.Lights.ToString().ToUpperInvariant();
            var playerCheck = param.Count > 0 ? param[0]?.Trim().ToUpperInvariant() : string.Empty;
            failed = string.IsNullOrEmpty(playerCheck) || playerLight != playerCheck;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Check(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            uint tempUint = 0;

            if (player == null) return ScriptOutcome.Pass;

            uint onCheck;

            if (!uint.TryParse(param[0], out tempUint) || !uint.TryParse(param[1], out onCheck) || tempUint > Globals.FlagIndexCount)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            bool tempBool = Convert.ToBoolean(onCheck);

            bool flag = player.Info.Flags[tempUint];

            failed = flag != tempBool;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHum(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;
            Map map = null;

            if (!int.TryParse(param[1], out tempInt) || !int.TryParse(param[3], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            map = Envir.GetMapByNameAndInstance(param[2], tempInt2);
            if (map == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = !Compare(param[0], map.Players.Count(), tempInt);

            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckMon(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;
            Map map = null;

            if (!int.TryParse(param[1], out tempInt) || !int.TryParse(param[3], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            map = Envir.GetMapByNameAndInstance(param[2], tempInt2);
            if (map == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = !Compare(param[0], map.MonsterCount, tempInt);

            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckExactMon(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;
            Map map = null;

            if (Envir.GetMonsterInfo(param[0]) == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            if (!int.TryParse(param[2], out tempInt) || !int.TryParse(param[4], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            map = Envir.GetMapByNameAndInstance(param[3], tempInt2);
            if (map == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = (!Compare(param[1], Envir.Objects.Count((
            d => d.CurrentMap == map &&
            d.Race == ObjectType.Monster &&
            string.Equals(d.Name.Replace(" ", ""), param[0], StringComparison.OrdinalIgnoreCase) &&
            !d.Dead)), tempInt));

            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Random(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;
            int tempInt = 0;

            if (!int.TryParse(param[0], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = 0 != Envir.Random.Next(0, tempInt);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_Groupleader(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = (player.GroupMembers == null || player.GroupMembers[0] != player);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupCount(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = (player.GroupMembers == null || !Compare(param[0], player.GroupMembers.Count, tempInt));
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_GroupCheckNearby(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            Point target = default(Point);
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            target = new Point(-1, -1);
            for (int j = 0; j < player.CurrentMap.NPCs.Count; j++)
            {
                NPCObject ob = player.CurrentMap.NPCs[j];
                if (ob.ObjectID != player.NPCObjectID) continue;
                target = ob.CurrentLocation;
                break;
            }
            if (target.X == -1)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.GroupMembers == null)
                failed = true;
            else
            {
                for (int j = 0; j < player.GroupMembers.Count; j++)
                {
                    if (player.GroupMembers[j] == null) continue;
                    failed |= !Functions.InRange(player.GroupMembers[j].CurrentLocation, target, 9);
                    if (failed) break;
                }
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_PetCount(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = !Compare(param[0], player.Pets.Count(), tempInt);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_PetLevel(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            for (int p = 0; p < player.Pets.Count(); p++)
            {
                failed = !Compare(param[0], player.Pets[p].PetLevel, tempInt);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckCalc(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;

            int left;
            int right;

            try
            {
                if (!int.TryParse(param[0], out left) || !int.TryParse(param[2], out right))
                {
                    failed = !Compare(param[1], param[0], param[2]);
                }
                else
                {
                    failed = !Compare(param[1], left, right);
                }
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[1], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_InGuild(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            if (param[0].Length > 0)
            {
                failed = player.MyGuild == null || player.MyGuild.Name != param[0];
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = player.MyGuild == null;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HasGT(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = player.MyGuild == null || !player.MyGuild.HasGT;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckQuest(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            string tempString = param[1].ToUpper();

            if (tempString == "ACTIVE")
            {
                failed = !player.CurrentQuests.Any(e => e.Index == tempInt);
            }
            else //COMPLETE
            {
                failed = !player.CompletedQuests.Contains(tempInt);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckRelationship(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            if (player.Info.Married == 0)
            {
                failed = true;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckWeddingRing(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = !player.CheckMakeWeddingRing();
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckPet(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            bool petMatch = false;
            for (int c = player.Pets.Count - 1; c >= 0; c--)
            {
                if (string.Compare(player.Pets[c].Info.Name, param[0], true) != 0) continue;

                petMatch = true;
            }

            failed = !petMatch;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HasBagSpace(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            int slotCount = 0;

            for (int k = 0; k < player.Info.Inventory.Length; k++)
                if (player.Info.Inventory[k] == null) slotCount++;

            failed = !Compare(param[0], slotCount, tempInt);
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_IsNewHuman(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = player.Info.AccountInfo.Characters.Count > 1;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckConquest(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                failed = Conquest.WarIsOn;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AffordGuard(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt) || !int.TryParse(param[1], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                ConquestGuildArcherInfo Archer = Conquest.ArcherList.FirstOrDefault(g => g.Info.Index == tempInt2);
                if (Archer == null || Archer.GetRepairCost() == 0)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                if (player.MyGuild != null)
                    failed = (player.MyGuild.Gold < Archer.GetRepairCost());
                else
                    failed = true;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AffordGate(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt) || !int.TryParse(param[1], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                ConquestGuildGateInfo Gate = Conquest.GateList.FirstOrDefault(f => f.Info.Index == tempInt2);
                if (Gate == null || Gate.GetRepairCost() == 0)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                if (player.MyGuild != null)
                    failed = (player.MyGuild.Gold < Gate.GetRepairCost());
                else
                    failed = true;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AffordWall(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt) || !int.TryParse(param[1], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                ConquestGuildWallInfo Wall = Conquest.WallList.FirstOrDefault(h => h.Info.Index == tempInt2);
                if (Wall == null || Wall.GetRepairCost() == 0)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                if (player.MyGuild != null)
                    failed = (player.MyGuild.Gold < Wall.GetRepairCost());
                else
                    failed = true;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_AffordSiege(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;
            int tempInt2 = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt) || !int.TryParse(param[1], out tempInt2))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                ConquestGuildGateInfo Gate = Conquest.GateList.FirstOrDefault(f => f.Info.Index == tempInt2);
                if (Gate == null || Gate.GetRepairCost() == 0)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                if (player.MyGuild != null)
                    failed = (player.MyGuild.Gold < Gate.GetRepairCost());
                else
                    failed = true;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckPermission(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            GuildRankOptions guildPermissions;
            if (!Enum.TryParse(param[0], true, out guildPermissions))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            if (player.MyGuild == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            failed = !(player.MyGuildRank.Options.HasFlag(guildPermissions));

            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestAvailable(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                if (player.MyGuild != null)
                    failed = (Conquest.GuildInfo.AttackerID != -1);
                else
                    failed = true;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_ConquestOwner(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[0], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            try
            {
                ConquestObject Conquest = Envir.Conquests.FirstOrDefault(z => z.Info.Index == tempInt);
                if (Conquest == null)
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                if (player.MyGuild != null && player.MyGuild.Guildindex == Conquest.GuildInfo.Owner)
                    failed = false;
                else
                    failed = true;
            }
            catch (ArgumentException)
            {
                MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[0], seg.Key));
                return ScriptOutcome.Abort;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckTimer(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!long.TryParse(param[1], out long time))
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }

                try
                {
                    var globalTimerKey = "_-" + param[0];

                    Timer timer;

                    if (Envir.Timers.ContainsKey(globalTimerKey))
                    {
                        timer = Envir.Timers[globalTimerKey];
                    }
                    else
                    {
                        timer = player.GetTimer(param[0]);
                    }

                    long remainingTime = 0;

                    if (timer != null) remainingTime = (timer.RelativeTime - Envir.Time) / 1000;

                    failed = !Compare(param[2], remainingTime, time);
                }
                catch (ArgumentException)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Incorrect operator: {0}, Page: {1}", param[2], seg.Key));
                    return ScriptOutcome.Abort;
                }
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_HeroLevel(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;
            int tempInt = 0;

            if (player == null) return ScriptOutcome.Pass;

            if (!int.TryParse(param[1], out tempInt))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.CurrentHero == null)
            {
                failed = true;
            }
            else
            {
                failed = !Compare(param[0], player.CurrentHero.Level, tempInt);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHeroClass(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirClass heroClass;
            if (!MirClass.TryParse(param[0], true, out heroClass))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.CurrentHero == null)
            {
                failed = true;
            }
            else
            {
                failed = player.CurrentHero.Class != heroClass;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHeroGender(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            MirGender heroGender;
            if (!MirGender.TryParse(param[0], false, out heroGender))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }
            if (player.CurrentHero == null)
            {
                failed = true;
            }
            else
            {
                failed = player.CurrentHero.Gender != heroGender;
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHeroItem(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            ushort heroItemCount;
            ushort heroItemDura;

            if (!ushort.TryParse(param[1], out heroItemCount))
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            bool heroCheckDura = ushort.TryParse(param[2], out heroItemDura);

            var heroItemInfo = Envir.GetItemInfo(param[0]);

            if (player.CurrentHero == null || player.CurrentHero.Inventory == null)
            {
                failed = true;
                return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
            }

            foreach (var item in player.CurrentHero.Inventory.Where(item => item != null && item.Info == heroItemInfo))
            {
                if (heroCheckDura)
                    if (item.CurrentDura < (heroItemDura * 1000)) continue;

                if (heroItemCount > item.Count)
                {
                    heroItemCount -= item.Count;
                    continue;
                }
                heroItemCount = 0;
                break;
            }
            if (heroItemCount > 0)
                failed = true;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckBuff(ScriptExecuteContext ctx)
        {
            NPCSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            {
                if (!Enum.TryParse(param[0], true, out BuffType buffType))
                {
                    failed = true;
                    return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
                }
                failed = !player.HasBuff(buffType);
            }
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }
    }
}
