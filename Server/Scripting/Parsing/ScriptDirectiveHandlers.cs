using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;

namespace Server.Scripting
{
    /// <summary>
    /// The bodies of the loader directives. Each one receives the whole line list and changes it in
    /// place; the catalog decides which run and in what order.
    /// </summary>
    internal static class ScriptDirectiveHandlers
    {
        /// <summary>
        /// How many files one script may pull in through other files. A file that names itself, directly
        /// or through a chain, would otherwise grow the line list for as long as there is memory.
        /// </summary>
        private const int MaxDepth = 8;

        private sealed class DependencyException : Exception
        {
            internal readonly ScriptDiagnostic Diagnostic;
            internal DependencyException(ScriptDiagnostic diagnostic) { Diagnostic = diagnostic; }
        }

        public static void Insert(List<string> lines)
        {
            ApplyText(lines, ScriptDirectiveCatalog.Insert);
        }

        public static void Include(List<string> lines)
        {
            ApplyText(lines, ScriptDirectiveCatalog.Include);
        }

        private static void ApplyText(List<string> lines, ScriptDirectiveEntry entry)
        {
            var source = ScriptSourceLine.Bind(lines, ScriptParseScope.FileName);
            entry.ApplySource(source);
            lines.Clear();
            lines.AddRange(source.Select(line => line.Text));
        }

