using System.Collections.Generic;
using Server.MirObjects;

namespace Server.MirForms.Scripts
{
    public enum ScriptStructureGroup
    {
        Value,
        Page,
        Block,
        Section,
    }

    /// <summary>
    /// One row of the palette. Value rows carry a single value, structure rows carry the text they
    /// insert; a row can also stand for a section that already exists, in which case choosing it jumps
    /// to that section instead of adding a duplicate.
    /// </summary>
    public sealed class StructureCandidate
    {
        public readonly ScriptStructureGroup Group;
        public readonly string Label;
        public readonly string Description;
        public readonly string Text;
        public readonly bool TopLevel;

        /// <summary>1-based line of the section this row already stands for; 0 when the row inserts.</summary>
        public int JumpLine;

        public StructureCandidate(string label, string description, string text)
            : this(ScriptStructureGroup.Value, label, description, text, false)
        {
        }

        public StructureCandidate(ScriptStructureGroup group, string label, string description, string text, bool topLevel)
        {
            Group = group;
            Label = label;
            Description = description;
            Text = text;
            TopLevel = topLevel;
        }

        /// <summary>Rows that only name a group: nothing to insert and nowhere to jump.</summary>
        public bool IsHeader { get { return Text == null && JumpLine <= 0; } }

        public bool IsJump { get { return JumpLine > 0; } }
    }

    /// <summary>
    /// Structure rows for the caret. The rules mirror NPCScript.ParseSegment: every #IF starts a new
    /// segment, and one segment holds at most one SAY, ACT, ELSESAY and ELSEACT, so the palette only
    /// offers what the loader can still accept at that point.
    /// </summary>
    public static class ScriptStructureCatalog
    {
        private const string NewPageText = "[@NewPage]\r\n#SAY\r\n\r\n#ACT\r\n\r\n";
        private const string InputPageText = "[@@NewPage]\r\n#SAY\r\n\r\n#ACT\r\n\r\n";

        public static List<StructureCandidate> Build(ScriptProject project, int line)
        {
            var rows = new List<StructureCandidate>();

            // Blocks come first: while writing a page they are what the caret needs, and the page
            // list is long enough to bury anything below it.
            AddBlocks(rows, project, line);
            AddSections(rows, project);
            AddPages(rows);

            return rows;
        }

        /// <summary>Rows for menus that always insert a section, without looking at the buffer.</summary>
        public static List<StructureCandidate> Sections()
        {
            var rows = new List<StructureCandidate>();

            foreach (ScriptSectionInfo info in ScriptProject.SectionCatalog)
            {
                rows.Add(new StructureCandidate(ScriptStructureGroup.Section, info.Key, Description(info.Kind), info.Key + "\r\n", true));
            }

            return rows;
        }

        public static StructureCandidate NewPage()
        {
            return Page("[@NewPage]", GameLanguage.Get("Insert a new page."), NewPageText);
        }

        private static void AddPages(List<StructureCandidate> rows)
        {
            AddHeader(rows, "Pages");

            rows.Add(Page("[@NewPage]", GameLanguage.Get("Insert a new page."), NewPageText));
            rows.Add(Page("[@@NewPage]", GameLanguage.Get("Insert an input request page."), InputPageText));

            foreach (ScriptSpecialPage page in ScriptSpecialPages.Entries)
            {
                rows.Add(Page(page.Key, GameLanguage.Get(page.Description), page.Key + "\r\n"));
            }
        }

        private static void AddBlocks(List<StructureCandidate> rows, ScriptProject project, int line)
        {
            AddHeader(rows, "Blocks");

            bool say = false, act = false, elseSay = false, elseAct = false;

            foreach (ScriptBlock block in project.SegmentAt(line))
            {
                switch (block.Kind)
                {
                    case ScriptBlockKind.Say:
                        say = true;
                        break;
                    case ScriptBlockKind.Act:
                        act = true;
                        break;
                    case ScriptBlockKind.ElseSay:
                        elseSay = true;
                        break;
                    case ScriptBlockKind.ElseAct:
                        elseAct = true;
                        break;
                }
            }

            rows.Add(Block("#IF", GameLanguage.Get("Start a condition block."), "#IF\r\n"));

            if (!say) rows.Add(Block("#SAY", GameLanguage.Get("Text the NPC says."), "#SAY\r\n"));

            if (!act) rows.Add(Block("#ACT", GameLanguage.Get("Actions that run when the checks pass."), "#ACT\r\n"));

            if (say && !elseSay) rows.Add(Block("#ELSESAY", GameLanguage.Get("Text when the checks fail."), "#ELSESAY\r\n"));

            if (act && !elseAct) rows.Add(Block("#ELSEACT", GameLanguage.Get("Actions when the checks fail."), "#ELSEACT\r\n"));
        }

        /// <summary>
        /// Every section the loader understands. A section that already exists in the file is offered
        /// as a jump target: a second copy would feed the same list twice.
        /// </summary>
        private static void AddSections(List<StructureCandidate> rows, ScriptProject project)
        {
            AddHeader(rows, "Sections");

            foreach (ScriptSectionInfo info in ScriptProject.SectionCatalog)
            {
                ScriptPageInfo existing = project.Sections.Find(x => x.Section == info.Kind);
                string description = Description(info.Kind);

                if (existing == null)
                {
                    rows.Add(new StructureCandidate(ScriptStructureGroup.Section, info.Key, description, info.Key + "\r\n", true));
                    continue;
                }

                var jump = new StructureCandidate(ScriptStructureGroup.Section, info.Key, description, null, true)
                {
                    JumpLine = existing.Line,
                };

                rows.Add(jump);
            }
        }

        /// <summary>
        /// Display text for a section. The literals live here so the localization scan (which reads
        /// GameLanguage.Get call sites) keeps them in the catalog.
        /// </summary>
        private static string Description(ScriptSectionKind kind)
        {
            switch (kind)
            {
                case ScriptSectionKind.Types: return GameLanguage.Get("Item type numbers this NPC works with.");
                case ScriptSectionKind.UsedTypes: return GameLanguage.Get("Item type numbers this NPC reacts to.");
                case ScriptSectionKind.Trade: return GameLanguage.Get("Goods this NPC sells.");
                case ScriptSectionKind.Recipe: return GameLanguage.Get("Items this NPC can craft.");
                case ScriptSectionKind.Quests: return GameLanguage.Get("Quests this NPC handles.");
                case ScriptSectionKind.Speech: return GameLanguage.Get("Speech numbers this NPC can say.");
                default: return string.Empty;
            }
        }

        private static void AddHeader(List<StructureCandidate> rows, string text)
        {
            rows.Add(new StructureCandidate(ScriptStructureGroup.Value, GameLanguage.Get(text), string.Empty, null, false));
        }

        private static StructureCandidate Page(string label, string description, string text)
        {
            return new StructureCandidate(ScriptStructureGroup.Page, label, description, text, true);
        }

        private static StructureCandidate Block(string label, string description, string text)
        {
            return new StructureCandidate(ScriptStructureGroup.Block, label, description, text, false);
        }
    }
}
