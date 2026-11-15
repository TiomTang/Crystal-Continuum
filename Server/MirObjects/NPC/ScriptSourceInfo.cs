using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Server.MirObjects
{
    /// <summary>Immutable load-time evidence. Never reopens a file when an execution fails.</summary>
    public sealed class ScriptSourceInfo
    {
        public readonly string FileName;
        public readonly string Text;
        public readonly string Context;
        public readonly string ContentVersion;
        public readonly int Line;

        public ScriptSourceInfo(string fileName, int line, string text, string context = null, string contentVersion = null)
        {
            FileName = fileName ?? string.Empty;
            Line = line;
            Text = text ?? string.Empty;
            Context = context ?? string.Empty;
            // Callers should supply the whole-file version. The fallback identifies this source excerpt.
            ContentVersion = contentVersion ?? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Text + "\n" + Context)));
        }
    }

    /// <summary>
    /// Invocation-local evidence, containing strings only: never attach a player, account or chat object.
    /// The owner can update progress while its execution scope is active. Reports take safe snapshots.
    /// </summary>
    public sealed class ScriptExecutionEvidence
    {
        public ScriptSourceInfo Source;
        public string ScriptIdentity;
        public string PageKey;
        public int Segment;
        public ScriptDiagnosticPhase Phase;
        public IList<string> Arguments;
        public string LastCompletedAction;
        public string CallPath;
        public bool HasSideEffects;

        internal ScriptExecutionEvidence Copy()
        {
            var copy = (ScriptExecutionEvidence)MemberwiseClone();
            copy.Arguments = Arguments == null ? null : new List<string>(Arguments);
            return copy;
        }
    }
}
