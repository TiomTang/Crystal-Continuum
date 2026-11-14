using System.Collections.Generic;

namespace Server.MirObjects
{
    public class NPCPage
    {
        public string Key;
        public List<NPCSegment> SegmentList = new List<NPCSegment>();
        public List<string> Args = new List<string>();
        public List<string> Buttons = new List<string>();

        public List<int> ScriptCalls = new List<int>();

        public bool BreakFromSegments = false;

        public NPCPage(string key)
        {
            Key = key;
        }

        public string ArgumentParse(string key)
        {
            ScriptPageKeyEntry entry = ScriptPageKeyCatalog.Find(key);

            // Automatic markers and input pages keep their arguments inside the key; only clickable
            // pages split "…(a,b)" into Args and keep an empty "()" so the header still matches.
            if (entry == null || !entry.UsesArguments) return key;

            string[] args;
            string normalized = ScriptPageKeyCatalog.SplitArguments(key, out args);

            if (args.Length == 0) return key;

            Args = new List<string>(args);

            return normalized;
        }
    }
}