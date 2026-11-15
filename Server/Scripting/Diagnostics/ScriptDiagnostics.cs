using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Server.Scripting
{

    /// <summary>
    /// Collects NPC script problems while scripts load, so an unknown keyword is never dropped
    /// silently: the server logs it and tools can read the same collection afterwards.
    /// </summary>
    public static class ScriptDiagnostics
    {
        /// <summary>Upper bound, so a broken script cannot grow the collection without limit.</summary>
        public const int MaxEntries = 5000;

        private static readonly object Sync = new object();
        private sealed class Entry
        {
            internal ScriptDiagnostic Diagnostic;
            internal int EmittedCount;
            internal bool Log;
        }

        private sealed class ExecutionScope : IDisposable
        {
            internal readonly ScriptExecutionEvidence Evidence;
            internal readonly ExecutionScope Previous;
            private readonly int _thread = Environment.CurrentManagedThreadId;
            private bool _disposed;

            internal ExecutionScope(ScriptExecutionEvidence evidence)
            {
                Evidence = evidence;
                Previous = _execution;
                _execution = this;
            }

            public void Dispose()
            {
                if (_thread != Environment.CurrentManagedThreadId) return;
                _disposed = true;
                while (_execution != null && _execution._disposed) _execution = _execution.Previous;
            }
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        [ThreadStatic] private static ExecutionScope _execution;
        [ThreadStatic] private static bool _publishing;
        private static string _batchId = Guid.NewGuid().ToString("N");
        private static readonly string EngineVersion = (typeof(ScriptDiagnostics).Assembly.GetName().Version?.ToString() ?? "unknown") +
            "/" + typeof(ScriptDiagnostics).Module.ModuleVersionId.ToString("N");
        private static ScriptDiagnostic _overflow;
        private static int _emittedSuppressed;
        private static int _suppressed;

        /// <summary>Optional sink for warnings; the server points this at its log.</summary>
        public static Action<string> Sink;

        /// <summary>Structured first reports and periodic summaries. Every recipient receives a copy.</summary>
        public static Action<ScriptDiagnostic> DetailSink;

        /// <summary>Every execution-affecting runtime report, including repeats and overflow references.</summary>
        public static event Action<ScriptDiagnostic> Reported;

        public static string BatchId { get { lock (Sync) { return _batchId; } } }

        /// <summary>Thread-local, nestable invocation evidence; dispose on the creating thread.</summary>
        public static IDisposable BeginExecution(ScriptExecutionEvidence evidence)
        {
            return new ExecutionScope(evidence);
        }

        /// <summary>
        /// Also parse the pages no button reaches, so a script author sees mistakes in pages nobody has
        /// wired up yet. Set false to skip the extra work.
        /// </summary>
        public static bool DeepScan = true;

        /// <summary>Pages the deep scan looked at, for tests and startup reports.</summary>
        public static int DeepScanned;

        public static int Count
        {
            get { lock (Sync) { return Entries.Count; } }
        }

        /// <summary>Number of reports dropped after the limit was reached.</summary>
        public static int Suppressed
        {
            get { lock (Sync) { return _suppressed; } }
        }

        public static void Clear()
        {
            Flush();
            List<ScriptDiagnostic> summaries;
            lock (Sync)
            {
                // Capture pending summaries before resetting; callbacks never run under the collector lock.
                summaries = PendingSummaries();
                Entries.Clear();
                _suppressed = 0;
                _emittedSuppressed = 0;
                _overflow = null;
                DeepScanned = 0;
                _batchId = Guid.NewGuid().ToString("N");
            }
            foreach (ScriptDiagnostic summary in summaries) Publish(summary);
        }

        public static IReadOnlyList<ScriptDiagnostic> Snapshot()
        {
            List<ScriptDiagnostic> list;

            lock (Sync)
            {
                list = new List<ScriptDiagnostic>(Entries.Count + (_overflow == null ? 0 : 1));
                foreach (Entry entry in Entries.Values) list.Add(entry.Diagnostic.Copy());
                if (_overflow != null) list.Add(_overflow.Copy());
            }

            list.Sort((a, b) => string.CompareOrdinal(a.FileName + a.PageKey + a.Keyword, b.FileName + b.PageKey + b.Keyword));

            return list;
        }

        /// <summary>
        /// Records a problem found while a file loads. Entries are keyed without the line number, so a
        /// mistake repeated on ten lines stays one entry and keeps the first line it was seen on.
        /// </summary>
        public static ScriptDiagnostic Report(ScriptDiagnosticKind kind, ScriptParseContext context, string keyword, string message,
            Exception exception = null)
        {
            ScriptSourceInfo source = context?.Source;
            if (source == null && context != null)
                source = new ScriptSourceInfo(context.FileName ?? context.Segment?.Page?.FileName,
                    context.LineNumber, context.Line);
            ScriptDiagnostic diagnostic = Create(kind, source, context?.PageKey ?? context?.Segment?.Page?.Key,
                keyword, message, false, null, null, exception, null);

            // Load-time missing pages are visible too; deep scanning must not hide actionable evidence.
            return Collect(diagnostic, true);
        }

        /// <summary>
        /// Records a run-time problem. Script identity, exception type and reason distinguish failures
        /// on otherwise identical page keys, while repeat visits increment the existing entry.
        /// </summary>
        public static ScriptDiagnostic ReportRuntime(ScriptDiagnosticKind kind, string pageKey, string keyword, string message,
            string scriptIdentity = null, string exceptionType = null, Exception exception = null, ScriptSourceInfo source = null)
        {
            ScriptExecutionEvidence evidence = _execution?.Evidence;
            if (ScriptInteraction.Current != null)
            {
                var progress = ScriptInteraction.Current.CreateEvidence(evidence);
                if (evidence != null) progress.Phase = evidence.Phase;
                evidence = progress;
            }
            ScriptDiagnostic diagnostic = Create(kind, source ?? evidence?.Source, pageKey ?? evidence?.PageKey,
                keyword, message, true, scriptIdentity ?? evidence?.ScriptIdentity, exceptionType, exception, evidence);
            ScriptDiagnostic result = Collect(diagnostic, true);
            if (AffectsExecution(result) && !_publishing) Notify(Reported, result);
            return result;
        }

        internal static ScriptExecutionEvidence CaptureExecution()
        {
            return _execution?.Evidence?.Copy();
        }

        private static ScriptDiagnostic Create(ScriptDiagnosticKind kind, ScriptSourceInfo source, string pageKey,
            string keyword, string message, bool runtime, string identity, string exceptionType,
            Exception exception, ScriptExecutionEvidence evidence)
        {
            IList<string> args = evidence?.Arguments;
            var phase = runtime ? evidence?.Phase ?? ScriptDiagnosticPhase.Action : ScriptDiagnosticPhase.Load;
            if (kind == ScriptDiagnosticKind.UnknownValue) phase = evidence?.Phase ?? ScriptDiagnosticPhase.Display;
            return new ScriptDiagnostic
            {
                Kind = kind,
                Category = CategoryOf(kind),
                Phase = phase,
                Effect = EffectOf(kind, phase, runtime),
                // Local editor navigation needs the exact path, not a redacted log identifier.
                FileName = ScriptValidationScope.IsActive ? source?.FileName ?? identity
                    : ScriptDiagnosticText.Safe(source?.FileName ?? identity, 512),
                PageKey = ScriptDiagnosticText.Safe(pageKey, 256),
                Keyword = ScriptDiagnosticText.Safe(keyword, 128),
                Text = ScriptDiagnosticText.Safe(source?.Text, 2048),
                SourceContext = ScriptDiagnosticText.Safe(source?.Context, 4096, multiline: true),
                SourceVersion = ScriptDiagnosticText.Version(source?.ContentVersion),
                Line = source?.Line ?? 0,
                Message = ScriptDiagnosticText.Safe(message, 2048, args),
                ExceptionType = ScriptDiagnosticText.Safe(exception?.GetType().FullName ?? exceptionType, 256),
                ExceptionDetails = ScriptDiagnosticText.Exception(exception, args),
                Runtime = runtime,
                EngineVersion = EngineVersion,
                Arguments = ScriptDiagnosticText.Arguments(args),
                LastCompletedAction = ScriptDiagnosticText.Safe(evidence?.LastCompletedAction, 256),
                CallPath = ScriptDiagnosticText.Safe(evidence?.CallPath ?? identity, 2048),
                ScriptIdentity = ScriptDiagnosticText.Safe(identity, 512),
                Segment = evidence?.Segment ?? 0,
                PossibleSideEffects = evidence?.HasSideEffects ?? false,
                ExecutionEvidenceAvailable = evidence != null,
                OccurrencesSinceLastFlush = 1,
            };
        }

        private static ScriptDiagnosticCategory CategoryOf(ScriptDiagnosticKind kind)
        {
            switch (kind)
            {
                case ScriptDiagnosticKind.UnknownCheck:
                case ScriptDiagnosticKind.UnknownAction:
                case ScriptDiagnosticKind.UnknownValue:
                case ScriptDiagnosticKind.UnknownBlock:
                case ScriptDiagnosticKind.LegacyEncoding:
                    return ScriptDiagnosticCategory.Unsupported;
                case ScriptDiagnosticKind.MissingHandler:
                case ScriptDiagnosticKind.MissingExecutor:
                case ScriptDiagnosticKind.DiagnosticsSuppressed:
                    return ScriptDiagnosticCategory.EngineFault;
                case ScriptDiagnosticKind.RuntimeException:
                case ScriptDiagnosticKind.LoadFailed:
                case ScriptDiagnosticKind.ChainLimit:
                    return ScriptDiagnosticCategory.RuntimeFailure;
                default:
                    return ScriptDiagnosticCategory.ScriptError;
            }
        }

        private static ScriptDiagnosticEffect EffectOf(ScriptDiagnosticKind kind, ScriptDiagnosticPhase phase, bool runtime)
        {
            if (kind == ScriptDiagnosticKind.UnknownValue && phase != ScriptDiagnosticPhase.Display)
                return runtime ? ScriptDiagnosticEffect.CallChainStopped : ScriptDiagnosticEffect.SegmentDisabled;
            if (kind == ScriptDiagnosticKind.UnknownValue || kind == ScriptDiagnosticKind.LegacyEncoding)
                return ScriptDiagnosticEffect.DisplayFallback;
            if (kind == ScriptDiagnosticKind.DiagnosticsSuppressed)
                return ScriptDiagnosticEffect.Notice;
            if (kind == ScriptDiagnosticKind.LoadFailed || kind == ScriptDiagnosticKind.MissingScript)
                return ScriptDiagnosticEffect.ScriptNotLoaded;
            if (kind == ScriptDiagnosticKind.ChainLimit || (runtime &&
                (kind == ScriptDiagnosticKind.MissingPage || kind == ScriptDiagnosticKind.MissingScript)))
                return ScriptDiagnosticEffect.CallChainStopped;
            if (runtime) return ScriptDiagnosticEffect.SegmentAborted;
            if (kind == ScriptDiagnosticKind.UnknownCheck || kind == ScriptDiagnosticKind.UnknownAction ||
                kind == ScriptDiagnosticKind.UnknownBlock || kind == ScriptDiagnosticKind.MalformedDirective || kind == ScriptDiagnosticKind.WrongArity ||
                kind == ScriptDiagnosticKind.TrailingBlockText || kind == ScriptDiagnosticKind.InvalidArguments ||
                kind == ScriptDiagnosticKind.MissingHandler || kind == ScriptDiagnosticKind.MissingExecutor)
                return ScriptDiagnosticEffect.SegmentDisabled;
            return ScriptDiagnosticEffect.Notice;
        }

        private static bool AffectsExecution(ScriptDiagnostic diagnostic)
        {
            return diagnostic.Runtime && !diagnostic.IsSummary &&
                (diagnostic.Effect == ScriptDiagnosticEffect.SegmentAborted || diagnostic.Effect == ScriptDiagnosticEffect.CallChainStopped ||
                 diagnostic.Effect == ScriptDiagnosticEffect.SegmentDisabled || diagnostic.Effect == ScriptDiagnosticEffect.ScriptNotLoaded);
        }

        private static ScriptDiagnostic Collect(ScriptDiagnostic diagnostic, bool log)
        {
            if (ScriptValidationScope.Current != null) return ScriptValidationScope.Current.Collect(diagnostic);
            // Every key component is bounded and safe; never keep raw account/input data in the index.
            string key = KeyPart(diagnostic.Kind.ToString()) + KeyPart(diagnostic.FileName) + KeyPart(diagnostic.PageKey) +
                KeyPart(diagnostic.Keyword) + KeyPart(diagnostic.Text) + KeyPart(diagnostic.SourceVersion) +
                KeyPart(diagnostic.Runtime.ToString()) + KeyPart(diagnostic.Phase.ToString()) + KeyPart(diagnostic.Effect.ToString()) +
                KeyPart(diagnostic.Segment.ToString()) + KeyPart(diagnostic.ExceptionType) + KeyPart(diagnostic.Message) +
                KeyPart(diagnostic.ScriptIdentity) + KeyPart(diagnostic.CallPath);
            ScriptDiagnostic emission = null;
            ScriptDiagnostic result;
            lock (Sync)
            {
                DateTime now = DateTime.UtcNow;
                diagnostic.BatchId = _batchId;
                diagnostic.FirstSeenUtc = diagnostic.LastSeenUtc = now;
                Entry existing;
                if (Entries.TryGetValue(key, out existing))
                {
                    existing.Diagnostic.Count = Increment(existing.Diagnostic.Count);
                    existing.Diagnostic.LastSeenUtc = now;
                    // Return this invocation's evidence without mutating the stored first occurrence.
                    diagnostic.Id = existing.Diagnostic.Id;
                    diagnostic.Count = existing.Diagnostic.Count;
                    diagnostic.FirstSeenUtc = existing.Diagnostic.FirstSeenUtc;
                    result = diagnostic;
                }
                else if (Entries.Count >= MaxEntries)
                {
                    _suppressed = Increment(_suppressed);
                    if (_overflow == null)
                    {
                        _overflow = new ScriptDiagnostic
                        {
                            Id = Guid.NewGuid().ToString("N"), BatchId = _batchId,
                            Kind = ScriptDiagnosticKind.DiagnosticsSuppressed, Category = ScriptDiagnosticCategory.EngineFault,
                            Phase = ScriptDiagnosticPhase.Load, Effect = ScriptDiagnosticEffect.Notice,
                            Message = "Diagnostic capacity reached; additional reports are not retained. References share this suppression ID, not a source location.",
                            EngineVersion = EngineVersion, IsSummary = true, Suppressed = true,
                            FirstSeenUtc = now,
                        };
                    }
                    _overflow.LastSeenUtc = now;
                    _overflow.Count = _overflow.SuppressedCount = _suppressed;
                    if (_emittedSuppressed == 0)
                    {
                        emission = _overflow.Copy();
                        emission.OccurrencesSinceLastFlush = 1;
                        _emittedSuppressed = 1;
                    }
                    diagnostic.Id = _overflow.Id;
                    diagnostic.Suppressed = true;
                    diagnostic.SuppressedCount = _suppressed;
                    result = diagnostic;
                }
                else
                {
                    diagnostic.Id = Guid.NewGuid().ToString("N");
                    Entries.Add(key, new Entry { Diagnostic = diagnostic.Copy(), EmittedCount = 1, Log = log });
                    result = diagnostic;
                    if (log) emission = diagnostic.Copy();
                }
            }
            if (emission != null) Publish(emission);
            return result;
        }

        private static int Increment(int value) { return value == int.MaxValue ? value : value + 1; }

        /// <summary>Emit pending repeats and overflow deltas. The host decides the periodic interval.</summary>
        public static void Flush()
        {
            List<ScriptDiagnostic> summaries;
            lock (Sync) { summaries = PendingSummaries(); }
            foreach (ScriptDiagnostic summary in summaries) Publish(summary);
        }

        private static List<ScriptDiagnostic> PendingSummaries()
        {
            var summaries = new List<ScriptDiagnostic>();
            foreach (Entry entry in Entries.Values)
            {
                if (entry.Diagnostic.Count <= entry.EmittedCount) continue;
                if (entry.Log)
                {
                    ScriptDiagnostic summary = entry.Diagnostic.Copy();
                    summary.IsSummary = true;
                    summary.OccurrencesSinceLastFlush = summary.Count - entry.EmittedCount;
                    summaries.Add(summary);
                }
                entry.EmittedCount = entry.Diagnostic.Count;
            }
            if (_overflow != null && _suppressed > _emittedSuppressed)
            {
                ScriptDiagnostic summary = _overflow.Copy();
                summary.OccurrencesSinceLastFlush = _suppressed - _emittedSuppressed;
                summaries.Add(summary);
                _emittedSuppressed = _suppressed;
            }
            return summaries;
        }

        private static void Publish(ScriptDiagnostic diagnostic)
        {
            if (_publishing) { Fallback("Recursive diagnostic output suppressed", null); return; }
            _publishing = true;
            try
            {
                Action<string> sink = Sink;
                if (sink != null)
                    foreach (Action<string> callback in sink.GetInvocationList())
                        try { callback(diagnostic.IsSummary ? diagnostic.ToSummaryString() : diagnostic.ToDetailString()); }
                        catch (Exception exception) { Fallback("Script diagnostic sink failed", exception); }
                Notify(DetailSink, diagnostic);
            }
            finally { _publishing = false; }
        }

        private static void Notify(Action<ScriptDiagnostic> callbacks, ScriptDiagnostic diagnostic)
        {
            if (callbacks == null) return;
            bool alreadyPublishing = _publishing;
            _publishing = true;
            try
            {
                foreach (Action<ScriptDiagnostic> callback in callbacks.GetInvocationList())
                    try { callback(diagnostic.Copy()); }
                    catch (Exception exception) { Fallback("Script diagnostic callback failed", exception); }
            }
            finally { _publishing = alreadyPublishing; }
        }

        private static void Fallback(string message, Exception exception)
        {
            try { Trace.TraceWarning(message + (exception == null ? string.Empty : ": " + exception.GetType().FullName)); }
            catch (Exception) { /* Logging must never change fail-closed execution. */ }
        }

        private static string KeyPart(string value)
        {
            value = value ?? string.Empty;
            return value.Length + ":" + value;
        }

        /// <summary>One-line summary for load-time logging.</summary>
        public static string Describe()
        {
            lock (Sync)
            {
                if (Entries.Count == 0) return string.Empty;

                HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (Entry entry in Entries.Values)
                    files.Add(entry.Diagnostic.FileName);

                var summary = new StringBuilder();
                summary.Append(Entries.Count).Append(" script problem(s) in ").Append(files.Count).Append(" file(s)");

                if (_suppressed > 0)
                    summary.Append(", ").Append(_suppressed).Append(" more suppressed");

                return summary.ToString();
            }
        }
    }
}
