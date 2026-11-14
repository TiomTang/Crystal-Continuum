using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;

namespace Server.MirObjects
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

        public static void Insert(List<string> lines)
        {
            Expand(lines, ScriptDirectiveCatalog.Insert, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        }

        public static void Include(List<string> lines)
        {
            Expand(lines, ScriptDirectiveCatalog.Include, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        }

        /// <summary>
        /// Replaces every directive of one kind with the lines it stands for, at the position the
        /// directive held. The replacement is expanded again with every directive kind, so a pulled-in
        /// file may use directives of its own; a file already being read is reported, not followed.
        /// </summary>
        private static void Expand(List<string> lines, ScriptDirectiveEntry entry, HashSet<string> open, int depth)
        {
            var result = new List<string>(lines.Count);

            foreach (string line in lines)
            {
                if (!ScriptDirectiveCatalog.Is(line, entry))
                {
                    result.Add(line);
                    continue;
                }

                if (depth >= MaxDepth)
                {
                    Report(ScriptDiagnosticKind.MalformedDirective, line,
                        "Directive nesting deeper than " + MaxDepth + " levels");

                    continue;
                }

                string path;
                List<string> content = ReadContent(line, entry, out path);

                if (content == null) continue;

                if (!open.Add(path))
                {
                    Report(ScriptDiagnosticKind.MalformedDirective, line,
                        ScriptDirectiveCatalog.Tag(entry) + " reads a file that is already being read: " + path);

                    continue;
                }

                ExpandAll(content, open, depth + 1);
                open.Remove(path);

                result.AddRange(content);
            }

            lines.Clear();
            lines.AddRange(result);
        }

        private static void ExpandAll(List<string> lines, HashSet<string> open, int depth)
        {
            foreach (ScriptDirectiveEntry entry in ScriptDirectiveCatalog.Ordered)
                Expand(lines, entry, open, depth);
        }

        /// <summary>
        /// The lines a directive stands for, or null when it cannot be used and has already been
        /// reported. INSERT takes a whole file; INCLUDE takes one page written between braces.
        /// </summary>
        private static List<string> ReadContent(string line, ScriptDirectiveEntry entry, out string path)
        {
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            path = null;

            if (entry == ScriptDirectiveCatalog.Insert)
            {
                if (parts.Length < 2)
                {
                    Report(ScriptDiagnosticKind.MalformedDirective, line, "Malformed INSERT directive");
                    return null;
                }

                path = Resolve(parts[1]);

                if (!File.Exists(path))
                {
                    MessageQueue.Instance.Enqueue(GameLanguage.Get("INSERT Script Not Found: {0}", path));
                    Report(ScriptDiagnosticKind.MissingScript, line, "INSERT file not found: " + path);

                    return null;
                }

                return File.ReadAllLines(path).ToList();
            }

            if (parts.Length < 3)
            {
                Report(ScriptDiagnosticKind.MalformedDirective, line, "Malformed INCLUDE directive");
                return null;
            }

            path = Resolve(parts[1]);

            string page = ("[" + parts[2] + "]").ToUpper();

            if (!File.Exists(path))
            {
                MessageQueue.Instance.Enqueue(GameLanguage.Get("INCLUDE Script Not Found: {0}", path));
                Report(ScriptDiagnosticKind.MissingScript, line, "INCLUDE file not found: " + path);

                return null;
            }

            IList<string> extLines = File.ReadAllLines(path);

            for (int i = 0; i < extLines.Count; i++)
            {
                if (!extLines[i].ToUpper().StartsWith(page)) continue;

                var body = new List<string>();

                bool start = false, finish = false;

                for (int j = i + 1; j < extLines.Count; j++)
                {
                    if (extLines[j].Trim() == "{")
                    {
                        start = true;
                        continue;
                    }

                    if (extLines[j].Trim() == "}")
                    {
                        finish = true;
                        break;
                    }

                    if (start) body.Add(extLines[j]);
                }

                if (start && finish) return body;

                break;
            }

            MessageQueue.Instance.Enqueue(GameLanguage.Get("INCLUDE Page Not Found: {0} in {1}", page, path));
            Report(ScriptDiagnosticKind.MissingPage, line, "INCLUDE page " + page + " not found in " + path);

            return null;
        }

        private static string Resolve(string argument)
        {
            return Path.GetFullPath(Path.Combine(Settings.EnvirPath, Unquote(argument)));
        }

        private static void Report(ScriptDiagnosticKind kind, string line, string message)
        {
            ScriptDiagnostics.Report(kind, new ScriptParseContext
            {
                Line = line,
                FileName = ScriptParseScope.FileName,
            }, Tag(line), message);
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
