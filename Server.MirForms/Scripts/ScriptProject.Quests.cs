using Server.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.MirForms.Scripts
{
    public sealed partial class ScriptProject
    {
        public QuestScriptDocument Quest { get; private set; }

        private void ParseQuest()
        {
            // Keep the editor's final blank line in the section's caret range.
            Quest = QuestScriptParser.ParseLines(Lines, _path);
            foreach (QuestScriptSection section in Quest.Sections)
            {
                var page = new ScriptPageInfo
                {
                    Key = section.Entry.Key, Line = section.Line, EndLine = section.EndLine,
                    IsSection = true, QuestSection = section.Entry,
                };
                Pages.Add(page);
                Sections.Add(page);
            }
        }

        private List<ScriptIssue> CheckQuest(ScriptRules rules)
        {
            var issues = Quest.Diagnostics.Select(FromDiagnostic).ToList();
            if (rules == null) return issues;
            foreach (QuestScriptSection section in Quest.Sections)
            {
                foreach (QuestScriptRow row in section.Rows)
                {
                    string message = null;
                    switch (section.Entry.ValueKind)
                    {
                        case QuestScriptValueKind.Item:
                            if (rules.ItemExists != null && !rules.ItemExists(row.Name))
                                message = GameLanguage.Get("Quest item was not found: {0}", row.Name);
                            break;
                        case QuestScriptValueKind.Reward:
                            if (rules.ItemExists != null && !rules.ItemExists(row.Name) &&
                                !rules.ItemExists(row.Name + "(M)") && !rules.ItemExists(row.Name + "(F)"))
                                message = GameLanguage.Get("Quest reward item was not found: {0}", row.Name);
                            break;
                        case QuestScriptValueKind.Monster:
                            if (rules.MonsterExists != null && !rules.MonsterExists(row.Name))
                                message = GameLanguage.Get("Quest monster was not found: {0}", row.Name);
                            break;
                    }
                    if (message != null)
                        AddSectionIssue(issues, row.Source, section.Entry.Key, message, false);
                }
            }
            return issues;
        }
    }
}
