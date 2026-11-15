using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>Which host is compiling one script file.</summary>
    internal enum ScriptCompileMode
    {
        /// <summary>Read-only: no script is registered and no world state is published.</summary>
        Tool,

        /// <summary>World load: called scripts are bound and automatic markers are applied afterwards.</summary>
        Live,
    }

    /// <summary>An automatic marker read while compiling, waiting for the loader to publish its effect.</summary>
    internal sealed class ScriptAutoMarkerPlan
    {
        public readonly ScriptAutoMarkerEntry Entry;
        public readonly ScriptAutoMarkerLine Line;
        public readonly object Payload;

        public ScriptAutoMarkerPlan(ScriptAutoMarkerEntry entry, ScriptAutoMarkerLine line, object payload)
        {
            Entry = entry;
            Line = line;
            Payload = payload;
        }
    }

    /// <summary>
    /// One file compiled into pages. The pages themselves land on the script instance, because a page
    /// chain must see the pages already parsed for the same file; everything that changes the world
    /// (registering a called script, coordinates, monster flags, custom commands, robots) is listed
    /// separately here: tools stop at this result, the world loader continues with it.
    /// </summary>
    internal sealed class ScriptCompilation
    {
        public string FileName;
        public NPCScriptType Type;
        public List<ScriptSourceLine> SourceLines;
        public List<string> Lines;
        public readonly List<ScriptAutoMarkerPlan> AutoPlans = new List<ScriptAutoMarkerPlan>();
    }
}
