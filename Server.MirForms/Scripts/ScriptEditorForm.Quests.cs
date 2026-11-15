using Server.MirEnvir;
using Server.Scripting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Server.MirForms.Scripts
{
    public sealed partial class ScriptEditorForm
    {
        private List<StructureCandidate> _monsterCandidates;
        private List<StructureCandidate> _flagCandidates;

        private static string QuestTemplate()
        {
            return string.Join("\r\n\r\n", QuestScriptSectionCatalog.Entries.Select(entry =>
                entry.Key + "\r\n" + (entry.ValueKind == QuestScriptValueKind.Number ? "0\r\n" : string.Empty))) + "\r\n";
        }

        private void RefreshQuestPalette(ScriptProject project, int line)
        {
            _structureCandidates.AddRange(ScriptStructureCatalog.QuestSections(project));
            ScriptPageInfo page = project.SectionPageAt(line);
            if (page?.QuestSection == null)
            {
                _paletteHint = GameLanguage.Get("Choose a quest section to insert its values.");
                FillPalette();
                return;
            }
            QuestScriptValueKind kind = page.QuestSection.ValueKind;
            _paletteHint = QuestValueHint(kind);
            IEnumerable<StructureCandidate> values;
            switch (kind)
            {
                case QuestScriptValueKind.Item:
                    values = ItemCandidates().Select(item => new StructureCandidate(item.Label,
                        QuestValueHint(kind), item.Text.Replace(" ", string.Empty) + " 1 \"\""));
                    break;
                case QuestScriptValueKind.Reward:
                    values = ItemCandidates().Select(item => new StructureCandidate(item.Label,
                        QuestValueHint(kind), item.Text.Replace(" ", string.Empty) + " 1"));
                    break;
                case QuestScriptValueKind.Monster:
                    values = MonsterCandidates();
                    break;
                case QuestScriptValueKind.Flag:
                    values = FlagCandidates();
                    break;
                case QuestScriptValueKind.Number:
                    values = new[] { "0", "100", "1000" }.Select(value =>
                        new StructureCandidate(value, QuestValueHint(kind), value));
                    break;
                default:
                    values = new[] { new StructureCandidate(GameLanguage.Get("Text"), QuestValueHint(kind), GameLanguage.Get("Text")),
                        new StructureCandidate(GameLanguage.Get("Colored text"), GameLanguage.Get("Text with a client colour name."),
                            "{" + GameLanguage.Get("Text") + "/Red}") };
                    break;
            }
            var section = project.Quest.Sections.FirstOrDefault(x => x.Line == page.Line);
            var existing = new HashSet<string>(section?.Rows.Select(x => x.Source.Text) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            _valueCandidates.AddRange(values.Where(x => !existing.Contains(x.Text)));
            FillPalette();
        }

        private List<StructureCandidate> MonsterCandidates()
        {
            if (_monsterCandidates != null) return _monsterCandidates;
            _monsterCandidates = new List<StructureCandidate>();
            var monsters = NonEmpty(Envir.Main?.MonsterInfoList, Envir.Edit?.MonsterInfoList);
            if (monsters != null)
                foreach (var monster in monsters.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                    _monsterCandidates.Add(new StructureCandidate(monster.Name, QuestValueHint(QuestScriptValueKind.Monster),
                        monster.Name.Replace(" ", string.Empty) + " 1 \"\""));
            return _monsterCandidates;
        }

        private List<StructureCandidate> FlagCandidates()
        {
            if (_flagCandidates != null) return _flagCandidates;
            _flagCandidates = new List<StructureCandidate>();
            for (int flag = 0; flag <= Globals.FlagIndexCount - 1000; flag++)
            {
                string number = flag.ToString(CultureInfo.InvariantCulture);
                _flagCandidates.Add(new StructureCandidate(number, QuestValueHint(QuestScriptValueKind.Flag), number + " \"\""));
            }
            return _flagCandidates;
        }

        internal static string QuestValueHint(QuestScriptValueKind kind)
        {
            switch (kind)
            {
                case QuestScriptValueKind.Item:
                    return GameLanguage.Get("Item name, optional UInt16 count and quoted task message.");
                case QuestScriptValueKind.Reward:
                    return GameLanguage.Get("Reward item name and optional UInt16 count; gender variants are supported.");
                case QuestScriptValueKind.Monster:
                    return GameLanguage.Get("Monster name, optional Int32 count and quoted task message.");
                case QuestScriptValueKind.Flag:
                    return GameLanguage.Get("Quest flag number and optional quoted task message.");
                case QuestScriptValueKind.Number:
                    return GameLanguage.Get("Unsigned reward amount (0 to 4294967295).");
                default:
                    return GameLanguage.Get("Quest description text; NPC commands are not executed here.");
            }
        }
    }
}
