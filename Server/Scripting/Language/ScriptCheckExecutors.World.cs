using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.Scripting
{
    /// <summary>
    /// The checks that read the world: time, map position, script timers, dice, monsters, the game
    /// shop and conquest.
    /// </summary>
    public partial class ScriptSegment
    {
        // ---------------------------------------------------------------- time


        internal static ScriptOutcome Exec_CheckDay(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;

            var day = Envir.Now.DayOfWeek.ToString().ToUpper();
            var dayToCheck = param[0].ToUpper();

            failed = day != dayToCheck;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckHour(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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

        // ---------------------------------------------------------------- map


        internal static ScriptOutcome Exec_CheckMap(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
            List<string> param = ctx.Params;
            bool failed = false;

            var playerLight = Envir.Lights.ToString().ToUpperInvariant();
            var playerCheck = param.Count > 0 ? param[0]?.Trim().ToUpperInvariant() : string.Empty;
            failed = string.IsNullOrEmpty(playerCheck) || playerLight != playerCheck;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        internal static ScriptOutcome Exec_CheckRange(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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

        // ---------------------------------------------------------------- script state


        internal static ScriptOutcome Exec_CheckTimer(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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

        // ---------------------------------------------------------------- random


        internal static ScriptOutcome Exec_Random(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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

        // ---------------------------------------------------------------- monsters


        internal static ScriptOutcome Exec_Check(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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

        // ---------------------------------------------------------------- game shop


        internal static ScriptOutcome Exec_HasGT(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            PlayerObject player = ctx.Player;
            bool failed = false;

            if (player == null) return ScriptOutcome.Pass;

            failed = player.MyGuild == null || !player.MyGuild.HasGT;
            return failed ? ScriptOutcome.Fail : ScriptOutcome.Pass;
        }

        // ---------------------------------------------------------------- conquest


        internal static ScriptOutcome Exec_CheckConquest(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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

        internal static ScriptOutcome Exec_ConquestAvailable(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
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
            ScriptSegment seg = ctx.Segment;
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
    }
}
