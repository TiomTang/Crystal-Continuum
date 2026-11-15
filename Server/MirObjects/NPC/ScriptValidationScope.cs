using System;
using System.Collections.Generic;
using System.IO;

namespace Server.MirObjects
{
    /// <summary>A thread-local, read-only parsing session. It never publishes runtime diagnostics.</summary>
    internal sealed class ScriptValidationScope : IDisposable
    {
        [ThreadStatic] private static ScriptValidationScope _current;
        internal static ScriptValidationScope Current => _current;
        internal static bool IsActive => _current != null;

        private readonly ScriptValidationScope _previous;
        private readonly string _previousFile;
        private readonly Dictionary<string, (string[] Lines, bool Legacy)> _files;
        private readonly HashSet<string> _calls;
        private readonly int _depth;
        private int _expandedLines;
        private int _pages;
        private long _pageScanLines;
        private long _pageKeyCharacters;
        private bool _pageBudgetReported;
        internal readonly List<ScriptDiagnostic> Diagnostics = new List<ScriptDiagnostic>();
        internal readonly List<ScriptValidationResult> CalledScripts = new List<ScriptValidationResult>();
        internal readonly string EnvirPath;
        internal readonly string BatchId = Guid.NewGuid().ToString("N");

        internal ScriptValidationScope(string envirPath, string fileName, string[] buffer)
        {
            EnvirPath = Path.GetFullPath(envirPath);
            _previous = _current;
            _previousFile = ScriptParseScope.FileName;
            _depth = (_previous?._depth ?? -1) + 1;
            _files = _previous?._files ?? new Dictionary<string, (string[], bool)>(StringComparer.OrdinalIgnoreCase);
            _calls = _previous?._calls ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _files[fileName] = (buffer, _files.TryGetValue(fileName, out var cached) && cached.Legacy);
            _calls.Add(fileName);
            _current = this;
            ScriptParseScope.Set(fileName);
        }

        internal string Resolve(string directory, string name) => Path.Combine(EnvirPath, directory, name);
        internal string NameListPath(string name) => ScriptPaths.NameList(Resolve("NameLists", string.Empty), name);

        internal string[] ReadFile(string path, out bool legacy)
        {
            path = Path.GetFullPath(path);
            if (_files.TryGetValue(path, out var cached))
            {
                legacy = cached.Legacy;
                return cached.Lines;
            }
            if (new FileInfo(path).Length > 8 * 1024 * 1024)
                throw new IOException("Validation dependency exceeds the 8 MiB file limit: " + path);
            string[] lines = ScriptTextFile.ReadAllLines(path, out legacy);
            _files.Add(path, (lines, legacy));
            return lines;
        }

        /// <summary>
        /// File existence as this parse sees it: an unsaved buffer the session already holds exists too.
        /// Callers outside a session keep the plain file-system answer.
        /// </summary>
        internal static bool Exists(string path)
        {
            if (File.Exists(path)) return true;
            return _current != null && _current._files.ContainsKey(Path.GetFullPath(path));
        }

        internal void ConsumeLines(int count)
        {
            if (_previous != null) { _previous.ConsumeLines(count); return; }
            _expandedLines += count;
            if (_expandedLines > 100000)
                throw new IOException("Validation dependency expansion exceeds 100000 lines.");
        }

        internal bool TryConsumePage(int lineCount, string key, ScriptSourceInfo source)
        {
            var session = this;
            while (session._previous != null) session = session._previous;
            if (++session._pages <= 1024 && (session._pageScanLines += lineCount) <= 2000000 &&
                (session._pageKeyCharacters += key.Length) <= 1000000) return true;
            if (!session._pageBudgetReported)
            {
                session._pageBudgetReported = true;
                Collect(new ScriptDiagnostic
                {
                    Kind = ScriptDiagnosticKind.ChainLimit, Category = ScriptDiagnosticCategory.EngineFault,
                    Phase = ScriptDiagnosticPhase.Load, Effect = ScriptDiagnosticEffect.Notice, Suppressed = true,
                    FileName = source?.FileName ?? ScriptParseScope.FileName, Line = source?.Line ?? 0,
                    Text = source?.Text, SourceVersion = source?.ContentVersion, SourceContext = source?.Context,
                    PageKey = key, Message = "Validation page expansion budget reached; the result is incomplete.",
                });
            }
            return false;
        }

        internal void ValidateCall(string path, ScriptParseContext context)
        {
            path = Path.GetFullPath(path);
            // Live CALL registration reuses an existing script, including a recursive one.
            if (_calls.Contains(path)) return;
            if (_depth >= 8)
            {
                ScriptDiagnostics.Report(ScriptDiagnosticKind.MalformedDirective, context, "CALL",
                    "Validation CALL nesting exceeds 8 levels.");
                return;
            }
            try
            {
                string[] lines = ReadFile(path, out bool legacy);
                if (legacy) ScriptDiagnostics.Report(ScriptDiagnosticKind.LegacyEncoding, context, "CALL",
                    "file is not UTF-8, read as GBK: " + path);
                CalledScripts.Add(NPCScript.Validate(string.Join(Environment.NewLine, lines), path, EnvirPath, NPCScriptType.Called));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                ScriptDiagnostics.Report(ScriptDiagnosticKind.LoadFailed, context, "CALL", exception.Message, exception);
            }
        }

        internal ScriptDiagnostic Collect(ScriptDiagnostic diagnostic)
        {
            diagnostic.Id = Guid.NewGuid().ToString("N");
            diagnostic.BatchId = BatchId;
            diagnostic.FirstSeenUtc = diagnostic.LastSeenUtc = DateTime.UtcNow;
            // Unlike runtime aggregation, editing needs every original source location.
            if (Diagnostics.Count < ScriptDiagnostics.MaxEntries) Diagnostics.Add(diagnostic.Copy());
            else if (Diagnostics.Count == ScriptDiagnostics.MaxEntries)
                Diagnostics.Add(new ScriptDiagnostic
                {
                    Id = Guid.NewGuid().ToString("N"), BatchId = BatchId,
                    Kind = ScriptDiagnosticKind.DiagnosticsSuppressed,
                    Category = ScriptDiagnosticCategory.EngineFault, Effect = ScriptDiagnosticEffect.Notice,
                    Message = "Validation diagnostic capacity reached; the result is incomplete.", Suppressed = true,
                });
            return diagnostic;
        }

        public void Dispose()
        {
            _current = _previous;
            ScriptParseScope.Set(_previousFile);
        }
    }
}
