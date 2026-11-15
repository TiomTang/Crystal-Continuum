using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>
    /// Entry point for one NPC script line. Each keyword kind has a catalog of its own that owns the
    /// entries and the lookups; this class only picks the catalog for the line, decides the outcome and
    /// records what could not be resolved.
    /// </summary>
    public static class ScriptKeywordRegistry
    {
        /// <summary>Keywords allowed inside #IF.</summary>
        public static IReadOnlyList<ScriptKeywordEntry> Checks
        {
            get { return ScriptCheckCatalog.Entries; }
        }

        /// <summary>Keywords allowed inside #ACT and #ELSEACT.</summary>
        public static IReadOnlyList<ScriptKeywordEntry> Actions
        {
            get { return ScriptActionCatalog.Entries; }
        }

        /// <summary>Variables replaced inside #SAY, written as &lt;$NAME&gt;.</summary>
        public static IReadOnlyList<ScriptKeywordEntry> Variables
        {
            get { return ScriptValueCatalog.PlayerEntries; }
        }

        /// <summary>Variables available when a script runs for a monster.</summary>
        public static IReadOnlyList<ScriptKeywordEntry> MonsterVariables
        {
            get { return ScriptValueCatalog.MonsterEntries; }
        }

        /// <summary>
        /// Finds the executor registered for an action type. When a monster runs the script and the entry
        /// carries a monster variant, that one is used instead.
        /// </summary>
        public static Func<ScriptExecuteContext, ScriptOutcome> ActionExecutor(ScriptActionKind type, bool forMonster)
        {
            ScriptKeywordEntry entry = ScriptActionCatalog.Find(type);

            if (entry == null) return null;

            if (forMonster && entry.MonsterExecutor != null) return entry.MonsterExecutor;

            return entry.Executor;
        }

        /// <summary>
        /// Finds the executor registered for a condition type. The same table entry that names the
        /// keyword also runs it, so a condition cannot be parsed without a behaviour behind it.
        /// </summary>
        public static Func<ScriptExecuteContext, ScriptOutcome> CheckExecutor(ScriptCheckKind type)
        {
            ScriptKeywordEntry entry = ScriptCheckCatalog.Find(type);

            return entry == null ? null : entry.Executor;
        }

        public static bool TryGetCheck(string name, out ScriptKeywordEntry keyword)
        {
            keyword = ScriptCheckCatalog.Find(name);

            return keyword != null;
        }

        public static bool TryGetAction(string name, out ScriptKeywordEntry keyword)
        {
            keyword = ScriptActionCatalog.Find(name);

            return keyword != null;
        }

        public static bool TryGetVariable(string name, out ScriptKeywordEntry keyword)
        {
            keyword = ScriptValueCatalog.FindPlayer(name);

            return keyword != null;
        }

        public static bool TryGetMonsterVariable(string name, out ScriptKeywordEntry keyword)
        {
            keyword = ScriptValueCatalog.FindMonster(name);

            return keyword != null;
        }

        /// <summary>
        /// Parses one line against the catalog for its kind. Unrecognised keywords and incomplete lines
        /// are recorded in <see cref="ScriptDiagnostics"/> instead of being dropped silently.
        /// </summary>
        public static ScriptParseOutcome Dispatch(ScriptKeywordKind kind, ScriptParseContext context)
        {
            if (context == null || context.Parts == null || context.Parts.Length == 0) return ScriptParseOutcome.Skipped;

            if (context.Source == null)
                context.Source = new ScriptSourceInfo(context.FileName ?? context.Segment?.Page?.FileName,
                    context.LineNumber, context.Line);

            ScriptKeywordEntry keyword = Find(kind, context.Parts[0]);

            if (keyword == null)
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(KindOf(kind), context, context.Parts[0],
                    string.Format("Unknown {0} \"{1}\"", KindName(kind), context.Parts[0])));

                return ScriptParseOutcome.Unknown;
            }

            if (keyword.Handler == null)
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingHandler, context, keyword.Name,
                    string.Format("\"{0}\" has no parser", keyword.Name)));

                return ScriptParseOutcome.Unknown;
            }

            if (context.Parts.Length < keyword.MinParts)
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(ScriptDiagnosticKind.WrongArity, context, keyword.Name,
                    string.Format("\"{0}\" needs at least {1} argument(s)", keyword.Name, keyword.MinParts - 1)));

                return ScriptParseOutcome.Skipped;
            }

            if (kind != ScriptKeywordKind.Check && kind != ScriptKeywordKind.Action) return keyword.Handler(context);

            bool check = kind == ScriptKeywordKind.Check;
            if ((check ? !keyword.Check.HasValue : !keyword.Action.HasValue) || keyword.Executor == null)
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingExecutor, context, keyword.Name,
                    string.Format("\"{0}\" has no {1} executor", keyword.Name, KindName(kind))));
                return ScriptParseOutcome.Unknown;
            }

            if (check && !ScriptCheckHandlers.ValidateArguments(context, keyword.Check.Value))
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(ScriptDiagnosticKind.InvalidArguments, context, keyword.Name,
                    string.Format("Invalid arguments for \"{0}\"; expected {1}", keyword.Name, keyword.Usage)));
                return ScriptParseOutcome.Skipped;
            }

            int count = check ? context.Segment.CheckList.Count : context.Acts.Count;
            ScriptParseOutcome outcome;

            try
            {
                outcome = keyword.Handler(context);
            }
            catch (Exception ex)
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(ScriptDiagnosticKind.RuntimeException, context, keyword.Name,
                    string.Format("Failed to parse {0} \"{1}\"", KindName(kind), keyword.Name), ex));
                return ScriptParseOutcome.Skipped;
            }

            int registered = check ? context.Segment.CheckList.Count : context.Acts.Count;
            for (int i = count; i < registered; i++)
            {
                if (check) context.Segment.CheckList[i].Source = context.Source;
                else context.Acts[i].Source = context.Source;
            }

            if (outcome != ScriptParseOutcome.Handled || registered <= count)
            {
                MarkInvalid(kind, context, ScriptDiagnostics.Report(ScriptDiagnosticKind.InvalidArguments, context, keyword.Name,
                    string.Format("\"{0}\" did not register a valid {1}; expected {2}", keyword.Name, KindName(kind), keyword.Usage)));
                return ScriptParseOutcome.Skipped;
            }

            return outcome;
        }

        private static void MarkInvalid(ScriptKeywordKind kind, ScriptParseContext context, ScriptDiagnostic diagnostic)
        {
            if (kind == ScriptKeywordKind.Check || kind == ScriptKeywordKind.Action)
                context.Segment?.MarkInvalid(diagnostic, kind == ScriptKeywordKind.Check);
        }

        private static ScriptKeywordEntry Find(ScriptKeywordKind kind, string name)
        {
            switch (kind)
            {
                case ScriptKeywordKind.Check:
                    return ScriptCheckCatalog.Find(name);
                case ScriptKeywordKind.Action:
                    return ScriptActionCatalog.Find(name);
                case ScriptKeywordKind.Variable:
                    return ScriptValueCatalog.FindPlayer(name);
                default:
                    return null;
            }
        }

        private static ScriptDiagnosticKind KindOf(ScriptKeywordKind kind)
        {
            switch (kind)
            {
                case ScriptKeywordKind.Check:
                    return ScriptDiagnosticKind.UnknownCheck;
                case ScriptKeywordKind.Action:
                    return ScriptDiagnosticKind.UnknownAction;
                default:
                    return ScriptDiagnosticKind.UnknownValue;
            }
        }

        private static string KindName(ScriptKeywordKind kind)
        {
            switch (kind)
            {
                case ScriptKeywordKind.Check:
                    return "check";
                case ScriptKeywordKind.Action:
                    return "action";
                default:
                    return "variable";
            }
        }
    }
}
