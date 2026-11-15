using Server.Scripting;
using System;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        /// <summary>Check arguments only; never activate a marker in the live world.</summary>
        internal bool ValidateAutoMarker(string line, ScriptSourceLine source)
        {
            ScriptAutoMarkerEntry marker = ScriptAutoMarkerCatalog.Find(Type, line, out ScriptAutoMarkerLine parsed);
            if (marker == null) return true; // Page-only markers and type ownership are checked separately.

            string error = marker.ValidateArguments?.Invoke(parsed);
            if (error == null) return true;

            ScriptDiagnostics.Report(ScriptDiagnosticKind.InvalidArguments, new ScriptParseContext
            {
                Line = source.Text,
                LineNumber = source.Number,
                FileName = source.FileName,
                Source = source.Source,
                PageKey = line,
            }, marker.Marker, error);
            return false;
        }

        // Shared with activation: preserve int parsing, casts and lookup order at runtime.
        private static bool TryParseAutoMapCoord(ScriptAutoMarkerLine line, out int x, out int y)
        {
            x = y = 0;
            return line.Args.Length == 3 && int.TryParse(line.Args[1], out x) && int.TryParse(line.Args[2], out y);
        }

        private static bool TryParseAutoMonsterIndex(ScriptAutoMarkerLine line, out int index)
        {
            return int.TryParse(line.Body, out index);
        }

        internal static string ValidateAutoMapCoordArguments(ScriptAutoMarkerLine line)
        {
            return TryParseAutoMapCoord(line, out _, out _) ? null : "MAPCOORD requires a map and two integer coordinates: (<map>,<x>,<y>).";
        }

        private static bool HasAutoCustomCommand(ScriptAutoMarkerLine line)
        {
            return !string.IsNullOrEmpty(line.Body);
        }

        internal static string ValidateAutoCustomCommandArguments(ScriptAutoMarkerLine line)
        {
            return HasAutoCustomCommand(line) ? null : "CUSTOMCOMMAND requires a nonempty command.";
        }

        internal static string ValidateAutoMonsterArguments(ScriptAutoMarkerLine line)
        {
            return TryParseAutoMonsterIndex(line, out _) ? null : "The monster index must be an integer.";
        }

        internal static string ValidateAutoTimeArguments(ScriptAutoMarkerLine line)
        {
            // Robot.AddRobot deliberately remains unchanged: its permissive registration is runtime policy.
            string[] values = line.Body.Split(',');
            if (values.Length != 5)
                return "TIME requires five values: (month,day,hour,minute,weekday); # matches any value.";

            if (!IsAutoTimeValue(values[0], 1, 12) || !IsAutoTimeValue(values[1], 1, 31) ||
                !IsAutoTimeValue(values[2], 0, 23) || !IsAutoTimeValue(values[3], 0, 59))
                return "TIME requires month 1-12, day 1-31, hour 0-23 and minute 0-59, each one or two digits or #, without spaces.";

            string weekday = values[4];
            if (weekday != "#" && (weekday.Length == 0 || weekday != weekday.Trim() ||
                !Enum.TryParse(weekday, true, out DayOfWeek day) || !Enum.IsDefined(typeof(DayOfWeek), day)))
                return "TIME weekday must be an English weekday, a defined numeric weekday (0-6), or #.";

            return null;
        }

        private static bool IsAutoTimeValue(string value, int minimum, int maximum)
        {
            if (value == "#") return true;
            if (value.Length < 1 || value.Length > 2) return false;
            foreach (char digit in value)
                if (digit < '0' || digit > '9') return false;
            return int.TryParse(value, out int number) && number >= minimum && number <= maximum;
        }
    }
}