        public static void InsertSource(List<ScriptSourceLine> lines)
        {
            Expand(lines, ScriptDirectiveCatalog.Insert, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        }

        public static void IncludeSource(List<ScriptSourceLine> lines)
        {
            Expand(lines, ScriptDirectiveCatalog.Include, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        }

        /// <summary>
        /// Replaces every directive of one kind with the lines it stands for, at the position the
        /// directive held. The replacement is expanded again with every directive kind, so a pulled-in
        /// file may use directives of its own; a file already being read is reported, not followed.
        /// </summary>
        private static void Expand(List<ScriptSourceLine> lines, ScriptDirectiveEntry entry, HashSet<string> open, int depth)
        {
            var result = new List<ScriptSourceLine>(lines.Count);

            foreach (ScriptSourceLine line in lines)
            {
                if (line.Error != null || !ScriptDirectiveCatalog.Is(line.Text, entry))
                {
                    result.Add(line);
                    continue;
                }

                try
                {
                    if (depth >= MaxDepth)
                        Report(ScriptDiagnosticKind.MalformedDirective, line,
                            "Directive nesting deeper than " + MaxDepth + " levels");

                    string identity;
                    List<ScriptSourceLine> content = ReadContent(line, entry, out identity);
                    if (content == null) continue;

                    // Charge every dependency use, including cache hits, before recursively expanding it.
                    ScriptValidationScope.Current?.ConsumeLines(content.Count);

                    if (!open.Add(identity))
                        Report(ScriptDiagnosticKind.MalformedDirective, line,
                            ScriptDirectiveCatalog.Tag(entry) + " reads content that is already being read: " + identity);
                    try { ExpandAll(content, open, depth + 1); }
                    finally { open.Remove(identity); }
                    result.AddRange(content);
                }
                catch (DependencyException exception)
                {
                    // Keep a source-bound failure marker so only its containing segment is disabled.
                    result.Add(new ScriptSourceLine(line.Text, line.Number, line.FileName, line.Source, exception.Diagnostic));
                }
                catch (Exception exception) when (ScriptValidationScope.IsActive &&
                    (exception is ArgumentException || exception is NotSupportedException ||
                     exception is IOException || exception is UnauthorizedAccessException ||
                     exception is System.Security.SecurityException))
                {
                    // An editor dependency failure must not abort unrelated pages or lose its location.
                    ScriptDiagnosticKind kind = exception is ArgumentException || exception is NotSupportedException ||
                        exception is PathTooLongException
                        ? ScriptDiagnosticKind.MalformedDirective : ScriptDiagnosticKind.LoadFailed;
                    ScriptDiagnostic diagnostic = ReportDiagnostic(kind, line, exception.Message, exception);
                    result.Add(new ScriptSourceLine(line.Text, line.Number, line.FileName, line.Source, diagnostic));
                }
            }

            lines.Clear();
            lines.AddRange(result);
        }

        private static void ExpandAll(List<ScriptSourceLine> lines, HashSet<string> open, int depth)
        {
            foreach (ScriptDirectiveEntry entry in ScriptDirectiveCatalog.Ordered)
                Expand(lines, entry, open, depth);
        }

        /// <summary>
        /// The lines a directive stands for, or null when it cannot be used and has already been
        /// reported. INSERT takes a whole file; INCLUDE takes one page written between braces.
        /// </summary>
        private static List<ScriptSourceLine> ReadContent(ScriptSourceLine line, ScriptDirectiveEntry entry, out string identity)
        {
            string[] parts = line.Text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            identity = null;

            if (entry == ScriptDirectiveCatalog.Insert)
            {
                if (parts.Length < 2)
                {
                    Report(ScriptDiagnosticKind.MalformedDirective, line, "Malformed INSERT directive");
                    return null;
                }

                string insertPath = Resolve(parts[1]);
                identity = "INSERT|" + insertPath;

                if (!ScriptValidationScope.Exists(insertPath))
                {
                    if (!ScriptValidationScope.IsActive) MessageQueue.Instance.Enqueue(GameLanguage.Get("INSERT Script Not Found: {0}", insertPath));
                    Report(ScriptDiagnosticKind.MissingScript, line, "INSERT file not found: " + insertPath);

                    return null;
                }

                return ReadScriptFile(insertPath, line);
            }

            if (parts.Length < 3)
            {
                Report(ScriptDiagnosticKind.MalformedDirective, line, "Malformed INCLUDE directive");
                return null;
            }

            string path = Resolve(parts[1]);

            string page = "[" + parts[2] + "]";
            identity = "INCLUDE|" + path + "|" + page;

            if (!ScriptValidationScope.Exists(path))
            {
                if (!ScriptValidationScope.IsActive) MessageQueue.Instance.Enqueue(GameLanguage.Get("INCLUDE Script Not Found: {0}", path));
                Report(ScriptDiagnosticKind.MissingScript, line, "INCLUDE file not found: " + path);

                return null;
            }

            IList<ScriptSourceLine> extLines = ReadScriptFile(path, line);

            for (int i = 0; i < extLines.Count; i++)
            {
                if (!string.Equals(extLines[i].Text, page, StringComparison.OrdinalIgnoreCase)) continue;

                var body = new List<ScriptSourceLine>();

                bool start = false, finish = false;
                int braces = 0;

                for (int j = i + 1; j < extLines.Count; j++)
                {
                    // The opener belongs to the requested page, but its body may intentionally
                    // contain further pages or data sections. Match braces instead of truncating it.
                    if (!start && ScriptPageKeyCatalog.TryParseHeader(extLines[j].Text, out _)) break;

                    if (extLines[j].Text.Trim() == "{")
                    {
                        braces++;
                        if (!start)
                        {
                            start = true;
                            continue;
                        }
                    }

                    if (start && extLines[j].Text.Trim() == "}")
                    {
                        if (--braces == 0)
                        {
                            finish = true;
                            break;
                        }
                    }

                    if (start) body.Add(extLines[j]);
                }

                if (start && finish) return body;

                break;
            }

            if (!ScriptValidationScope.IsActive) MessageQueue.Instance.Enqueue(GameLanguage.Get("INCLUDE Page Not Found: {0} in {1}", page, path));
            Report(ScriptDiagnosticKind.MissingPage, line, "INCLUDE page " + page + " not found in " + path);

            return null;
        }

        /// <summary>
        /// Reads a file a directive points at, with the same encoding rule as the script itself: GBK is
        /// accepted but reported, so a converted script does not silently keep a legacy include.
        /// </summary>
        private static List<ScriptSourceLine> ReadScriptFile(string path, ScriptSourceLine line)
        {
            bool legacy;

            IList<string> lines = ScriptValidationScope.Current != null
                ? ScriptValidationScope.Current.ReadFile(path, out legacy)
                : ScriptTextFile.ReadAllLines(path, out legacy);

            if (legacy)
                Report(ScriptDiagnosticKind.LegacyEncoding, line, "file is not UTF-8, read as GBK: " + path);

            return ScriptSourceLine.Bind(lines, path);
        }

        private static string Resolve(string argument)
        {
            return Path.GetFullPath(Path.Combine(ScriptValidationScope.Current?.EnvirPath ?? Settings.EnvirPath, Unquote(argument)));
        }

        private static void Report(ScriptDiagnosticKind kind, ScriptSourceLine line, string message)
        {
            ScriptDiagnostic diagnostic = ReportDiagnostic(kind, line, message);
            if (kind != ScriptDiagnosticKind.LegacyEncoding)
                throw new DependencyException(diagnostic);
        }

        private static ScriptDiagnostic ReportDiagnostic(ScriptDiagnosticKind kind, ScriptSourceLine line, string message,
            Exception exception = null)
        {
            return ScriptDiagnostics.Report(kind, new ScriptParseContext
            {
                Line = line.Text,
                LineNumber = line.Number,
                FileName = line.FileName ?? ScriptParseScope.FileName,
                Source = line.Source,
            }, Tag(line.Text), message, exception);
        }

        private static string Tag(string line)
        {
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 0 ? string.Empty : parts[0].ToUpper();
        }

        /// <summary>
        /// Strips one level of the two wrappers scripts use: "path" and [path]. A bare path is left
        /// alone; the old loader dropped its first and last character.
        /// </summary>
        private static string Unquote(string argument)
        {
            if (argument == null) return string.Empty;

            if (argument.Length >= 2)
            {
                char first = argument[0], last = argument[argument.Length - 1];

                if ((first == '\"' && last == '\"') || (first == '[' && last == ']'))
                    return argument.Substring(1, argument.Length - 2);
            }

            return argument;
        }
    }
}
