using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Scripting
{
    /// <summary>
    /// Reads a quest definition file. The rules are the ones the runtime has always used, kept in one place
    /// so the editor reports exactly what the server would do:
    /// <list type="bullet">
    /// <item>a header counts only when the whole line matches a catalog key, case-insensitive, untrimmed;</item>
    /// <item>a body runs to the next line that starts with <c>[</c> or <c>//</c>, or to the end of the file;</item>
    /// <item>blank body lines are skipped but do not end the body;</item>
    /// <item>the same header may appear more than once and the runtime merges the occurrences in file order.</item>
    /// </list>
    /// Problems are returned with the document; nothing is reported to the global diagnostic collector and
    /// nothing here touches Envir or Settings, so the editor can parse unsaved text.
    /// </summary>
    public static class QuestScriptParser
    {
        private static readonly Regex Quoted = new Regex("\"([^\"]*)\"", RegexOptions.Compiled);

        /// <summary>Parses quest file text. <paramref name="fileName"/> only labels diagnostics and row sources.</summary>
        public static QuestScriptDocument Parse(string text, string fileName = null)
        {
            return ParseLines(SplitLines(text), fileName);
        }

        /// <summary>
        /// Parses quest file lines, as a loader that already split the file has them. Line numbers are 1-based
        /// indexes into this list, so they are the file's own line numbers.
        /// </summary>
        public static QuestScriptDocument ParseLines(IReadOnlyList<string> lines, string fileName = null)
        {
            if (lines == null || lines.Count == 0) return QuestScriptDocument.Empty;

            var sections = new List<QuestScriptSection>();
            var diagnostics = new List<ScriptDiagnostic>();
            var seen = new HashSet<QuestScriptSectionKind>();
            var consumed = new bool[lines.Count];
            string contentVersion = null;

            for (int i = 0; i < lines.Count; i++)
            {
                string header = lines[i] ?? string.Empty;

                if (header.Length == 0 || header[0] != '[') continue;

                consumed[i] = true;

                QuestScriptSectionEntry entry = QuestScriptSectionCatalog.Find(header);

                if (entry == null)
                {
                    Report(diagnostics, Source(fileName, i + 1, header, header, ref contentVersion, lines),
                        header, "Unknown quest section header; this line and the lines after it are not read.");
                    continue;
                }

                if (!seen.Add(entry.Kind))
                    Report(diagnostics, Source(fileName, i + 1, header, entry.Key, ref contentVersion, lines),
                        entry.Key, "Duplicate quest section header; the runtime appends every occurrence in file order.");

                var rows = new List<QuestScriptRow>();
                int endIndex = i;

                for (int j = i + 1; j < lines.Count; j++)
                {
                    string line = lines[j] ?? string.Empty;

                    if (line.StartsWith("[") || line.StartsWith("//")) break;

                    consumed[j] = true;
                    endIndex = j;

                    if (line.Length == 0) continue;

                    ScriptSourceInfo source = Source(fileName, j + 1, line, entry.Key, ref contentVersion, lines);
                    QuestScriptRow row = ParseRowCore(entry, line, source, out string problem);

                    if (row == null) continue;

                    if (problem != null)
                        Report(diagnostics, source, entry.Key, problem);
                    else if (entry.ValueKind == QuestScriptValueKind.Number && rows.Count > 0)
                        Report(diagnostics, source, entry.Key,
                            "More than one value in a numeric section; only the last line is kept.");

                    rows.Add(row);
                }

                sections.Add(new QuestScriptSection(entry, i + 1, endIndex + 1, rows));
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (consumed[i]) continue;

                string line = lines[i] ?? string.Empty;

                if (line.Length == 0 || line.StartsWith("//")) continue;

                Report(diagnostics, Source(fileName, i + 1, line, string.Empty, ref contentVersion, lines),
                    string.Empty, "Line outside every quest section; the runtime does not read it.");
            }

            return new QuestScriptDocument(sections, diagnostics);
        }

        /// <summary>
        /// Converts one body line. Pure: the same call the runtime uses, with no item or monster lookup, so
        /// callers that need the raw task fields can get them without an initialized Envir.
        /// </summary>
        public static QuestScriptRow ParseRow(QuestScriptSectionEntry entry, string line, ScriptSourceInfo source = null)
        {
            return ParseRowCore(entry, line, source, out _);
        }

        /// <summary>Converts one body line and reports why the converted value is suspicious, if it is.</summary>
        internal static QuestScriptRow ParseRowCore(QuestScriptSectionEntry entry, string line, ScriptSourceInfo source,
            out string problem)
        {
            problem = null;

            if (entry == null || string.IsNullOrEmpty(line)) return null;

            if (source == null) source = new ScriptSourceInfo(null, 0, line);

            switch (entry.ValueKind)
            {
                case QuestScriptValueKind.Text:
                    return new QuestScriptRow(source, line, 0, 0, 0, string.Empty);

                case QuestScriptValueKind.Item:
                case QuestScriptValueKind.Reward:
                {
                    string[] split = line.Split(' ');
                    ushort count = 1;

                    if (split.Length > 1 && !ushort.TryParse(split[1], out count))
                        problem = "Task count is not a number between 0 and 65535; the runtime uses 0.";

                    return new QuestScriptRow(source, split[0], count, 0, 0,
                        entry.ValueKind == QuestScriptValueKind.Item ? QuotedMessage(line) : string.Empty);
                }

                case QuestScriptValueKind.Monster:
                {
                    string[] split = line.Split(' ');
                    int count = 1;

                    if (split.Length > 1 && !int.TryParse(split[1], out count))
                        problem = "Kill count is not a valid integer; the runtime uses 0.";

                    return new QuestScriptRow(source, split[0], count, 0, 0, QuotedMessage(line));
                }

                case QuestScriptValueKind.Flag:
                {
                    string[] split = line.Split(' ');
                    int flag = -1;

                    if (!int.TryParse(split[0], out flag))
                        problem = "Flag number is not a number; the runtime uses 0, which is a valid flag.";
                    else if (flag < 0 || flag > Globals.FlagIndexCount - 1000)
                        problem = string.Format("Flag number is outside the range 0-{0}; the runtime ignores this line.",
                            Globals.FlagIndexCount - 1000);

                    return new QuestScriptRow(source, split[0], 0, 0, flag, QuotedMessage(line));
                }

                default:
                {
                    uint number;

                    if (!uint.TryParse(line, out number))
                        problem = "Value is not a non-negative number; the runtime sets this reward to 0.";

                    return new QuestScriptRow(source, line, 0, number, 0, string.Empty);
                }
            }
        }

        private static string QuotedMessage(string line)
        {
            Match match = Quoted.Match(line);

            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static ScriptSourceInfo Source(string fileName, int line, string text, string context,
            ref string contentVersion, IReadOnlyList<string> lines)
        {
            // One hash per document: every row of a parse stands for the same text.
            if (contentVersion == null)
            {
                var joined = new StringBuilder();

                for (int i = 0; i < lines.Count; i++) joined.Append(lines[i] ?? string.Empty).Append('\n');

                contentVersion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined.ToString())));
            }

            return new ScriptSourceInfo(fileName, line, text, context, contentVersion);
        }

        /// <summary>
        /// Records a problem with the same source evidence a row carries: file, line, line text, section
        /// context and the version of the whole text the line was read from.
        /// </summary>
        private static void Report(List<ScriptDiagnostic> diagnostics, ScriptSourceInfo source, string keyword, string message)
        {
            DateTime now = DateTime.UtcNow;

            diagnostics.Add(new ScriptDiagnostic
            {
                Kind = ScriptDiagnosticKind.MalformedSection,
                Category = ScriptDiagnosticCategory.ScriptError,
                Phase = ScriptDiagnosticPhase.Load,
                Effect = ScriptDiagnosticEffect.Notice,
                FileName = source.FileName,
                Keyword = keyword ?? string.Empty,
                Text = source.Text,
                Message = message,
                Line = source.Line,
                SourceContext = source.Context,
                SourceVersion = source.ContentVersion,
                Runtime = false,
                // Stable within a parse: the position plus the order the problem was found in.
                Id = string.Concat("quest:", source.FileName, ":", source.Line.ToString(), ":",
                    diagnostics.Count.ToString()),
                FirstSeenUtc = now,
                LastSeenUtc = now,
            });
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

            // A byte order mark left in a decoded buffer is not text the author wrote.
            if (text[0] == '\uFEFF') text = text.Substring(1);

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            // File.ReadAllLines drops the empty element a trailing newline produces; both entry points agree.
            if (lines.Length > 0 && lines[lines.Length - 1].Length == 0)
                Array.Resize(ref lines, lines.Length - 1);

            return lines;
        }
    }
}
