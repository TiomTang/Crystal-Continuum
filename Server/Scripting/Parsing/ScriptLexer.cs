using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Scripting
{
    /// <summary>
    /// The small grammar shared by the loader, the executor and the editor: what a comment is, how a
    /// statement is split into words, and where a &lt;$VALUE&gt; or a %ARG(n) starts and ends. The
    /// expressions are static so the hot paths stop allocating a Regex per call.
    /// </summary>
    public static class ScriptLexer
    {
        private static readonly string[] Empty = new string[0];

        /// <summary>A &lt;$VALUE&gt; placeholder. Non-greedy: two placeholders in one token both match.</summary>
        public static readonly Regex Placeholder = new Regex(@"<\$([^<>]*)>", RegexOptions.Compiled);

        /// <summary>A %ARG(0) reference; case-insensitive because script authors write both.</summary>
        public static readonly Regex Argument = new Regex(@"%ARG\((\d+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A value argument such as %A1 inside parentheses, e.g. CHECKLEVEL(%A1).</summary>
        public static readonly Regex StatementValue = new Regex(@"(.*?)\(([A-Z][0-9])\)", RegexOptions.Compiled);

        /// <summary>One argument inside parentheses.</summary>
        public static readonly Regex OneValue = new Regex(@"(.*?)\(((.*?))\)", RegexOptions.Compiled);

        /// <summary>Two arguments inside parentheses.</summary>
        public static readonly Regex TwoValue = new Regex(@"(.*?)\(((.*?),(.*?))\)", RegexOptions.Compiled);

        /// <summary>A page button written as &lt;Text/@Page&gt;.</summary>
        public static readonly Regex Button = new Regex(@"<.*?/(\@.*?)>", RegexOptions.Compiled);

        /// <summary>
        /// A variable name: "A1" when stored, "%A1" when read from a placeholder. The prefix is optional
        /// because the store uses the bare name and the lookup passes the '%' along.
        /// </summary>
        public static readonly Regex VariableKey = new Regex(@"%?[A-Za-z][0-9]", RegexOptions.Compiled);

        /// <summary>
        /// True when the line is a comment. Both the loader and the editor use this rule, so a line the
        /// editor greys out is not parsed as a statement by the server.
        /// </summary>
        public static bool IsComment(string line)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string text = line.TrimStart();

            return text.Length > 0 && (text[0] == ';' || (text.Length > 1 && text[0] == '/' && text[1] == '/'));
        }

        /// <summary>
        /// Splits a statement on spaces, keeping a &lt;...&gt; group together so a value that takes an
        /// argument with a space, such as &lt;$ITEMNAME(a b)&gt;, stays one word. Empty words are dropped.
        /// </summary>
        public static string[] Tokenize(string line)
        {
            if (string.IsNullOrEmpty(line)) return Empty;

            var words = new List<string>();
            var word = new StringBuilder();
            bool inValue = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                // Only "<$" opens a value. A comparison such as "LEVEL < 22" starts with '<' too,
                // and must keep splitting on spaces or the value becomes "< 22".
                if (c == '<' && i + 1 < line.Length && line[i + 1] == '$') inValue = true;
                else if (c == '>') inValue = false;

                if (c == ' ' && !inValue)
                {
                    if (word.Length > 0)
                    {
                        words.Add(word.ToString());
                        word.Clear();
                    }

                    continue;
                }

                word.Append(c);
            }

            if (word.Length > 0) words.Add(word.ToString());

            return words.ToArray();
        }

        /// <summary>True when the key has the shape of an NPC variable name, such as A1 or %A1.</summary>
        public static bool IsVariableKey(string key)
        {
            return !string.IsNullOrEmpty(key) && VariableKey.IsMatch(key);
        }
    }
}
