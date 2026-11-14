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
        public static void Insert(List<string> lines)
        {
            List<string> newLines = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!ScriptDirectiveCatalog.Is(lines[i], ScriptDirectiveCatalog.Insert)) continue;

                string[] split = lines[i].Split(' ');

                if (split.Length < 2)
                {
                    ReportMalformed(lines[i], ScriptDirectiveCatalog.Insert);
                    continue;
                }

                string path = Path.Combine(Settings.EnvirPath, Unquote(split[1]));

                if (!File.Exists(path))
                    MessageQueue.Instance.Enqueue(GameLanguage.Get("INSERT Script Not Found: {0}", path));
                else
                    newLines = File.ReadAllLines(path).ToList();

                lines.AddRange(newLines);
            }

            lines.RemoveAll(str => ScriptDirectiveCatalog.Is(str, ScriptDirectiveCatalog.Insert));
        }

        public static void Include(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!ScriptDirectiveCatalog.Is(lines[i], ScriptDirectiveCatalog.Include)) continue;

                string[] split = lines[i].Split(' ');

                if (split.Length < 3)
                {
                    ReportMalformed(lines[i], ScriptDirectiveCatalog.Include);
                    continue;
                }

                string path = Path.Combine(Settings.EnvirPath, Unquote(split[1]));
                string page = ("[" + split[2] + "]").ToUpper();

                bool start = false, finish = false;

                var parsedLines = new List<string>();

                if (!File.Exists(path))
                {
                    MessageQueue.Instance.Enqueue(GameLanguage.Get("INCLUDE Script Not Found: {0}", path));
                    return;
                }

                IList<string> extLines = File.ReadAllLines(path);

                for (int j = 0; j < extLines.Count; j++)
                {
                    if (!extLines[j].ToUpper().StartsWith(page)) continue;

                    for (int x = j + 1; x < extLines.Count; x++)
                    {
                        if (extLines[x].Trim() == ("{"))
                        {
                            start = true;
                            continue;
                        }

                        if (extLines[x].Trim() == ("}"))
                        {
                            finish = true;
                            break;
                        }

                        parsedLines.Add(extLines[x]);
                    }
                }

                if (start && finish)
                {
                    lines.InsertRange(i + 1, parsedLines);
                    parsedLines.Clear();
                }
            }

            lines.RemoveAll(str => ScriptDirectiveCatalog.Is(str, ScriptDirectiveCatalog.Include));
        }

        private static void ReportMalformed(string line, ScriptDirectiveEntry entry)
        {
            string tag = ScriptDirectiveCatalog.Tag(entry);

            ScriptDiagnostics.Report(ScriptDiagnosticKind.MalformedDirective, new ScriptParseContext
            {
                Line = line,
                FileName = ScriptParseScope.FileName,
            }, tag, "Malformed " + tag + " directive");
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

                if ((first == '"' && last == '"') || (first == '[' && last == ']'))
                    return argument.Substring(1, argument.Length - 2);
            }

            return argument;
        }
    }
}
