using System;
using System.Text;

namespace Server.MirObjects
{
    public enum ScriptDiagnosticKind
    {
        UnknownCheck,
        UnknownAction,
        UnknownValue,
        UnknownBlock,
        MissingHandler,
        MalformedDirective,
        WrongArity,
        MissingPage,
        MissingScript,
        DuplicatePage,
        MalformedSection,
        LoadFailed,
        LegacyEncoding,
        RuntimeException,
        ChainLimit,
        InvalidArguments,
        MissingExecutor,
        DiagnosticsSuppressed,
    }

    public enum ScriptDiagnosticCategory { ScriptError, Unsupported, EngineFault, RuntimeFailure }
    public enum ScriptDiagnosticPhase { Load, Check, Action, Display }
    public enum ScriptDiagnosticEffect { SegmentDisabled, SegmentAborted, CallChainStopped, ScriptNotLoaded, DisplayFallback, Notice }

    /// <summary>A safe diagnostic snapshot. Mutating a returned instance cannot alter the collector.</summary>
    public sealed class ScriptDiagnostic
    {
        public ScriptDiagnosticKind Kind;
        public string FileName;
        public string PageKey;
        public string Keyword;
        public string Text;
        public string Message;
        public string ExceptionType;
        public int Line;
        public bool Runtime;
        public int Count = 1;

        public string Id;
        public string BatchId;
        public ScriptDiagnosticCategory Category;
        public ScriptDiagnosticPhase Phase;
        public ScriptDiagnosticEffect Effect;
        public string SourceVersion;
        public string SourceContext;
        public string EngineVersion;
        public DateTime FirstSeenUtc;
        public DateTime LastSeenUtc;
        public string ExceptionDetails;
        public string Arguments;
        public string LastCompletedAction;
        public string CallPath;
        public string ScriptIdentity;
        public int Segment;
        public bool PossibleSideEffects;
        public bool ExecutionEvidenceAvailable;
        public bool IsSummary;

        /// <summary>Occurrences since the previous emission; Count is the lifetime total in this batch.</summary>
        public int OccurrencesSinceLastFlush;
        /// <summary>True for an overflow report referencing the batch's bounded suppression record.</summary>
        public bool Suppressed;
        public int SuppressedCount;

        internal ScriptDiagnostic Copy()
        {
            return (ScriptDiagnostic)MemberwiseClone();
        }

        public string ToSummaryString()
        {
            return string.Format("[Script:{0}] batch={1} {2}/{3} {4} effect={5} {6}:{7} {8} segment={9} {10}: {11}{12} count={13} since-last={14}{15}",
                Id, BatchId, Category, Phase, Kind, Effect, FileName, Line, PageKey, Segment, Keyword, Message,
                Runtime ? " [runtime]" : string.Empty, Count, OccurrencesSinceLastFlush,
                IsSummary ? " [summary]" : string.Empty);
        }

        public string ToDetailString()
        {
            var text = new StringBuilder(ToSummaryString());
            text.AppendLine().Append("First UTC: ").Append(FirstSeenUtc.ToString("O"))
                .Append("; last UTC: ").Append(LastSeenUtc.ToString("O"));
            text.AppendLine().Append("Engine: ").Append(EngineVersion).Append("; source version: ").Append(SourceVersion);
            if (!string.IsNullOrEmpty(ScriptIdentity)) text.AppendLine().Append("Script identity: ").Append(ScriptIdentity);
            text.AppendLine().Append("Source: ").Append(Text);
            if (!string.IsNullOrEmpty(SourceContext)) text.AppendLine().Append("Context: ").Append(SourceContext);
            if (!string.IsNullOrEmpty(Arguments)) text.AppendLine().Append("Arguments: ").Append(Arguments);
            if (!string.IsNullOrEmpty(LastCompletedAction)) text.AppendLine().Append("Last completed action: ").Append(LastCompletedAction);
            if (!string.IsNullOrEmpty(CallPath)) text.AppendLine().Append("Call path: ").Append(CallPath);
            text.AppendLine().Append("Possible side effects: ").Append(PossibleSideEffects ? "True" :
                ExecutionEvidenceAvailable ? "False" : "Unknown (execution evidence unavailable)");
            if (!string.IsNullOrEmpty(ExceptionType)) text.AppendLine().Append("Exception type: ").Append(ExceptionType);
            if (!string.IsNullOrEmpty(ExceptionDetails)) text.AppendLine().Append(ExceptionDetails);
            return text.ToString();
        }

        public override string ToString()
        {
            return ToSummaryString();
        }
    }
}
