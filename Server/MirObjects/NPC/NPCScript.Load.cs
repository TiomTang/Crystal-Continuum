using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        public void LoadInfo()
        {
            ClearInfo();

            if (!Directory.Exists(Settings.NPCPath)) return;

            string fileName = Path.Combine(Settings.NPCPath, FileName + ".txt");

            if (File.Exists(fileName))
            {
                ScriptParseScope.Set(FileName + ".txt");

                List<string> lines = File.ReadAllLines(fileName).ToList();

                foreach (ScriptDirectiveEntry directive in ScriptDirectiveCatalog.Ordered)
                    directive.Apply(lines);

                switch (Type)
                {
                    case NPCScriptType.Normal:
                    default:
                        ParseScript(lines);
                        break;
                    case NPCScriptType.AutoPlayer:
                    case NPCScriptType.AutoMonster:
                    case NPCScriptType.Robot:
                        ParseDefault(lines);
                        break;
                }

                ScriptParseScope.Set(null);
            }
            else
                MessageQueue.Enqueue(GameLanguage.Get("Script Not Found: {0}", FileName));
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

            if (loadedNPC != null)
            {
                loadedNPC.UsedGoods.Clear();

                string path = Path.Combine(Settings.GoodsPath, loadedNPC.Info.Index.ToString() + ".msd");

                if (!File.Exists(path)) return;

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

                        for (int k = 0; k < count; k++)
                        {
                            UserItem item = new UserItem(reader, version, customversion);
                            if (Envir.BindItem(item))
                                loadedNPC.UsedGoods.Add(item);
                        }
                    }
                }
            }
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

        private void ParseScript(IList<string> lines)
        {
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

        private List<NPCPage> ParsePages(IList<string> lines, string key = MainKey)
        {
            List<NPCPage> pages = new List<NPCPage>();
            List<string> buttons = new List<string>();

            NPCPage page = ParsePage(lines, key);
            pages.Add(page);

            buttons.AddRange(page.Buttons);

            for (int i = 0; i < buttons.Count; i++)
            {
                string section = buttons[i];

                bool match = pages.Any(t => t.Key.ToUpper() == section.ToUpper());

                if (match) continue;

                page = ParsePage(lines, section);
                buttons.AddRange(page.Buttons);

                pages.Add(page);
            }

            return pages;
        }

        private NPCPage ParsePage(IList<string> scriptLines, string sectionName)
        {
            bool nextPage = false, nextSection = false;

            List<string> lines = scriptLines.Where(x => !string.IsNullOrEmpty(x)).ToList();

            NPCPage Page = new NPCPage(sectionName);

            //Cleans arguments out of search page name
            string tempSectionName = Page.ArgumentParse(sectionName);

            //parse all individual pages in a script, defined by sectionName
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (ScriptLexer.IsComment(line)) continue;

                if (!lines[i].ToUpper().StartsWith(tempSectionName.ToUpper())) continue;

                List<string> segmentLines = new List<string>();

                nextPage = false;

                //Found a page, now process that page and split it into segments
                for (int j = i + 1; j < lines.Count; j++)
                {
                    string nextLine = lines[j];

                    if (j < lines.Count - 1)
                        nextLine = lines[j + 1];
                    else
                        nextLine = "";

                    if (ScriptPageKeyCatalog.TryParseHeader(nextLine, out string _))
                    {
                        nextPage = true;
                    }

                    else if (ScriptBlockCatalog.SplitsSegment(nextLine))
                    {
                        nextSection = true;
                    }

                    if (nextSection || nextPage)
                    {
                    segmentLines.Add(lines[j]);

                        //end of segment, so need to parse it and put into the segment list within the page
                        if (segmentLines.Count > 0)
                        {
                            NPCSegment segment = ParseSegment(Page, segmentLines);

                            List<string> currentButtons = new List<string>();
                            currentButtons.AddRange(segment.Buttons);
                            currentButtons.AddRange(segment.ElseButtons);
                            currentButtons.AddRange(segment.GotoButtons);

                            Page.Buttons.AddRange(currentButtons);
                            Page.SegmentList.Add(segment);
                            segmentLines.Clear();

                            nextSection = false;
                        }

                        if (nextPage) break;

                        continue;
                    }

                    segmentLines.Add(lines[j]);
                }

                //bottom of script reached, add all lines found to new segment
                if (segmentLines.Count > 0)
                {
                    NPCSegment segment = ParseSegment(Page, segmentLines);

                    List<string> currentButtons = new List<string>();
                    currentButtons.AddRange(segment.Buttons);
                    currentButtons.AddRange(segment.ElseButtons);
                    currentButtons.AddRange(segment.GotoButtons);

                    Page.Buttons.AddRange(currentButtons);
                    Page.SegmentList.Add(segment);
                    segmentLines.Clear();
                }                

                return Page;
            }

            return Page;
        }

        private NPCSegment ParseSegment(NPCPage page, IEnumerable<string> scriptLines)
        {
            List<string>
                checks = new List<string>(),
                acts = new List<string>(),
                say = new List<string>(),
                buttons = new List<string>(),
                elseSay = new List<string>(),
                elseActs = new List<string>(),
                elseButtons = new List<string>(),
                gotoButtons = new List<string>();

            List<string> lines = scriptLines.ToList();

            // Tag to destination. The segment asks which role the tag carries, not which tag it was,
            // so a new block type that reuses a role needs no code here.
            var roleText = new Dictionary<ScriptBlockRole, List<string>>
            {
                { ScriptBlockRole.Check, checks },
                { ScriptBlockRole.Say, say },
                { ScriptBlockRole.Act, acts },
                { ScriptBlockRole.ElseSay, elseSay },
                { ScriptBlockRole.ElseAct, elseActs },
            };

            var roleButtons = new Dictionary<ScriptBlockRole, List<string>>
            {
                { ScriptBlockRole.Say, buttons },
                { ScriptBlockRole.Act, gotoButtons },
                { ScriptBlockRole.ElseSay, elseButtons },
                { ScriptBlockRole.ElseAct, gotoButtons },
            };

            List<string> currentSay = say, currentButtons = buttons;

            for (int i = 0; i < lines.Count; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                if (ScriptLexer.IsComment(lines[i])) continue;

                if (lines[i].TrimStart().StartsWith("#"))
                {
                    ScriptBlockKind kind;
                    ScriptBlockEntry block = ScriptBlockCatalog.TryFind(lines[i], out kind);

                    if (block == null)
                    {
                        ReportUnknownBlock(page, lines[i]);
                        continue;
                    }

                    currentSay = roleText[block.Role];

                    List<string> blockButtons;
                    currentButtons = roleButtons.TryGetValue(block.Role, out blockButtons) ? blockButtons : null;

                    continue;
                }

                if (ScriptPageKeyCatalog.TryParseHeader(lines[i], out _)) break;

                if (currentButtons != null)
                {
                    Match match = ScriptLexer.Button.Match(lines[i]);

                    while (match.Success)
                    {
                        string argu = match.Groups[1].Captures[0].Value;
                        argu = argu.Split('/')[0];

                        currentButtons.Add(string.Format("[{0}]", argu));
                        match = match.NextMatch();
                    }

                    //Check if line has a goto command
                    var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    ScriptKeywordEntry action = parts.Length > 1 ? ScriptActionCatalog.Find(parts[0]) : null;

                    if (action != null && action.Button != ScriptButtonKind.None)
                    {
                        int target = action.Button == ScriptButtonKind.DelayedPage ? 2 : 1;

                        if (parts.Length > target)
                        {
                            string button = string.Format("[{0}]", parts[target].ToUpper());

                            if (action.Button == ScriptButtonKind.Roll) buttons.Add(button);
                            else gotoButtons.Add(button);
                        }
                    }
                }

                currentSay.Add(lines[i].TrimEnd());
            }

            NPCSegment segment = new NPCSegment(page, say, buttons, elseSay, elseButtons, gotoButtons);

            for (int i = 0; i < checks.Count; i++)
                segment.ParseCheck(checks[i]);

            for (int i = 0; i < acts.Count; i++)
                segment.ParseAct(segment.ActList, acts[i]);

            for (int i = 0; i < elseActs.Count; i++)
                segment.ParseAct(segment.ElseActList, elseActs[i]);

            currentButtons = new List<string>();
            currentButtons.AddRange(buttons);
            currentButtons.AddRange(elseButtons);
            currentButtons.AddRange(gotoButtons);

            return segment;
        }

        /// <summary>
        /// A line that looks like a block tag but is not one. The segment keeps the block it was already
        /// in, so a misspelled tag costs one line instead of aborting the whole script.
        /// </summary>
        private static void ReportUnknownBlock(NPCPage page, string line)
        {
            string text = (line ?? string.Empty).Trim();
            int end = text.IndexOfAny(new[] { ' ', '\t' });
            string tag = end >= 0 ? text.Substring(0, end) : text;

            ScriptDiagnostics.Report(ScriptDiagnosticKind.UnknownBlock, new ScriptParseContext
            {
                Line = line,
                FileName = ScriptParseScope.FileName,
                PageKey = page == null ? string.Empty : page.Key,
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

                if (info == null) return;

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
            if (line.Args.Length != 3) return false;

            int x, y;

            if (!int.TryParse(line.Args[1], out x) || !int.TryParse(line.Args[2], out y)) return false;

            Map map = Envir.MapList.FirstOrDefault(m => m.Info.FileName == line.Args[0]);

            if (map == null) return false;

            Point point = new Point((short)x, (short)y);

            if (!map.Info.ActiveCoords.Contains(point)) map.Info.ActiveCoords.Add(point);

            return true;
        }

        internal bool ApplyAutoCustomCommand(ScriptAutoMarkerLine line)
        {
            if (string.IsNullOrEmpty(line.Body)) return false;

            Envir.CustomCommands.Add(line.Body);

            return true;
        }

        internal bool ApplyAutoSpawn(ScriptAutoMarkerLine line)
        {
            int index;

            if (!int.TryParse(line.Body, out index)) return false;

            MonsterInfo info = Envir.GetMonsterInfo((short)index);

            if (info == null) return false;

            info.HasSpawnScript = true;

            return true;
        }

        internal bool ApplyAutoDie(ScriptAutoMarkerLine line)
        {
            int index;

            if (!int.TryParse(line.Body, out index)) return false;

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
