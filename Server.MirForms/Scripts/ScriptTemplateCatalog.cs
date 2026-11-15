using Server.MirObjects;

namespace Server.MirForms.Scripts
{
    /// <summary>A complete editable snippet; Label is an English localization key.</summary>
    public sealed class ScriptTemplateEntry
    {
        public string Label { get; }
        public string Text { get; }

        public ScriptTemplateEntry(string label, string text)
        {
            Label = label;
            Text = text;
        }
    }

    /// <summary>Toolbar templates composed from registered block, page and command spellings.</summary>
    public static class ScriptTemplateCatalog
    {
        private const string NewLine = "\r\n";
        private static readonly string Page = ScriptPageKeyCatalog.Page.Prefix + "@NewPage]";
        private static readonly string Input = ScriptPageKeyCatalog.Input.Prefix + "Input]";
        private static readonly string Library = ScriptPageKeyCatalog.Page.Prefix + "@Library]";

        public static readonly ScriptTemplateEntry[] Entries =
        {
            new ScriptTemplateEntry("Dialog Page", Page + NewLine
                + Block(ScriptBlockKind.Say, "Welcome.\\" + NewLine + "<Close/@Exit>")),
            new ScriptTemplateEntry("Conditional Page", Page + NewLine
                + Block(ScriptBlockKind.Check, ScriptCheckCatalog.Find(ScriptCheckKind.Level).Name + " >= 1")
                + Block(ScriptBlockKind.Say, "Welcome.\\" + NewLine + "<Close/@Exit>")
                + Block(ScriptBlockKind.ElseSay, "You do not meet the requirements.\\" + NewLine + "<Close/@Exit>")),
            new ScriptTemplateEntry("Input Page", Page + NewLine
                + Block(ScriptBlockKind.Say, "<Enter text/" + Input.Trim('[', ']') + ">" + NewLine + "<Close/@Exit>")
                + NewLine + Input + NewLine
                + Block(ScriptBlockKind.Act, ScriptActionCatalog.Find(ScriptActionKind.LocalMessage).Name + " \"%INPUTSTR\" " + ChatType.System)
                + Block(ScriptBlockKind.Say, "<Back/" + Page.Trim('[', ']') + ">")),
            new ScriptTemplateEntry("Teleport Page", Page + NewLine
                + "; Example only: replace map 0 and coordinates with your map definition." + NewLine
                + Block(ScriptBlockKind.Act, ScriptActionCatalog.Find(ScriptActionKind.Move).Name + " 0 330 330")),
            new ScriptTemplateEntry("Include library template", Library + NewLine + "{" + NewLine
                + Block(ScriptBlockKind.Say, "Shared dialogue.\\" + NewLine + "<Close/@Exit>") + "}" + NewLine),
        };

        /// <summary>Page skeleton shared by the structure palette and automatic marker candidates.</summary>
        public static string PageText(string key)
        {
            return key + NewLine + Block(ScriptBlockKind.Say) + Block(ScriptBlockKind.Act);
        }

        private static string Block(ScriptBlockKind kind, string body = "")
        {
            return ScriptBlockCatalog.Tag(kind) + NewLine + body + NewLine;
        }
    }
}
