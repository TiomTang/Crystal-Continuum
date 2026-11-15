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
    /// how it is read, and how its effect is published. Reading is separate from publishing so a tool
    /// can walk the same row without changing the world, and the loader never looks a map or a monster
    /// up twice for one line.
    /// </summary>
    public sealed class ScriptAutoMarkerEntry
    {
        public readonly NPCScriptType Type;
        public readonly string Marker;
        public readonly string Arguments;
        public readonly string Description;

        /// <summary>Reads the line; returns the effect to publish, or null when it does not apply.</summary>
        private readonly Func<ScriptAutoMarkerLine, object> _plan;

        /// <summary>Publishes one effect produced by <see cref="_plan"/>.</summary>
        private readonly Action<object> _activate;

        internal readonly Func<ScriptAutoMarkerLine, string> ValidateArguments;

        /// <summary>Editable page header; arguments use the same order as the loader.</summary>
        public string Key => ScriptPageKeyCatalog.Auto.Prefix + Marker + "(" + Arguments + ")]";

        internal ScriptAutoMarkerEntry(NPCScriptType type, string marker, Func<ScriptAutoMarkerLine, object> plan,
            Action<object> activate, string arguments = "", string description = "",
            Func<ScriptAutoMarkerLine, string> validateArguments = null)
        {
            Type = type;
            Marker = marker;
            _plan = plan;
            _activate = activate;
            ValidateArguments = validateArguments;
            Arguments = arguments;
            Description = description;
        }

        /// <summary>Reads the marker without publishing anything.</summary>
        internal object ReadPlan(ScriptAutoMarkerLine line) => _plan(line);

        /// <summary>Publishes one already-read marker.</summary>
        internal void Publish(object payload) => _activate(payload);
    }

    /// <summary>
    /// Markers of the automatic scripts. The loader walks the file once and asks this table for the
    /// marker on each line, so a new marker is a row here plus its handler, not another Contains test
    /// in the middle of the loader.
    /// </summary>
    public static class ScriptAutoMarkerCatalog
    {
        public static readonly ScriptAutoMarkerEntry MapCoord = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoPlayer, "MAPCOORD", line => NPCScript.PlanAutoMapCoord(line),
            payload => NPCScript.ActivateAutoMapCoord(payload),
            arguments: "<map>,<x>,<y>", description: "AutoPlayer: run when the player reaches this map coordinate.",
            validateArguments: NPCScript.ValidateAutoMapCoordArguments);

        public static readonly ScriptAutoMarkerEntry CustomCommand = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoPlayer, "CUSTOMCOMMAND", line => NPCScript.PlanAutoCustomCommand(line),
            payload => NPCScript.ActivateAutoCustomCommand(payload),
            arguments: "<command>", description: "AutoPlayer: register a custom command and run its page.",
            validateArguments: NPCScript.ValidateAutoCustomCommandArguments);

        public static readonly ScriptAutoMarkerEntry Spawn = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoMonster, "SPAWN", line => NPCScript.PlanAutoSpawn(line),
            payload => NPCScript.ActivateAutoSpawn(payload),
            arguments: "<monster index>", description: "AutoMonster: run when this monster definition spawns.",
            validateArguments: NPCScript.ValidateAutoMonsterArguments);

        public static readonly ScriptAutoMarkerEntry Die = new ScriptAutoMarkerEntry(
            NPCScriptType.AutoMonster, "DIE", line => NPCScript.PlanAutoDie(line),
            payload => NPCScript.ActivateAutoDie(payload),
            arguments: "<monster index>", description: "AutoMonster: run when this monster definition dies.",
            validateArguments: NPCScript.ValidateAutoMonsterArguments);

        public static readonly ScriptAutoMarkerEntry Time = new ScriptAutoMarkerEntry(
            NPCScriptType.Robot, "TIME", line => NPCScript.PlanAutoTime(line),
            payload => NPCScript.ActivateAutoTime(payload),
            arguments: "#,#,#,#,#", description: "Robot: month, day, hour, minute and English weekday; # matches any value.",
            validateArguments: NPCScript.ValidateAutoTimeArguments);

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
