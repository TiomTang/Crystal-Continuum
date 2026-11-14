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
    /// The checks that read relations: name lists, guild membership, marriage and the group.
    /// </summary>
    public partial class NPCSegment
    {
        // ---------------------------------------------------------------- name lists


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

        // ---------------------------------------------------------------- guild


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

        // ---------------------------------------------------------------- relations


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

        // ---------------------------------------------------------------- groups


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
    }
}
