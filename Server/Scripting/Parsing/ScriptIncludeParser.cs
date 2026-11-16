using System.Collections.Generic;

namespace Server.Scripting
{
    /// <summary>The brace-delimited body selected by an INCLUDE page header.</summary>
    internal static class ScriptIncludeParser
    {
        internal static bool TryFindBody(IList<ScriptSourceLine> lines, int header, out int opener, out int closer)
        {
            opener = closer = -1;
            int depth = 0;
            for (int i = header + 1; i < lines.Count; i++)
            {
                // Only the opener is restricted to the requested page; a body may contain more pages.
                if (opener < 0 && ScriptPageKeyCatalog.TryParseHeader(lines[i].Text, out _)) break;
                string text = lines[i].Text.Trim();
                if (text == "{")
                {
                    depth++;
                    if (opener < 0) opener = i;
                }
                else if (opener >= 0 && text == "}" && --depth == 0)
                {
                    closer = i;
                    return true;
                }
            }
            return false;
        }
    }
}
