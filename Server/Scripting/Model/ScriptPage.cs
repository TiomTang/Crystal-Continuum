using System.Collections.Generic;

namespace Server.MirObjects
{
    public class ScriptPage
    {
        public string Key;
        public string FileName;
        public ScriptSourceInfo Source;
        public List<ScriptSegment> SegmentList = new List<ScriptSegment>();
        public List<string> Args = new List<string>();
        public List<string> Buttons = new List<string>();

        public List<int> ScriptCalls = new List<int>();


        public ScriptPage(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Strips the arguments of a clickable page key, leaving the empty "()" the header carries.
        /// Returns the key unchanged when it is not a clickable page or carries no arguments.
        /// </summary>
        public static string NormalizeSectionName(string key, out string[] args)
        {
            args = null;

            ScriptPageKeyEntry entry = ScriptPageKeyCatalog.Find(key);

            // Automatic markers and input pages keep their arguments inside the key; only clickable
            // pages split "…(a,b)" into Args and keep an empty "()" so the header still matches.
            if (entry == null || !entry.UsesArguments) return key;

            string normalized = ScriptPageKeyCatalog.SplitArguments(key, out args);

            if (args.Length == 0) return key;

            return normalized;
        }

        public string ArgumentParse(string key)
        {
            string[] args;
            string normalized = NormalizeSectionName(key, out args);

            if (args == null || args.Length == 0) return key;

            Args = new List<string>(args);

            return normalized;
        }
    }
}