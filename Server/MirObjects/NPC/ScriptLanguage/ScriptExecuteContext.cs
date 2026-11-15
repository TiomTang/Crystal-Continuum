using System.Collections.Generic;
using Server.MirEnvir;

namespace Server.MirObjects
{
    /// <summary>Mutable control flow belongs to one invocation, never to a shared page.</summary>
    internal sealed class ScriptCallContext
    {
        public bool BreakRequested;
        public bool ConditionAborted;
        public bool ExecutionAborted;
        public string LastCompletedAction;
        public bool HasSideEffects;
        public ScriptDiagnostic FailureDiagnostic;

        public void Abort(ScriptDiagnostic diagnostic)
        {
            ExecutionAborted = true;
            ConditionAborted = true;
            if (FailureDiagnostic == null) FailureDiagnostic = diagnostic;
        }
    }

    /// <summary>What an executor reports back to the segment loop.</summary>
    public enum ScriptOutcome
    {
        /// <summary>Condition holds, or the action ran normally.</summary>
        Pass,

        /// <summary>Condition does not hold. The segment fails.</summary>
        Fail,

        /// <summary>Stop this list using the executor's legacy return semantics; not necessarily an error.</summary>
        Abort,

        /// <summary>A known parameter contract was violated, unlike a normal business early return.</summary>
        InvalidArguments,
    }

    /// <summary>
    /// State that has to outlive one action: composing a mail is spread over several actions
    /// (COMPOSEMAIL, ADDMAILITEM, ADDMAILGOLD, SENDMAIL), so the half-built message lives here.
    /// </summary>
    public sealed class ScriptActionState
    {
        public MailInfo Mail;
    }

    /// <summary>
    /// Everything a keyword executor needs. The bodies were moved out of the switch in NPCSegment one
    /// for one, so the same context has to carry the player, the monster, the arguments and the
    /// segment the check or action belongs to.
    /// </summary>
    public sealed class ScriptExecuteContext
    {
        public NPCSegment Segment;

        internal ScriptCallContext Call;

        /// <summary>The check being evaluated, null while an action runs.</summary>
        public NPCChecks Check;

        /// <summary>The action being run, null while a check is evaluated.</summary>
        public NPCActions Action;

        /// <summary>The action list the current action belongs to.</summary>
        public IList<NPCActions> Acts;

        public PlayerObject Player;

        public MonsterObject Monster;

        /// <summary>Arguments with script variables already substituted.</summary>
        public List<string> Params;

        /// <summary>Shared across the actions of one segment run.</summary>
        public ScriptActionState State;

        /// <summary>Upper cased value name with its arguments stripped, for example CONQUESTGATE().</summary>
        public string InnerMatch;

        /// <summary>Capture groups of the &lt;$VALUE(A1)&gt; form.</summary>
        public System.Text.RegularExpressions.Match VariableMatch;

        /// <summary>Capture groups of the &lt;$VALUE(A)&gt; form.</summary>
        public System.Text.RegularExpressions.Match OneValueMatch;

        /// <summary>Capture groups of the &lt;$VALUE(A,B)&gt; form.</summary>
        public System.Text.RegularExpressions.Match TwoValueMatch;
    }
}
