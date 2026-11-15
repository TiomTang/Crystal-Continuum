using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        /// <summary>
        /// Reads a MAPCOORD line into the coordinate it would activate, or null when it is not one the
        /// loader would act on. Reading is all this does: publishing stays in Activate.
        /// </summary>
        internal static object PlanAutoMapCoord(ScriptAutoMarkerLine line)
        {
            if (!TryParseAutoMapCoord(line, out int x, out int y)) return null;

            Map map = Envir.MapList.FirstOrDefault(m => m.Info.FileName == line.Args[0]);

            if (map == null) return null;

            return (map, new Point((short)x, (short)y));
        }

        internal static void ActivateAutoMapCoord(object payload)
        {
            (Map map, Point point) = ((Map, Point))payload;

            if (!map.Info.ActiveCoords.Contains(point)) map.Info.ActiveCoords.Add(point);
        }

        internal static object PlanAutoCustomCommand(ScriptAutoMarkerLine line)
        {
            return HasAutoCustomCommand(line) ? line.Body : null;
        }

        internal static void ActivateAutoCustomCommand(object payload)
        {
            Envir.CustomCommands.Add((string)payload);
        }

        internal static object PlanAutoSpawn(ScriptAutoMarkerLine line)
        {
            if (!TryParseAutoMonsterIndex(line, out int index)) return null;

            return Envir.GetMonsterInfo((short)index);
        }

        internal static void ActivateAutoSpawn(object payload)
        {
            ((MonsterInfo)payload).HasSpawnScript = true;
        }

        internal static object PlanAutoDie(ScriptAutoMarkerLine line)
        {
            if (!TryParseAutoMonsterIndex(line, out int index)) return null;

            return Envir.GetMonsterInfo((short)index);
        }

        internal static void ActivateAutoDie(object payload)
        {
            ((MonsterInfo)payload).HasDieScript = true;
        }

        internal static object PlanAutoTime(ScriptAutoMarkerLine line)
        {
            return line.Line.ToUpperInvariant();
        }

        internal static void ActivateAutoTime(object payload)
        {
            Robot.AddRobot((string)payload);
        }
    }
}
