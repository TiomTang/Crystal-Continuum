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
    /// The values a monster script can print; the engine registered a narrower set for monsters.
    /// </summary>
    public partial class ScriptSegment
    {
        internal static string ExecMonsterValue_OUTPUT(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = seg.FindVariable(monster, "%" + ctx.VariableMatch.Groups[2].Captures[0].Value.ToUpper());
            return newValue;
        }

        internal static string ExecMonsterValue_USERNAME(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.Name;
            return newValue;
        }

        internal static string ExecMonsterValue_LEVEL(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.Level.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_MAP(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.CurrentMap.Info.FileName;
            return newValue;
        }

        internal static string ExecMonsterValue_X_COORD(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.CurrentLocation.X.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_Y_COORD(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.CurrentLocation.Y.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_HP(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.HP.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_MAXHP(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = monster.Stats[Stat.HP].ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_DATE(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Envir.Now.ToShortDateString();
            return newValue;
        }

        internal static string ExecMonsterValue_MAPLIGHT(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Envir.Lights.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_USERCOUNT(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Envir.PlayerCount.ToString(CultureInfo.InvariantCulture);
            return newValue;
        }

        internal static string ExecMonsterValue_GUILDWARTIME(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Settings.Guild_WarTime.ToString();
            return newValue;
        }

        internal static string ExecMonsterValue_GUILDWARFEE(ScriptExecuteContext ctx)
        {
            ScriptSegment seg = ctx.Segment;
            MonsterObject monster = ctx.Monster;
            string newValue = string.Empty;

            if (monster == null) return string.Empty;

            newValue = Settings.Guild_WarCost.ToString();
            return newValue;
        }
    }
}
