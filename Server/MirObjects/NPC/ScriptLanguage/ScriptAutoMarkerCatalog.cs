using System;

namespace Server.MirObjects
{
    /// <summary>One automatic marker line, already split into the parts its handler needs.</summary>
    public struct ScriptAutoMarkerLine
    {
        /// <summary>The whole line, as written.</summary>
        public readonly string Line;

        /// <summary>The marker name without its "[@_" prefix or arguments, upper case.</summary>
        public readonly string Marker;

        /// <summary>Text inside the first parentheses, or empty when the marker has none.</summary>
        public readonly string Body;

        /// <summary>Body split on commas and trimmed.</summary>
        public readonly string[] Args;

        public ScriptAutoMarkerLine(string line, string marker, string body, string[] args)
        {
            Line = line;
            Marker = marker;
            Body = body;
            Args = args;
        }
    }

    /// <summary>
    /// One automatic marker the loader acts on: which script type owns it, the marker in the page key,
    /// and the effect. A marker that only opens a page, such as LEVELUP, is not listed here on purpose.
    /// </summary>
    public sealed class ScriptAutoMarkerEntry
    {
        public readonly NPCScriptType Type;
        public readonly string Marker;
        public readonly Func<NPCScript, ScriptAutoMarkerLine, bool> Apply;

        public ScriptAutoMarkerEntry(NPCScriptType type, string marker, Func<NPCScript, ScriptAutoMarkerLine, bool> apply)
        {
            Type = type;
            Marker = marker;
            Apply = apply;
        }
    }

    /// <summary>
    /// Markers of the automatic scripts. The loader walks the file once and asks this table for the
    /// marker on each line, so a new marker is a row here plus its handler, not another Contains test
    /// in the middle of the loader.
    /// </summary>
    public static class ScriptAutoMarkerCatalog
    {
        public static readonly ScriptAutoMarkerEntry MapCoord = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoPlayer, "MAPCOORD", (script, line) => script.ApplyAutoMapCoord(line));

        public static readonly ScriptAutoMarkerEntry CustomCommand = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoPlayer, "CUSTOMCOMMAND", (script, line) => script.ApplyAutoCustomCommand(line));

        public static readonly ScriptAutoMarkerEntry Spawn = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoMonster, "SPAWN", (script, line) => script.ApplyAutoSpawn(line));

        public static readonly ScriptAutoMarkerEntry Die = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoMonster, "DIE", (script, line) => script.ApplyAutoDie(line));

        public static readonly ScriptAutoMarkerEntry Time = new ScriptAutoMarkerEntry(
            NPCScriptType.Robot, "TIME", (script, line) => script.ApplyAutoTime(line));

        public static readonly ScriptAutoMarkerEntry[] Entries = { MapCoord, CustomCommand, Spawn, Die, Time };

        /// <summary>Marker that applies to this script type, or null when the line only opens a page.</summary>
        public static ScriptAutoMarkerEntry Find(NPCScriptType type, string line, out ScriptAutoMarkerLine parsed)
        {
            parsed = default(ScriptAutoMarkerLine);

            if (!TryParse(line, out parsed)) return null;

            foreach (ScriptAutoMarkerEntry entry in Entries)
            {
                if (entry.Type == type && entry.Marker == parsed.Marker) return entry;
            }

            return null;
        }

        /// <summary>Reads "[@_MARKER(...)]" into its marker and arguments.</summary>
        public static bool TryParse(string line, out ScriptAutoMarkerLine parsed)
        {
            parsed = default(ScriptAutoMarkerLine);

            string marker;
            string[] args;

            if (!ScriptPageKeyCatalog.TryParseAuto(line, out marker, out args)) return false;

            string key;

            ScriptPageKeyCatalog.TryParseHeader(line, out key);

            string body = key.Substring(ScriptPageKeyCatalog.Auto.Prefix.Length, key.Length - ScriptPageKeyCatalog.Auto.Prefix.Length - 1);
            int open = body.IndexOf('(');
            int close = open < 0 ? -1 : body.IndexOf(')', open + 1);
            string inner = close > open ? body.Substring(open + 1, close - open - 1) : string.Empty;

            parsed = new ScriptAutoMarkerLine(line, marker, inner, args);

            return true;
        }
    }
}
