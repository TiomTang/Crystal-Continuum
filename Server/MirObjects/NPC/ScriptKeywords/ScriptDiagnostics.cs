using System;
using System.Collections.Generic;
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
        LoadFailed,
    }

    /// <summary>One script problem, deduplicated by file, page, keyword and line text.</summary>
    public sealed class ScriptDiagnostic
    {
        public ScriptDiagnosticKind Kind;
        public string FileName;
        public string PageKey;
        public string Keyword;
        public string Text;
        public string Message;

        /// <summary>1-based line in the script file, or 0 when the problem was found at run time.</summary>
        public int Line;

        /// <summary>True when a player reached the line, false when the loader found it.</summary>
        public bool Runtime;

        /// <summary>How often the same problem was reported.</summary>
        public int Count = 1;

        public override string ToString()
        {
            string where = Line > 0 ? FileName + ":" + Line : FileName;

            return string.Format("[{0}] {1} {2}: {3} ({4}){5}{6}", Kind, where, PageKey, Message, Text,
                Runtime ? " [runtime]" : string.Empty, Count > 1 ? " x" + Count : string.Empty);
        }
    }

    /// <summary>
    /// Collects NPC script problems while scripts load, so an unknown keyword is never dropped
    /// silently: the server logs it and tools can read the same collection afterwards.
    /// </summary>
    public static class ScriptDiagnostics
    {
        /// <summary>Upper bound, so a broken script cannot grow the collection without limit.</summary>
        public const int MaxEntries = 5000;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, ScriptDiagnostic> Entries =
            new Dictionary<string, ScriptDiagnostic>(StringComparer.Ordinal);

        private static int _suppressed;

        /// <summary>Optional sink for warnings; the server points this at its log.</summary>
        public static Action<string> Sink;

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
            lock (Sync)
            {
                Entries.Clear();
                _suppressed = 0;
            }
        }

        public static IReadOnlyList<ScriptDiagnostic> Snapshot()
        {
            List<ScriptDiagnostic> list;

            lock (Sync)
            {
                list = new List<ScriptDiagnostic>(Entries.Values);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.FileName + a.PageKey + a.Keyword, b.FileName + b.PageKey + b.Keyword));

            return list;
        }

        /// <summary>
        /// Records a problem found while a file loads. Entries are keyed without the line number, so a
        /// mistake repeated on ten lines stays one entry and keeps the first line it was seen on.
        /// </summary>
        public static void Report(ScriptDiagnosticKind kind, ScriptParseContext context, string keyword, string message)
        {
            if (context == null) return;

            string text = (context.Line ?? string.Empty).Trim();
            string file = context.FileName ?? string.Empty;
            string page = context.PageKey ?? string.Empty;
            string key = kind + "|" + file + "|" + page + "|" + keyword + "|" + text;

            bool added = false;

            lock (Sync)
            {
                ScriptDiagnostic existing;

                if (Entries.TryGetValue(key, out existing))
                {
                    existing.Count++;
                }
                else if (Entries.Count >= MaxEntries)
                {
                    _suppressed++;
                }
                else
                {
                    Entries.Add(key, new ScriptDiagnostic
                    {
                        Kind = kind,
                        FileName = file,
                        PageKey = page,
                        Keyword = keyword,
                        Text = text,
                        Message = message,
                        Line = context.LineNumber,
                    });

                    added = true;
                }
            }

            if (!added) return;

            Action<string> sink = Sink;

            if (sink != null)
            {
                string where = context.LineNumber > 0 ? file + ":" + context.LineNumber : file;

                sink(string.Format("[Script] {0} {1}: {2} | {3}", where, page, message, text));
            }
        }

        /// <summary>
        /// Records a problem found while a script runs, where the file and line are no longer known.
        /// Entries are keyed by page and keyword, so a value used on every visit is reported once.
        /// </summary>
        public static void ReportRuntime(ScriptDiagnosticKind kind, string pageKey, string keyword, string message)
        {
            bool added = false;

            string key = kind + "||" + pageKey + "|" + keyword + "|";

            lock (Sync)
            {
                ScriptDiagnostic existing;

                if (Entries.TryGetValue(key, out existing))
                {
                    existing.Count++;
                }
                else if (Entries.Count >= MaxEntries)
                {
                    _suppressed++;
                }
                else
                {
                    Entries.Add(key, new ScriptDiagnostic
                    {
                        Kind = kind,
                        FileName = string.Empty,
                        PageKey = pageKey ?? string.Empty,
                        Keyword = keyword ?? string.Empty,
                        Text = string.Empty,
                        Message = message,
                        Runtime = true,
                    });

                    added = true;
                }
            }

            if (!added) return;

            Action<string> sink = Sink;

            if (sink != null)
            {
                sink(string.Format("[Script] {0}: {1}", pageKey, message));
            }
        }

        /// <summary>One-line summary for load-time logging.</summary>
        public static string Describe()
        {
            lock (Sync)
            {
                if (Entries.Count == 0) return string.Empty;

                HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ScriptDiagnostic entry in Entries.Values)
                    files.Add(entry.FileName);

                var summary = new StringBuilder();
                summary.Append(Entries.Count).Append(" script problem(s) in ").Append(files.Count).Append(" file(s)");

                if (_suppressed > 0)
                    summary.Append(", ").Append(_suppressed).Append(" more suppressed");

                return summary.ToString();
            }
        }
    }
}
