using System.Collections.Generic;

namespace Server.Scripting
{
    /// <summary>
    /// A line of a script paired with the number it had in the file. Keywords are collected while a
    /// segment is split and parsed afterwards, so the number has to travel with the text.
    /// </summary>
    public readonly struct ScriptSourceLine
    {
        public readonly string Text;
        public readonly int Number;
        public readonly string FileName;
        public readonly ScriptSourceInfo Source;
        public readonly ScriptDiagnostic Error;

        public ScriptSourceLine(string text, int number, string fileName = null, ScriptSourceInfo source = null, ScriptDiagnostic error = null)
        {
            Text = text;
            Number = number;
            FileName = fileName;
            Source = source ?? new ScriptSourceInfo(fileName, number, text);
            Error = error;
        }

        /// <summary>Bind original locations before directives change the number or order of lines.</summary>
        public static List<ScriptSourceLine> Bind(IList<string> lines, string fileName)
        {
            var source = new List<ScriptSourceLine>(lines.Count);
            string version = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines))));

            for (int i = 0; i < lines.Count; i++)
            {
                var context = new List<string>();
                for (int j = Math.Max(0, i - 1); j <= Math.Min(lines.Count - 1, i + 1); j++)
                    context.Add((j + 1) + ": " + lines[j]);
                source.Add(new ScriptSourceLine(lines[i], i + 1, fileName,
                    new ScriptSourceInfo(fileName, i + 1, lines[i], string.Join("\n", context), version)));
            }

            return source;
        }
    }

    /// <summary>Everything a keyword handler needs about the line it is parsing.</summary>
    public sealed class ScriptParseContext
    {
        /// <summary>Line split on spaces after %ARG substitution.</summary>
        public string[] Parts;

        /// <summary>Raw line, kept for diagnostics.</summary>
        public string Line;

        /// <summary>1-based line number in the script file, or 0 when it is not known.</summary>
        public int LineNumber;

        /// <summary>Script file being parsed, for diagnostics.</summary>
        public string FileName;

        /// <summary>Page the line belongs to, for diagnostics.</summary>
        public string PageKey;
        public ScriptSourceInfo Source;

        /// <summary>Segment receiving the parsed checks and actions.</summary>
        public ScriptSegment Segment;

        /// <summary>Target list for action keywords.</summary>
        public List<ScriptActions> Acts;
    }

    /// <summary>
    /// Remembers the script file currently being parsed so diagnostics can name it. Parsing runs on
    /// the load thread, so a thread-static value is enough.
    /// </summary>
    public static class ScriptParseScope
    {
        [System.ThreadStatic]
        private static string _fileName;

        [System.ThreadStatic]
        private static string _root;

        public static string FileName
        {
            get { return _fileName; }
        }

        /// <summary>Directory of the file being parsed, so CALL resolves inside the same one.</summary>
        public static string Root
        {
            get { return _root; }
        }

        public static void Set(string fileName, string root = null)
        {
            _fileName = fileName;
            _root = root;
        }
    }
}
