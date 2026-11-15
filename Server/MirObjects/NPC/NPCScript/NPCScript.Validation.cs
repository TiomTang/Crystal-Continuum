using Server.Scripting;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>Editor-only facts produced by the real loader, without registering a live NPC.</summary>
    public sealed class ScriptValidationResult
    {
        public IReadOnlyList<ScriptSourceLine> SourceLines { get; internal set; }
        public IReadOnlyList<ScriptPage> Pages { get; internal set; }
        public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; internal set; }
        public IReadOnlyList<ScriptValidationResult> CalledScripts { get; internal set; }
        public NPCScriptType Type { get; internal set; }
    }

    public partial class NPCScript
    {
        /// <summary>
        /// Kept for callers that already speak to the NPC host. The compile belongs to the generic
        /// validator, so this allocates no script, registers nothing and publishes no world state.
        /// </summary>
        public static ScriptValidationResult Validate(string text, string fileName, string envirPath,
            NPCScriptType type = NPCScriptType.Normal, string scriptDirectory = "NPCs")
        {
            return ScriptValidator.Validate(text, fileName, envirPath, type, scriptDirectory);
        }
    }
}
