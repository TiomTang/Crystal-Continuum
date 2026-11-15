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
        Directive,
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

    /// <summary>Insertion rows derived from the same catalogs the loader dispatches.</summary>
    public static class ScriptStructureCatalog
    {
        public static List<StructureCandidate> Build(ScriptProject project, int line, NPCScriptType? type = null)
        {
            var rows = new List<StructureCandidate>();

            AddHeader(rows, GameLanguage.Get("Blocks"));
            rows.AddRange(Blocks(project, line));
            AddSections(rows, project);
            AddHeader(rows, GameLanguage.Get("Directives"));
            rows.AddRange(Directives());
            AddPages(rows);
            AddHeader(rows, GameLanguage.Get("Automatic pages"));
            rows.AddRange(AutoMarkers(type));

            return rows;
        }

        /// <summary>Rows a data section line can still add: pages and sections belong to the file.</summary>
        public static List<StructureCandidate> TopLevel(ScriptProject project, NPCScriptType? type = null)
        {
            // Inside a data section only rows that belong to the whole file make sense; a block tag or
            // an INCLUDE would be inserted into the section body and never be loaded.
            var rows = new List<StructureCandidate>();

            AddSections(rows, project);
            AddPages(rows);
            AddHeader(rows, GameLanguage.Get("Automatic pages"));
            rows.AddRange(AutoMarkers(type));

            return rows;
        }

        /// <summary>All block tags for menus, or the tags not yet used in the caret's segment.</summary>
        public static List<StructureCandidate> Blocks(ScriptProject project = null, int line = 0)
        {
            var rows = new List<StructureCandidate>();
            var used = new HashSet<ScriptBlockKind>();

            if (project != null)
            {
                foreach (ScriptBlock block in project.SegmentAt(line)) used.Add(block.Kind);
            }

            foreach (ScriptBlockEntry entry in ScriptBlockCatalog.Entries)
            {
                // A splitting tag starts a fresh segment, so it is always available. ELSE blocks
                // need no matching SAY/ACT: the engine accepts either branch independently.
                if (!entry.SplitsSegment && used.Contains(entry.Kind)) continue;

                rows.Add(new StructureCandidate(ScriptStructureGroup.Block, entry.Text,
                    GameLanguage.Get(entry.Description), entry.Text + "\r\n", false));
            }

            return rows;
        }

        /// <summary>Rows for menus that always insert a section, without looking at the buffer.</summary>
        public static List<StructureCandidate> Sections()
        {
            var rows = new List<StructureCandidate>();

            foreach (ScriptSectionEntry entry in ScriptSectionCatalog.Entries)
                rows.Add(Section(entry));

            return rows;
        }

        /// <summary>Loader directives insert at the caret, since INCLUDE may supply a block body.</summary>
        public static List<StructureCandidate> Directives()
        {
            return new List<StructureCandidate>
            {
                new StructureCandidate(ScriptStructureGroup.Directive, ScriptDirectiveCatalog.Insert.Tag,
                    GameLanguage.Get("Insert a whole file relative to EnvirPath; include its extension and use a path without spaces."),
                    ScriptDirectiveCatalog.Insert.Tag + " [NPCs/Library.txt]\r\n", false),
                new StructureCandidate(ScriptStructureGroup.Directive, ScriptDirectiveCatalog.Include.Tag,
                    GameLanguage.Get("Include the brace-delimited body of a page from a file relative to EnvirPath; write the page without brackets."),
                    ScriptDirectiveCatalog.Include.Tag + " [NPCs/Library.txt] @Library\r\n", false),
            };
        }

        /// <summary>Automatic marker pages, optionally restricted to their actual owning script type.</summary>
        public static List<StructureCandidate> AutoMarkers(NPCScriptType? type = null)
        {
            var rows = new List<StructureCandidate>();

            foreach (ScriptAutoMarkerEntry entry in ScriptAutoMarkerCatalog.Entries)
            {
                if (type.HasValue && entry.Type != type.Value) continue;

                rows.Add(Page(entry.Key, GameLanguage.Get(entry.Description),
                    ScriptTemplateCatalog.PageText(entry.Key)));
            }

            return rows;
        }

        public static StructureCandidate NewPage()
        {
            string key = ScriptPageKeyCatalog.Page.Prefix + "@NewPage]";
            return Page(key, GameLanguage.Get("Insert a new page."), ScriptTemplateCatalog.PageText(key));
        }

        private static void AddPages(List<StructureCandidate> rows)
        {
            AddHeader(rows, GameLanguage.Get("Pages"));
            rows.Add(NewPage());

            string input = ScriptPageKeyCatalog.Input.Prefix + "NewPage]";
            rows.Add(Page(input, GameLanguage.Get("Insert an input request page."), ScriptTemplateCatalog.PageText(input)));

            foreach (ScriptSpecialPageEntry entry in ScriptSpecialPageCatalog.Entries)
                rows.Add(Page(entry.Key, GameLanguage.Get(entry.Description), ScriptTemplateCatalog.PageText(entry.Key)));
        }

        /// <summary>A data section already present is a jump target rather than a second copy.</summary>
        private static void AddSections(List<StructureCandidate> rows, ScriptProject project)
        {
            AddHeader(rows, GameLanguage.Get("Sections"));

            foreach (ScriptSectionEntry entry in ScriptSectionCatalog.Entries)
            {
                ScriptPageInfo existing = project.Sections.Find(x => x.Section == entry.Kind);
                StructureCandidate candidate = Section(entry);

                if (existing != null)
                {
                    candidate = new StructureCandidate(ScriptStructureGroup.Section, entry.Key,
                        GameLanguage.Get(entry.Description) + " " + entry.Usage, null, true) { JumpLine = existing.Line };
                }

                rows.Add(candidate);
            }
        }

        private static StructureCandidate Section(ScriptSectionEntry entry)
        {
            return new StructureCandidate(ScriptStructureGroup.Section, entry.Key,
                GameLanguage.Get(entry.Description) + " " + entry.Usage, entry.Key + "\r\n", true);
        }

        private static void AddHeader(List<StructureCandidate> rows, string text)
        {
            rows.Add(new StructureCandidate(ScriptStructureGroup.Value, text, string.Empty, null, false));
        }

        private static StructureCandidate Page(string label, string description, string text)
        {
            return new StructureCandidate(ScriptStructureGroup.Page, label, description, text, true);
        }
    }
}
