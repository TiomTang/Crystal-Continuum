using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        /// <summary>Source lines with their file numbers, shared by every page of one load.</summary>
        private List<ScriptSourceLine> _scriptLines;
        private List<ScriptSourceLine> _expandedSourceLines;

        /// <summary>Normalised page header to its line in <see cref="_scriptLines"/>.</summary>
        private Dictionary<string, int> _pageHeaderIndex;

        public void LoadInfo()
        {
            ClearInfo();

            if (!Directory.Exists(Settings.NPCPath)) return;

            string fileName = Path.Combine(Settings.NPCPath, FileName + ".txt");

            if (!File.Exists(fileName))
            {
                ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingScript,
                    new ScriptParseContext { FileName = FileName + ".txt" }, "NPC", "Script file was not found; no pages were loaded.");
                MessageQueue.Enqueue(GameLanguage.Get("Script Not Found: {0}", FileName));
                return;
            }

            ScriptParseScope.Set(FileName + ".txt");

            try
            {
                bool legacy;

                var sourceLines = ScriptSourceLine.Bind(ScriptTextFile.ReadAllLines(fileName, out legacy), FileName + ".txt");

                if (legacy)
                    ScriptDiagnostics.Report(ScriptDiagnosticKind.LegacyEncoding,
                        new ScriptParseContext { FileName = FileName + ".txt" }, string.Empty,
                        "file is not UTF-8, read as GBK");

                foreach (ScriptDirectiveEntry directive in ScriptDirectiveCatalog.Ordered)
                    directive.ApplySource(sourceLines);

                PrepareScriptSourceLines(sourceLines);
                List<string> lines = sourceLines.Select(line => line.Text).ToList();

                switch (Type)
                {
                    case NPCScriptType.Normal:
                    default:
                        ParseScript(lines);

                        if (ScriptDiagnostics.DeepScan) ReportUnreachablePages(lines);
                        break;
                    case NPCScriptType.AutoPlayer:
                    case NPCScriptType.AutoMonster:
                    case NPCScriptType.Robot:
                        ParseDefault(lines);
                        break;
                }
            }
            catch (Exception ex)
            {
                // One unreadable or unexpected script must not stop the NPC load. The script stays empty
                // and the reason is reported with the file name, so the author can find it.
                ClearInfo();

                ScriptDiagnostics.Report(ScriptDiagnosticKind.LoadFailed,
                    new ScriptParseContext { FileName = FileName + ".txt" }, string.Empty, ex.Message, ex);

                MessageQueue.Enqueue("Script Not Loaded: " + FileName);
            }
            finally
            {
                _scriptLines = null;
                _expandedSourceLines = null;
                _pageHeaderIndex = null;

                ScriptParseScope.Set(null);
            }
        }

        private static List<ScriptSourceLine> BuildScriptLines(IList<string> scriptLines)
        {
            var lines = new List<ScriptSourceLine>(scriptLines.Count);

            for (int i = 0; i < scriptLines.Count; i++)
            {
                if (string.IsNullOrEmpty(scriptLines[i])) continue;

                lines.Add(new ScriptSourceLine(scriptLines[i], i + 1, ScriptParseScope.FileName));
            }

            return lines;
        }

        private void PrepareScriptLines(IList<string> scriptLines)
        {
            PrepareScriptSourceLines(ScriptSourceLine.Bind(scriptLines, ScriptParseScope.FileName));
        }

        private void PrepareScriptSourceLines(List<ScriptSourceLine> sourceLines)
        {
            _expandedSourceLines = sourceLines;
            _scriptLines = sourceLines.Where(line => !string.IsNullOrEmpty(line.Text)).ToList();
            _pageHeaderIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _scriptLines.Count; i++)
            {
                string header;
                string[] args;

                if (!ScriptPageKeyCatalog.TryParseHeader(_scriptLines[i].Text, out header)) continue;

                string normalized = NPCPage.NormalizeSectionName(header, out args);

                if (!_pageHeaderIndex.ContainsKey(normalized)) _pageHeaderIndex[normalized] = i;
            }
        }

        /// <summary>
        /// First line whose header matches the normalised key. The index is only trusted when the
        /// header is written exactly as the key, so a page defined with arguments still falls back to
        /// the original prefix scan instead of changing what counts as a match.
        /// </summary>
        private int FindPageHeader(string tempSectionName, IList<ScriptSourceLine> lines)
        {
            int index;

            if (ReferenceEquals(lines, _scriptLines) && _pageHeaderIndex != null && _pageHeaderIndex.TryGetValue(tempSectionName, out index))
            {
                if (string.Equals(_scriptLines[index].Text, tempSectionName, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Text;

                if (ScriptLexer.IsComment(line)) continue;

                if (line.StartsWith(tempSectionName, StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }

        public void ClearInfo()
        {
            Goods = new List<UserItem>();
            Types = new List<ItemType>();
            UsedTypes = new List<ItemType>();
            NPCPages = new List<NPCPage>();
            CraftGoods = new List<RecipeInfo>();

            if (Type == NPCScriptType.AutoPlayer)
            {
                Envir.CustomCommands.Clear();
            }
        }
        public void LoadGoods()
        {
            var loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null) return;

            string path = Path.Combine(Settings.GoodsPath, loadedNPC.Info.Index.ToString() + ".msd");

            var loaded = new List<UserItem>();

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int version = reader.ReadInt32();
                        int count = version;
                        int customversion = Envir.LoadCustomVersion;
                        if (version == 9999)//the only real way to tell if the file was made before or after version code got added: assuming nobody had a config option to save more then 10000 sold items
                        {
                            version = reader.ReadInt32();
                            customversion = reader.ReadInt32();
                            count = reader.ReadInt32();
                        }
                        else
                            version = Envir.LoadVersion;

                        if (count < 0 || count > 100000)
                            throw new ArgumentException("Invalid goods count: " + count);

                        for (int k = 0; k < count; k++)
                        {
                            UserItem item = new UserItem(reader, version, customversion);
                            if (Envir.BindItem(item))
                                loaded.Add(item);
                        }
                    }
                }
            }

            // Swap only after the file has been read completely, so a truncated .msd cannot leave the
            // NPC with half its stock or clear what the previous load had.
            loadedNPC.UsedGoods.Clear();
            loadedNPC.UsedGoods.AddRange(loaded);
        }

        private void ParseDefault(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (ScriptPageKeyCatalog.Find(lines[i]) != ScriptPageKeyCatalog.Auto) continue;

                ScriptAutoMarkerLine markerLine;
                ScriptAutoMarkerEntry marker = ScriptAutoMarkerCatalog.Find(Type, lines[i], out markerLine);

                if (marker != null && !marker.Apply(this, markerLine)) continue;

                NPCPages.AddRange(ParsePages(lines, lines[i]));
            }
        }

        /// <summary>
        /// Parses the pages no button points at, only to report what is wrong inside them. Nothing opens a
        /// dead page, so without this its unknown keywords stay invisible until someone wires it up.
        /// </summary>
        private void ReportUnreachablePages(IList<string> lines)
        {
            var parsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (NPCPage page in NPCPages) parsed.Add(page.Key);

            for (int i = 0; i < lines.Count; i++)
            {
                string key;

                if (!ScriptPageKeyCatalog.TryParseHeader(lines[i], out key)) continue;
                if (!parsed.Add(key)) continue;

                ScriptDiagnostics.DeepScanned++;

                ParsePage(lines, key);
            }
        }

        private void ParseScript(IList<string> lines)
        {
            ReportDuplicatePages(lines);

            NPCPages.AddRange(ParsePages(lines));

            foreach (ScriptSectionEntry section in ScriptSectionCatalog.Ordered)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (!section.MatchesHeader(lines[i])) continue;

                    section.Parse(this, lines, i);
                }
            }
        }

        /// <summary>
        /// Reports a page header that appears more than once in the same file. The loader keeps the
        /// first copy, so without this the later bodies vanish without a word.
        /// </summary>
        private void ReportDuplicatePages(IList<string> lines)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Count; i++)
            {
                string key;

                if (!ScriptPageKeyCatalog.TryParseHeader(lines[i], out key)) continue;
                if (seen.Add(key)) continue;

                ScriptDiagnostics.Report(ScriptDiagnosticKind.DuplicatePage, new ScriptParseContext
                {
                    Line = lines[i],
                    LineNumber = SourceAt(lines, i).Number,
                    FileName = SourceAt(lines, i).FileName,
                    Source = SourceAt(lines, i).Source,
                }, key, "Duplicate page \"" + key + "\"");
            }
        }

        /// <summary>
        /// Reports a button or jump target that no page header answers to. Without this the target
        /// used to become an empty page, so the click did nothing and nothing was logged.
        /// </summary>
        private void ReportMissingPage(string key)
        {
            // A key built from a run-time value has no page yet; only a literal target can be dead.
            if (string.IsNullOrEmpty(key) || key.IndexOf("<$", StringComparison.Ordinal) >= 0) return;
            if (ScriptValidationScope.IsActive && (ScriptLexer.Argument.IsMatch(key) ||
                key.IndexOf("%INPUTSTR", StringComparison.Ordinal) >= 0)) return;

            string target = key.Trim('[', ']');
            ScriptSourceInfo source = _scriptLines?.FirstOrDefault(line =>
                line.Text.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0).Source;
            ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingPage, new ScriptParseContext
            {
                FileName = ScriptParseScope.FileName,
                Source = source,
            }, key, "Page not found: " + key);
        }

        private List<NPCPage> ParsePages(IList<string> lines, string key = null)
        {
            if (key == null) key = ScriptSpecialPageCatalog.Key(ScriptSpecialPageKind.Main);

            List<NPCPage> pages = new List<NPCPage>();
            List<string> buttons = new List<string>();
            var visited = new HashSet<string>(NPCPages.Select(existing => existing.Key), StringComparer.OrdinalIgnoreCase);

            if (!visited.Add(key)) return pages;
            if (ScriptValidationScope.Current?.TryConsumePage(lines.Count, key,
                _scriptLines?.FirstOrDefault().Source) == false) return pages;

            NPCPage page = ParsePage(lines, key);

            if (page == null)
            {
                // [@MAIN] has no body only when the file is not a dialogue script; the engine still
                // needs its page object so a caller can open an empty window instead of failing.
                if (ScriptSpecialPageCatalog.Find(key) == null) ReportMissingPage(key);
                else pages.Add(new NPCPage(key) { FileName = FileName + ".txt" });

                return pages;
            }

            pages.Add(page);

            buttons.AddRange(page.Buttons);

            for (int i = 0; i < buttons.Count; i++)
            {
                string section = buttons[i];

                if (!visited.Add(section)) continue;
                if (ScriptValidationScope.Current?.TryConsumePage(lines.Count, section, page?.Source) == false) break;

                page = ParsePage(lines, section);

                if (page == null)
                {
                    // A special page is answered by the engine, not by script text: it keeps an empty
                    // page as its carrier so Response still reaches ProcessSpecial. @Exit is closed
                    // by the client and never travels here.
                    if (ScriptSpecialPageCatalog.Find(section) != null ||
                        section.StartsWith("[@@", StringComparison.OrdinalIgnoreCase))
                    {
                        // Input pages are driven by the client's text prompt, so they need the same
                        // empty carrier a special page does.
                        pages.Add(new NPCPage(section) { FileName = FileName + ".txt" });
                    }
                    else if (!section.StartsWith("[@_", StringComparison.OrdinalIgnoreCase) &&
                             !string.Equals(section, "[@EXIT]", StringComparison.OrdinalIgnoreCase))
                    {
                        ReportMissingPage(section);
                    }

                    continue;
                }

                buttons.AddRange(page.Buttons);

                pages.Add(page);
            }

            return pages;
        }

        private NPCPage ParsePage(IList<string> scriptLines, string sectionName)
        {
            // Numbers travel with the text: empty lines are dropped here, so the index in this list is
            // no longer the line the script file has. The list is built once per load and shared by
            // every page, which is why a page no longer rebuilds it.
            var lines = _scriptLines ?? BuildScriptLines(scriptLines);

            NPCPage Page = new NPCPage(sectionName) { FileName = FileName + ".txt" };

            //Cleans arguments out of search page name
            string tempSectionName = Page.ArgumentParse(sectionName);

            int header = FindPageHeader(tempSectionName, lines);

            if (header < 0) return null;
            Page.Source = lines[header].Source;

            // Establish the page interval before looking at block boundaries. Looking only at the
            // next line lets an adjacent empty page absorb the following page's first #IF block.
            int end = header + 1;

            while (end < lines.Count && !ScriptPageKeyCatalog.TryParseHeader(lines[end].Text, out _)) end++;

            var segmentLines = new List<ScriptSourceLine>();

            for (int i = header + 1; i < end; i++)
            {
                if (ScriptBlockCatalog.SplitsSegment(lines[i].Text) && segmentLines.Count > 0)
                {
                    AddPageSegment(Page, segmentLines);
                    segmentLines.Clear();
                }

                segmentLines.Add(lines[i]);
            }

            if (segmentLines.Count > 0) AddPageSegment(Page, segmentLines);

            return Page;
        }

        private void AddPageSegment(NPCPage page, List<ScriptSourceLine> lines)
        {
            NPCSegment segment = ParseSegment(page, lines);
            page.Buttons.AddRange(segment.Buttons);
            page.Buttons.AddRange(segment.ElseButtons);
            page.Buttons.AddRange(segment.GotoButtons);
            page.SegmentList.Add(segment);
        }

        private ScriptSourceLine SourceAt(IList<string> lines, int index)
        {
            return _expandedSourceLines != null && index < _expandedSourceLines.Count
                ? _expandedSourceLines[index]
                : new ScriptSourceLine(lines[index], index + 1, ScriptParseScope.FileName);
        }

        private NPCSegment ParseSegment(NPCPage page, IEnumerable<ScriptSourceLine> scriptLines)
        {
            List<ScriptSourceLine>
                checks = new List<ScriptSourceLine>(),
                acts = new List<ScriptSourceLine>(),
                elseActs = new List<ScriptSourceLine>();

            List<string>
                say = new List<string>(),
                buttons = new List<string>(),
                elseSay = new List<string>(),
                elseButtons = new List<string>(),
                gotoButtons = new List<string>();

            List<ScriptSourceLine> lines = scriptLines.ToList();

            // Tag to destination. The segment asks which role the tag carries, not which tag it was,
            // so a new block type that reuses a role needs no code here.
            var roleLines = new Dictionary<ScriptBlockRole, List<ScriptSourceLine>>
            {
                { ScriptBlockRole.Check, checks },
                { ScriptBlockRole.Act, acts },
                { ScriptBlockRole.ElseAct, elseActs },
            };

            var roleText = new Dictionary<ScriptBlockRole, List<string>>
            {
                { ScriptBlockRole.Say, say },
                { ScriptBlockRole.ElseSay, elseSay },
            };

            var roleButtons = new Dictionary<ScriptBlockRole, List<string>>
            {
                { ScriptBlockRole.Say, buttons },
                { ScriptBlockRole.ElseSay, elseButtons },
            };

            // A segment starts in the say block, exactly as the old single list did.
            List<ScriptSourceLine> currentLines = null;
            List<string> currentSay = say, currentButtons = buttons;
            NPCSegment segment = new NPCSegment(page, say, buttons, elseSay, elseButtons, gotoButtons);
            segment.Source = lines.FirstOrDefault().Source ?? page.Source;

            for (int i = 0; i < lines.Count; i++)
            {
                string text = lines[i].Text;

                if (lines[i].Error != null)
                {
                    segment.MarkInvalid(lines[i].Error, false);
                    continue;
                }

                if (ScriptLexer.IsComment(text)) continue;

                if (text.TrimStart().StartsWith("#"))
                {
                    ScriptBlockKind kind;
                    ScriptBlockEntry block = ScriptBlockCatalog.TryFind(text, out kind);

                    if (block == null)
                    {
                        segment.MarkInvalid(ReportUnknownBlock(page, lines[i]), false);
                        continue;
                    }

                    List<ScriptSourceLine> blockLines;
                    List<string> blockText;

                    if (roleLines.TryGetValue(block.Role, out blockLines))
                    {
                        currentLines = blockLines;
                        currentSay = null;
                    }
                    else
                    {
                        currentLines = null;
                        currentSay = roleText.TryGetValue(block.Role, out blockText) ? blockText : null;
                    }

                    List<string> blockButtons;
                    currentButtons = roleButtons.TryGetValue(block.Role, out blockButtons) ? blockButtons : null;

                    continue;
                }

                if (ScriptPageKeyCatalog.TryParseHeader(text, out _)) break;

                if (currentButtons != null)
                {
                    string buttonText = segment.ParseArguments(new[] { text })[0];
                    Match match = ScriptLexer.Button.Match(buttonText);

                    while (match.Success)
                    {
                        string argu = match.Groups[1].Captures[0].Value;
                        argu = argu.Split('/')[0];

                        currentButtons.Add(string.Format("[{0}]", argu));
                        match = match.NextMatch();
                    }
                }

                if (currentLines != null) currentLines.Add(new ScriptSourceLine(text.TrimEnd(), lines[i].Number, lines[i].FileName, lines[i].Source));
                else if (currentSay != null)
                {
                    currentSay.Add(text.TrimEnd());
                    (ReferenceEquals(currentSay, say) ? segment.SaySources : segment.ElseSaySources).Add(lines[i].Source);
                }
            }

            string previousFile = ScriptParseScope.FileName;

            try
            {
                foreach (ScriptSourceLine line in checks)
                {
                    ScriptParseScope.Set(line.FileName ?? previousFile);
                    segment.ParseCheck(line.Text, line.Number, line.Source);
                }

                foreach (ScriptSourceLine line in acts)
                {
                    ScriptParseScope.Set(line.FileName ?? previousFile);
                    segment.ParseAct(segment.ActList, line.Text, line.Number, line.Source);
                }

                foreach (ScriptSourceLine line in elseActs)
                {
                    ScriptParseScope.Set(line.FileName ?? previousFile);
                    segment.ParseAct(segment.ElseActList, line.Text, line.Number, line.Source);
                }
            }
            finally
            {
                ScriptParseScope.Set(previousFile);
            }

            CollectActionButtons(segment.ActList, buttons, gotoButtons);
            CollectActionButtons(segment.ElseActList, elseButtons, gotoButtons);

            return segment;
        }

        private static void CollectActionButtons(IEnumerable<NPCActions> acts, List<string> buttons, List<string> gotoButtons)
        {
            foreach (NPCActions action in acts)
            {
                ScriptKeywordEntry entry = ScriptActionCatalog.Find(action.Type);

                if (entry == null || entry.Button == ScriptButtonKind.None) continue;

                int target = entry.Button == ScriptButtonKind.DelayedPage ? 1 : 0;

                if (action.Params.Count <= target || string.IsNullOrEmpty(action.Params[target])) continue;

                string button = "[" + action.Params[target] + "]";

                if (entry.Button == ScriptButtonKind.Roll) buttons.Add(button);
                else gotoButtons.Add(button);
            }
        }

        /// <summary>
        /// An unsupported block makes its segment unsafe; its body must not inherit the previous role.
        /// </summary>
        private static ScriptDiagnostic ReportUnknownBlock(NPCPage page, ScriptSourceLine line)
        {
            string text = (line.Text ?? string.Empty).Trim();
            int end = text.IndexOfAny(new[] { ' ', '\t' });
            string tag = end >= 0 ? text.Substring(0, end) : text;

            return ScriptDiagnostics.Report(ScriptDiagnosticKind.UnknownBlock, new ScriptParseContext
            {
                Line = line.Text,
                LineNumber = line.Number,
                FileName = line.FileName ?? ScriptParseScope.FileName,
                PageKey = page == null ? string.Empty : page.Key,
                Source = line.Source,
            }, tag, "Unknown block tag \"" + tag + "\"");
        }

        internal void ParseTypeSection(IList<string> lines, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(lines, headerIndex, ScriptSectionKind.Types))
            {
                int index;

                if (int.TryParse(entry.Text, out index)) Types.Add((ItemType)index);
            }
        }

        internal void ParseUsedTypeSection(IList<string> lines, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(lines, headerIndex, ScriptSectionKind.UsedTypes))
            {
                int index;

                if (int.TryParse(entry.Text, out index)) UsedTypes.Add((ItemType)index);
            }
        }

        internal void ParseGoodsSection(IList<string> lines, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(lines, headerIndex, ScriptSectionKind.Trade))
            {
                string line = entry.Text;
                string[] data = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 0) continue;

                ItemInfo info = Envir.GetItemInfo(data[0]);

                if (info == null) continue;

                UserItem goods = Envir.CreateShopItem(info, (uint)entry.Index);

                if (goods == null || Goods.Contains(goods))
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Could not find Item: {0}, File: {1}", line, FileName));
                    continue;
                }

                ushort count = 1;

                if (data.Length == 2) ushort.TryParse(data[1], out count);

                goods.Count = count;

                Goods.Add(goods);
            }
        }

        internal void ParseQuestsSection(IList<string> lines, int headerIndex)
        {
            NPCObject loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null) return;

            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(lines, headerIndex, ScriptSectionKind.Quests))
            {
                int index;

                int.TryParse(entry.Text, out index);

                if (index == 0) continue;

                QuestInfo info = Envir.GetQuestInfo(Math.Abs(index));

                if (info == null)
                {
                    ScriptDiagnostics.Report(ScriptDiagnosticKind.MalformedSection, new ScriptParseContext
                    {
                        Line = entry.Text,
                        LineNumber = SourceAt(lines, entry.Index).Number,
                        FileName = SourceAt(lines, entry.Index).FileName,
                    }, "Quests", "Unknown quest: " + index);

                    continue;
                }

                if (index > 0) info.NpcIndex = LoadedObjectID;
                else info.FinishNpcIndex = LoadedObjectID;

                if (loadedNPC.Quests.All(x => x != info)) loadedNPC.Quests.Add(info);
            }
        }

        internal void ParseSpeechSection(IList<string> lines, int headerIndex)
        {
            NPCObject loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null) return;

            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(lines, headerIndex, ScriptSectionKind.Speech))
            {
                string line = entry.Text;
                string[] parts = line.Split(' ');
                int weight = int.Parse(parts[0]);

                loadedNPC.Speech.Add(new NPCSpeech { Weight = weight, Message = line.Substring(parts[0].Length + 1) });
            }
        }

        internal void ParseCraftingSection(IList<string> lines, int headerIndex)
        {
            foreach (ScriptSectionLine entry in ScriptSectionCatalog.Body(lines, headerIndex, ScriptSectionKind.Recipe))
            {
                string line = entry.Text;
                string[] data = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 0) continue;

                ItemInfo info = Envir.GetItemInfo(data[0]);

                if (info == null) continue;

                RecipeInfo recipe = Envir.RecipeInfoList.SingleOrDefault(x => x.MatchItem(info.Index));

                if (recipe == null)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Could not find recipe: {0}, File: {1}", line, FileName));
                    continue;
                }

                if (recipe.Ingredients.Count == 0)
                {
                    MessageQueue.Enqueue(GameLanguage.Get("Could not find ingredients: {0}, File: {1}", line, FileName));
                    continue;
                }

                CraftGoods.Add(recipe);
            }
        }

        internal bool ApplyAutoMapCoord(ScriptAutoMarkerLine line)
        {
            if (!TryParseAutoMapCoord(line, out int x, out int y)) return false;

            Map map = Envir.MapList.FirstOrDefault(m => m.Info.FileName == line.Args[0]);

            if (map == null) return false;

            Point point = new Point((short)x, (short)y);

            if (!map.Info.ActiveCoords.Contains(point)) map.Info.ActiveCoords.Add(point);

            return true;
        }

        internal bool ApplyAutoCustomCommand(ScriptAutoMarkerLine line)
        {
            if (!HasAutoCustomCommand(line)) return false;

            Envir.CustomCommands.Add(line.Body);

            return true;
        }

        internal bool ApplyAutoSpawn(ScriptAutoMarkerLine line)
        {
            if (!TryParseAutoMonsterIndex(line, out int index)) return false;

            MonsterInfo info = Envir.GetMonsterInfo((short)index);

            if (info == null) return false;

            info.HasSpawnScript = true;

            return true;
        }

        internal bool ApplyAutoDie(ScriptAutoMarkerLine line)
        {
            if (!TryParseAutoMonsterIndex(line, out int index)) return false;

            MonsterInfo info = Envir.GetMonsterInfo((short)index);

            if (info == null) return false;

            info.HasDieScript = true;

            return true;
        }

        internal bool ApplyAutoTime(ScriptAutoMarkerLine line)
        {
            Robot.AddRobot(line.Line.ToUpperInvariant());

            return true;
        }
    }
}
