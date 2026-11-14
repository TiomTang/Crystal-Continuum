using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        protected static MessageQueue MessageQueue
        {
            get { return MessageQueue.Instance; }
        }

        public static NPCScript Get(int index)
        {
            return Envir.Scripts[index];
        }

        public static NPCScript GetOrAdd(uint loadedObjectID, string fileName, NPCScriptType type)
        {
            var script = Envir.Scripts.SingleOrDefault(x => x.Value.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) && x.Value.LoadedObjectID == loadedObjectID).Value;

            if (script != null)
            {
                return script;
            }

            return new NPCScript(loadedObjectID, fileName, type);
        }

        public readonly int ScriptID;
        public readonly uint LoadedObjectID;
        public readonly NPCScriptType Type;
        public readonly string FileName;

        public const string
            MainKey = "[@MAIN]",
            BuyKey = "[@BUY]",
            SellKey = "[@SELL]",
            BuySellKey = "[@BUYSELL]",
            RepairKey = "[@REPAIR]",
            SRepairKey = "[@SREPAIR]",
            RefineKey = "[@REFINE]",
            RefineCheckKey = "[@REFINECHECK]",
            RefineCollectKey = "[@REFINECOLLECT]",
            ReplaceWedRingKey = "[@REPLACEWEDDINGRING]",
            BuyBackKey = "[@BUYBACK]",
            StorageKey = "[@STORAGE]",
            ConsignKey = "[@CONSIGN]",
            MarketKey = "[@MARKET]",
            CraftKey = "[@CRAFT]",

            GuildCreateKey = "[@CREATEGUILD]",
            RequestWarKey = "[@REQUESTWAR]",
            SendParcelKey = "[@SENDPARCEL]",
            CollectParcelKey = "[@COLLECTPARCEL]",
            AwakeningKey = "[@AWAKENING]",
            DisassembleKey = "[@DISASSEMBLE]",
            DowngradeKey = "[@DOWNGRADE]",
            ResetKey = "[@RESET]",
            PearlBuyKey = "[@PEARLBUY]",
            BuyUsedKey = "[@BUYUSED]",
            BuyNewKey = "[@BUYNEW]",
            BuySellNewKey = "[@BUYSELLNEW]",
            HeroCreateKey = "[@CREATEHERO]",
            HeroManageKey = "[@MANAGEHERO]",

            TradeKey = "[TRADE]",
            RecipeKey = "[RECIPE]",
            TypeKey = "[TYPES]",
            UsedTypeKey = "[USEDTYPES]",
            QuestKey = "[QUESTS]",
            SpeechKey = "[SPEECH]",
            GuildTerritoryKey = "[@GUILDTERRITORY]";


        public List<ItemType> Types = new List<ItemType>();
        public List<ItemType> UsedTypes = new List<ItemType>();
        public List<UserItem> Goods = new List<UserItem>();
        public List<RecipeInfo> CraftGoods = new List<RecipeInfo>();

        public List<NPCPage> NPCSections = new List<NPCPage>();
        public List<NPCPage> NPCPages = new List<NPCPage>();

        private NPCScript(uint loadedObjectID, string fileName, NPCScriptType type)
        {
            ScriptID = ++Envir.ScriptIndex;

            LoadedObjectID = loadedObjectID;
            FileName = fileName;
            Type = type;

            Load();

            Envir.Scripts.Add(ScriptID, this);
        }

        public void Load()
        {
            LoadInfo();
            LoadGoods();
        }

        public float PriceRate(PlayerObject player, bool baseRate = false)
        {
            var callingNPC = Envir.NPCs.SingleOrDefault(x => x.ObjectID == player.NPCObjectID);

            if (callingNPC == null)
            {
                return 1F;
            }

            if (callingNPC.Conq == null || baseRate)
            {
                return callingNPC.Info.Rate / 100F;
            }

            if (player.MyGuild != null && player.MyGuild.Guildindex == callingNPC.Conq.GuildInfo.Owner)
            {
                return callingNPC.Info.Rate / 100F;
            }
            else
            {
                return (((callingNPC.Info.Rate / 100F) * callingNPC.Conq.GuildInfo.NPCRate) + callingNPC.Info.Rate) / 100F;
            }
        }


        public void LoadInfo()
        {
            ClearInfo();

            if (!Directory.Exists(Settings.NPCPath)) return;

            string fileName = Path.Combine(Settings.NPCPath, FileName + ".txt");

            if (File.Exists(fileName))
            {
                ScriptParseScope.Set(FileName + ".txt");

                List<string> lines = File.ReadAllLines(fileName).ToList();

                lines = ParseInsert(lines);
                lines = ParseInclude(lines);

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
                if (!lines[i].ToUpper().StartsWith("[@_")) continue;

                if (Type == NPCScriptType.AutoPlayer)
                {
                    if (lines[i].ToUpper().Contains("MAPCOORD"))
                    {
                        Regex regex = new Regex(@"\((.*?),([0-9]{1,3}),([0-9]{1,3})\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;

                        Map map = Envir.MapList.FirstOrDefault(m => m.Info.FileName == match.Groups[1].Value);

                        if (map == null) continue;

                        Point point = new Point(Convert.ToInt16(match.Groups[2].Value), Convert.ToInt16(match.Groups[3].Value));

                        if (!map.Info.ActiveCoords.Contains(point))
                        {
                            map.Info.ActiveCoords.Add(point);
                        }
                    }

                    if (lines[i].ToUpper().Contains("CUSTOMCOMMAND"))
                    {
                        Regex regex = new Regex(@"\((.*?)\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;

                        Envir.CustomCommands.Add(match.Groups[1].Value);
                    }
                }
                else if (Type == NPCScriptType.AutoMonster)
                {
                    MonsterInfo MobInfo;
                    if (lines[i].ToUpper().Contains("SPAWN"))
                    {
                        Regex regex = new Regex(@"\((.*?)\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;
                        MobInfo = Envir.GetMonsterInfo(Convert.ToInt16(match.Groups[1].Value));
                        if (MobInfo == null) continue;
                        MobInfo.HasSpawnScript = true;
                    }
                    if (lines[i].ToUpper().Contains("DIE"))
                    {
                        Regex regex = new Regex(@"\((.*?)\)");
                        Match match = regex.Match(lines[i]);

                        if (!match.Success) continue;
                        MobInfo = Envir.GetMonsterInfo(Convert.ToInt16(match.Groups[1].Value));
                        if (MobInfo == null) continue;
                        MobInfo.HasDieScript = true;
                    }
                }
                else if (Type == NPCScriptType.Robot)
                {
                    if (lines[i].ToUpper().Contains("TIME"))
                    {
                        Robot.AddRobot(lines[i].ToUpper());
                    }
                }

                NPCPages.AddRange(ParsePages(lines, lines[i]));
            }
        }

        private void ParseScript(IList<string> lines)
        {
            NPCPages.AddRange(ParsePages(lines));

            ParseGoods(lines);
            ParseTypes(lines);
            ParseQuests(lines);
            ParseCrafting(lines);
            ParseSpeech(lines);
        }

        private List<string> ParseInsert(List<string> lines)
        {
            List<string> newLines = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!ScriptDirectiveCatalog.Is(lines[i], ScriptDirectiveKind.Insert)) continue;

                string[] split = lines[i].Split(' ');

                if (split.Length < 2) continue;

                string path = Path.Combine(Settings.EnvirPath, split[1].Substring(1, split[1].Length - 2));

                if (!File.Exists(path))
                    MessageQueue.Enqueue(GameLanguage.Get("INSERT Script Not Found: {0}", path));
                else
                    newLines = File.ReadAllLines(path).ToList();

                lines.AddRange(newLines);
            }

            lines.RemoveAll(str => ScriptDirectiveCatalog.Is(str, ScriptDirectiveKind.Insert));

            return lines;
        }

        private List<string> ParseInclude(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!ScriptDirectiveCatalog.Is(lines[i], ScriptDirectiveKind.Include)) continue;

                string[] split = lines[i].Split(' ');

                string path = Path.Combine(Settings.EnvirPath, split[1].Substring(1, split[1].Length - 2));
                string page = ("[" + split[2] + "]").ToUpper();

                bool start = false, finish = false;

                var parsedLines = new List<string>();

                if (!File.Exists(path))
                {
                    MessageQueue.Enqueue(GameLanguage.Get("INCLUDE Script Not Found: {0}", path));
                    return parsedLines;
                }

                IList<string> extLines = File.ReadAllLines(path);

                for (int j = 0; j < extLines.Count; j++)
                {
                    if (!extLines[j].ToUpper().StartsWith(page)) continue;

                    for (int x = j + 1; x < extLines.Count; x++)
                    {
                        if (extLines[x].Trim() == ("{"))
                        {
                            start = true;
                            continue;
                        }

                        if (extLines[x].Trim() == ("}"))
                        {
                            finish = true;
                            break;
                        }

                        parsedLines.Add(extLines[x]);
                    }
                }

                if (start && finish)
                {
                    lines.InsertRange(i + 1, parsedLines);
                    parsedLines.Clear();
                }
            }

            lines.RemoveAll(str => ScriptDirectiveCatalog.Is(str, ScriptDirectiveKind.Include));

            return lines;
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

                if (line.StartsWith(";")) continue;

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

                    if (nextLine.StartsWith("[") && nextLine.EndsWith("]"))
                    {
                        nextPage = true;
                    }

                    else if (ScriptBlockCatalog.Is(nextLine, ScriptBlockKind.Check))
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
            List<string> currentSay = say, currentButtons = buttons;

            Regex regex = new Regex(@"<.*?/(\@.*?)>");

            for (int i = 0; i < lines.Count; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                if (lines[i].StartsWith(";")) continue;

                if (lines[i].StartsWith("#"))
                {
                    ScriptBlockKind kind;

                    if (!ScriptBlockCatalog.TryParse(lines[i], out kind)) throw new NotImplementedException();

                    switch (kind)
                    {
                        case ScriptBlockKind.Check:
                            currentSay = checks;
                            currentButtons = null;
                            continue;
                        case ScriptBlockKind.Say:
                            currentSay = say;
                            currentButtons = buttons;
                            continue;
                        case ScriptBlockKind.Act:
                            currentSay = acts;
                            currentButtons = gotoButtons;
                            continue;
                        case ScriptBlockKind.ElseSay:
                            currentSay = elseSay;
                            currentButtons = elseButtons;
                            continue;
                        case ScriptBlockKind.ElseAct:
                            currentSay = elseActs;
                            currentButtons = gotoButtons;
                            continue;
                        default:
                            throw new NotImplementedException();
                    }
                }

                if (lines[i].StartsWith("[") && lines[i].EndsWith("]")) break;

                if (currentButtons != null)
                {
                    Match match = regex.Match(lines[i]);

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

        private void ParseTypes(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(TypeKey)) continue;

                while (++i < lines.Count)
                {
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    if (!int.TryParse(lines[i], out int index)) break;
                    Types.Add((ItemType)index);
                }
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(UsedTypeKey)) continue;

                while (++i < lines.Count)
                {
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    if (!int.TryParse(lines[i], out int index)) break;
                    UsedTypes.Add((ItemType)index);
                }
            }
        }
        private void ParseGoods(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(TradeKey)) continue;

                while (++i < lines.Count)
                {
                    if (lines[i].StartsWith("[")) return;
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    var data = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    ItemInfo info = Envir.GetItemInfo(data[0]);
                    if (info == null)
                        continue;

                    UserItem goods = Envir.CreateShopItem(info, (uint)i);

                    if (goods == null || Goods.Contains(goods))
                    {
                        MessageQueue.Enqueue(GameLanguage.Get("Could not find Item: {0}, File: {1}", lines[i], FileName));
                        continue;
                    }

                    ushort count = 1;
                    if (data.Length == 2)
                        ushort.TryParse(data[1], out count);

                    goods.Count = count;

                    Goods.Add(goods);
                }
            }
        }
        private void ParseQuests(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(QuestKey)) continue;

                var loadedNPC = NPCObject.Get(LoadedObjectID);

                if (loadedNPC == null)
                {
                    return;
                }

                while (++i < lines.Count)
                {
                    if (lines[i].StartsWith("[")) return;
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    int.TryParse(lines[i], out int index);

                    if (index == 0) continue;

                    QuestInfo info = Envir.GetQuestInfo(Math.Abs(index));

                    if (info == null) return;

                    if (index > 0)
                        info.NpcIndex = LoadedObjectID;
                    else
                        info.FinishNpcIndex = LoadedObjectID;

                    if (loadedNPC.Quests.All(x => x != info))
                        loadedNPC.Quests.Add(info);

                }
            }
        }


        private void ParseSpeech(IList<string> lines)
        {
            var loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null)
            {
                return;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(SpeechKey)) continue;

                while (++i < lines.Count)
                {
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    var parts = lines[i].Split(' ');

                    if (parts.Length < 2 || !int.TryParse(parts[0], out int weight)) return;

                    loadedNPC.Speech.Add(new NPCSpeech { Weight = weight, Message = lines[i].Substring(parts[0].Length + 1) });
                }
            }
        }
        private void ParseCrafting(IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].ToUpper().StartsWith(RecipeKey)) continue;

                while (++i < lines.Count)
                {
                    if (lines[i].StartsWith("[")) return;
                    if (String.IsNullOrEmpty(lines[i])) continue;

                    var data = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    ItemInfo info = Envir.GetItemInfo(data[0]);
                    if (info == null)
                        continue;

                    RecipeInfo recipe = Envir.RecipeInfoList.SingleOrDefault(x => x.MatchItem(info.Index));

                    if (recipe == null)
                    {
                        MessageQueue.Enqueue(GameLanguage.Get("Could not find recipe: {0}, File: {1}", lines[i], FileName));
                        continue;
                    }

                    if (recipe.Ingredients.Count == 0)
                    {
                        MessageQueue.Enqueue(GameLanguage.Get("Could not find ingredients: {0}, File: {1}", lines[i], FileName));
                        continue;
                    }

                    CraftGoods.Add(recipe);
                }
            }
        }

        public void Call(MonsterObject monster, string key)
        {
            key = key.ToUpper();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    ProcessSegment(monster, page, segment);
                }
            }
        }
        public void Call(string key)
        {
            key = key.ToUpper();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    ProcessSegment(page, segment);
                }
            }
        }
        public void Call(PlayerObject player, uint objectID, string key)
        {
            key = key.ToUpper();

            if (!player.NPCDelayed)
            {
                if (key != MainKey)
                {
                    if (player.NPCObjectID != objectID) return;

                    bool found = false;

                    foreach (NPCSegment segment in player.NPCPage.SegmentList)
                    {
                        if (!player.NPCSuccess.TryGetValue(segment, out bool result)) break; //no result for segment ?

                        if ((result ? segment.Buttons : segment.ElseButtons).Any(s => s.ToUpper() == key))
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        var npc = NPCObject.Get(objectID);
                        string npcName = npc?.Info?.GameName ?? npc?.Name ?? "Unknown";
                        MessageQueue.Enqueue(GameLanguage.Get("Player: {0} was prevented access to NPC key: '{1}' on NPC: {2}", player.Name, key, npcName));
                        return;
                    }
                }
            }
            else
            {
                player.NPCDelayed = false;
            }

            if (key.StartsWith("[@@") && !player.NPCData.TryGetValue("NPCInputStr", out object _npcInputStr))
            {
                //send off packet to request input
                player.Enqueue(new S.NPCRequestInput { NPCID = player.NPCObjectID, PageName = key });
                return;
            }

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                player.NPCSpeech = new List<string>();
                player.NPCSuccess.Clear();
                player.NPCDialogClose = false;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    if (player.NPCDialogClose) break;

                    ProcessSegment(player, page, segment, objectID);
                }

                Response(player, page);
            }

            player.NPCData.Remove("NPCInputStr");
        }

        private void Response(PlayerObject player, NPCPage page)
        {
            // A CLOSE action ends the page without dialogue: the client hides the
            // window when it receives an empty page.
            if (player.NPCDialogClose)
            {
                player.NPCDialogClose = false;
                player.NPCSpeech.Clear();

                player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });

                return;
            }

            player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });

            ProcessSpecial(player, page);
        }

        private void ProcessSegment(PlayerObject player, NPCPage page, NPCSegment segment, uint objectID)
        {
            player.NPCObjectID = objectID;
            player.NPCScriptID = ScriptID;
            player.NPCSuccess.Add(segment, segment.Check(player));
            player.NPCPage = page;
        }

        private void ProcessSegment(MonsterObject monster, NPCPage page, NPCSegment segment)
        {
            segment.Check(monster);
        }
        private void ProcessSegment(NPCPage page, NPCSegment segment)
        {
            segment.Check();
        }

        private void ProcessSpecial(PlayerObject player, NPCPage page)
        {
            ScriptSpecialPageEntry special = ScriptSpecialPageCatalog.Find(page.Key);

            if (special?.Executor == null) return;

            special.Executor(this, player, page);
        }

        public void Buy(PlayerObject player, ulong index, ushort count)
        {
            UserItem goods = null;

            for (int i = 0; i < Goods.Count; i++)
            {
                if (Goods[i].UniqueID != index) continue;
                goods = Goods[i];
                break;
            }

            bool isUsed = false;
            bool isBuyBack = false;

            var callingNPC = NPCObject.Get(player.NPCObjectID);

            if (callingNPC != null)
            {
                if (goods == null)
                {
                    for (int i = 0; i < callingNPC.UsedGoods.Count; i++)
                    {
                        if (callingNPC.UsedGoods[i].UniqueID != index) continue;
                        goods = callingNPC.UsedGoods[i];
                        isUsed = true;
                        break;
                    }
                }

                if (goods == null)
                {
                    if (!callingNPC.BuyBack.ContainsKey(player.Name)) callingNPC.BuyBack[player.Name] = new List<UserItem>();
                    for (int i = 0; i < callingNPC.BuyBack[player.Name].Count; i++)
                    {
                        if (callingNPC.BuyBack[player.Name][i].UniqueID != index) continue;
                        goods = callingNPC.BuyBack[player.Name][i];
                        isBuyBack = true;
                        break;
                    }
                }
            }

            if (goods == null || count == 0 || count > goods.Info.StackSize) return;

            if (isBuyBack || isUsed)
                count = Math.Min(count, goods.Count);
            if (count == 0) return;
            if ((isBuyBack || isUsed) && count < goods.Count && goods.Slots.Any(slot => slot != null)) return;

            // Price a copy so an unsuccessful purchase cannot change shared stock.
            UserItem pricedItem = goods.Clone();
            pricedItem.Count = count;

            double totalCost = pricedItem.Price() * (double)PriceRate(player);
            if (totalCost < 0 || totalCost > uint.MaxValue || double.IsNaN(totalCost)) return;
            uint cost = (uint)totalCost;
            uint baseCost = (uint)Math.Clamp(pricedItem.Price() * (double)PriceRate(player, true), 0, uint.MaxValue);

            if (player.NPCPage.Key.ToUpper() == PearlBuyKey)//pearl currency
            {
                if (cost > player.Info.PearlCount) return;
            }
            else if (cost > player.Account.Gold) return;

            UserItem item = (isBuyBack || isUsed) ? pricedItem : Envir.CreateFreshItem(goods.Info);
            if ((isBuyBack || isUsed) && count < goods.Count)
                item.UniqueID = Envir.CreateFreshItem(goods.Info).UniqueID;
            item.Count = count;

            if (!player.CanGainItem(item)) return;

            if (player.NPCPage.Key.ToUpper() == PearlBuyKey)
            {
                player.IntelligentCreatureLosePearls((int)cost);
            }
            else
            {
                player.Account.Gold -= cost;
                player.Enqueue(new S.LoseGold { Gold = cost });

                if (callingNPC != null && callingNPC.Conq != null)
                {
                    callingNPC.Conq.GuildInfo.GoldStorage += (cost - baseCost);
                }
            }

            player.GainItem(item);

            if (isUsed)
            {
                goods.Count -= count;
                if (goods.Count == 0) callingNPC.UsedGoods.Remove(goods);

                List<UserItem> newGoodsList = new List<UserItem>();
                newGoodsList.AddRange(Goods);
                newGoodsList.AddRange(callingNPC.UsedGoods);

                callingNPC.NeedSave = true;

                player.SendNPCGoods(newGoodsList, PriceRate(player), player.NPCPage.Key.ToUpper() == BuyUsedKey ? PanelType.BuySub : PanelType.Buy, Settings.GoodsHideAddedStats);
            }

            if (isBuyBack)
            {
                goods.Count -= count;
                if (goods.Count == 0) callingNPC.BuyBack[player.Name].Remove(goods);
                player.SendNPCGoods(callingNPC.BuyBack[player.Name], PriceRate(player), PanelType.Buy);
            }
        }
        public void Sell(PlayerObject player, UserItem item)
        {
            /* Handle Item Sale */
        }
        public void Craft(PlayerObject player, ulong index, ushort count, int[] slots)
        {
            S.CraftItem p = new S.CraftItem();

            RecipeInfo recipe = null;

            for (int i = 0; i < CraftGoods.Count; i++)
            {
                if (CraftGoods[i].Item.UniqueID != index) continue;
                recipe = CraftGoods[i];
                break;
            }

            if (recipe == null || !recipe.CanCraft(player) || slots == null || slots.Length > player.Info.Inventory.Length ||
                slots.Any(slot => slot < 0 || slot >= player.Info.Inventory.Length) ||
                slots.Distinct().Count() != slots.Length)
            {
                player.Enqueue(p);
                return;
            }

            UserItem goods = recipe.Item;

            if (goods == null || goods.Count == 0 || count == 0 || count > goods.Info.StackSize)
            {
                player.Enqueue(p);
                return;
            }

            ulong totalCost = (ulong)recipe.Gold * count;
            if (totalCost > player.Account.Gold)
            {
                player.Enqueue(p);
                return;
            }

            bool hasItems = true;

            List<int> usedSlots = new List<int>();

            //Check Tools
            foreach (var tool in recipe.Tools)
            {
                bool found = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != tool.Info) continue;

                    usedSlots.Add(slot);

                    if ((uint)Math.Floor(item.CurrentDura / 1000M) < count)
                    {
                        hasItems = false;
                        break;
                    }

                    found = true;
                    break;
                }

                if (!found) hasItems = false;
                if (!hasItems)
                {
                    break;
                }
            }

            //Check Ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Count * count > ingredient.Info.StackSize)
                {
                    player.Enqueue(p);
                    return;
                }

                ushort amount = (ushort)(ingredient.Count * count);

                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != ingredient.Info || item.RentalInformation != null) continue;

                    usedSlots.Add(slot);

                    if (ingredient.CurrentDura < ingredient.MaxDura && ingredient.CurrentDura > item.CurrentDura)
                    {
                        hasItems = false;
                        break;
                    }

                    if (amount > item.Count)
                    {
                        hasItems = false;
                        break;
                    }

                    amount = 0;
                    break;
                }

                if (amount > 0)
                {
                    hasItems = false;
                    break;
                }
            }

            if (!hasItems || usedSlots.Count != (recipe.Tools.Count + recipe.Ingredients.Count))
            {
                player.Enqueue(p);
                return;
            }

            if (count > (goods.Info.StackSize / goods.Count) || count < 1)
            {
                player.Enqueue(p);
                return;
            }

            UserItem craftedItem = Envir.CreateFreshItem(goods.Info);
            craftedItem.Count = (ushort)(goods.Count * count);

            if (!player.CanGainItem(craftedItem))
            {
                player.Enqueue(p);
                return;
            }

            List<int> usedSlots2 = new List<int>();

            //Use Tool Durability
            foreach (var tool in recipe.Tools)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots2.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != tool.Info) continue;

                    usedSlots2.Add(slot);

                    player.DamageItem(item, (int)(count * 1000), true);

                    break;
                }
            }

            //Take Ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                ushort amount = (ushort)(ingredient.Count * count);

                for (int i = 0; i < slots.Length; i++)
                {
                    int slot = slots[i];

                    if (usedSlots2.Contains(slot)) continue;

                    if (slot < 0 || slot >= player.Info.Inventory.Length) continue;

                    UserItem item = player.Info.Inventory[slot];

                    if (item == null || item.Info != ingredient.Info || item.RentalInformation != null) continue;

                    usedSlots2.Add(slot);

                    if (item.Count > amount)
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = amount });
                        player.Info.Inventory[slot].Count -= amount;
                        break;
                    }
                    else
                    {
                        player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        amount -= item.Count;
                        player.Info.Inventory[slot] = null;
                    }

                    break;
                }
            }

            //Take Gold
            player.Account.Gold -= (uint)totalCost;
            player.Enqueue(new S.LoseGold { Gold = (uint)totalCost });

            if (Envir.Random.Next(100) >= recipe.Chance + player.Stats[Stat.CraftRatePercent])
            {
                player.ReceiveChat(GameLanguage.Get("Crafting attempt failed."), ChatType.System);
            }
            else
            {
                //Give Item
                player.GainItem(craftedItem);
            }

            p.Success = true;
            player.Enqueue(p);
        }
    }

    public enum NPCScriptType
    {
        Normal,
        Called,
        AutoPlayer,
        AutoMonster,
        Robot
    }
}
