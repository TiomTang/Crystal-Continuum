using Server.MirObjects;
using Server.Scripting;
using Server.MirEnvir;
using Shared.Helpers;

namespace Server.MirDatabase
{
    public class QuestInfo
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        protected static Envir EditEnvir
        {
            get { return Envir.Edit; }
        }

        protected static MessageQueue MessageQueue
        {
            get { return MessageQueue.Instance; }
        }

        public int Index;

        public uint NpcIndex;
        public NPCInfo NpcInfo;

        private uint _finishNpcIndex;

        public uint FinishNpcIndex
        {
            get { return _finishNpcIndex == 0 ? NpcIndex : _finishNpcIndex; }
            set { _finishNpcIndex = value; }
        }

        public NPCObject FinishNPC
        {
            get
            {
                return Envir.NPCs.Single(x => x.ObjectID == FinishNpcIndex);
            }
        }

        public string
            Name = string.Empty,
            Group = string.Empty,
            FileName = string.Empty,
            GotoMessage = string.Empty,
            KillMessage = string.Empty,
            ItemMessage = string.Empty,
            FlagMessage = string.Empty;

        public List<string> Description = new List<string>();
        public List<string> TaskDescription = new List<string>();
        public List<string> ReturnDescription = new List<string>();
        public List<string> CompletionDescription = new List<string>();

        public int RequiredMinLevel, RequiredMaxLevel, RequiredQuest;
        public RequiredClass RequiredClass = RequiredClass.None;

        public QuestType Type;

        public int TimeLimitInSeconds = 0;

        public List<QuestItemTask> CarryItems = new List<QuestItemTask>();

        public List<QuestKillTask> KillTasks = new List<QuestKillTask>();
        public List<QuestItemTask> ItemTasks = new List<QuestItemTask>();
        public List<QuestFlagTask> FlagTasks = new List<QuestFlagTask>();
        //TODO: ZoneTasks
        //TODO: EscortTasks

        public uint GoldReward;
        public uint ExpReward;
        public uint CreditReward;
        public List<QuestItemReward> FixedRewards = new List<QuestItemReward>();
        public List<QuestItemReward> SelectRewards = new List<QuestItemReward>();

        public QuestInfo() { }

        public QuestInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Name = reader.ReadString();
            Group = reader.ReadString();
            FileName = reader.ReadString();
            RequiredMinLevel = reader.ReadInt32();

            RequiredMaxLevel = reader.ReadInt32();
            if (RequiredMaxLevel == 0) RequiredMaxLevel = ushort.MaxValue;

            RequiredQuest = reader.ReadInt32();
            RequiredClass = (RequiredClass)reader.ReadByte();
            Type = (QuestType)reader.ReadByte();
            GotoMessage = reader.ReadString();
            KillMessage = reader.ReadString();
            ItemMessage = reader.ReadString();
            FlagMessage = reader.ReadString();

            if (Envir.LoadVersion > 90)
            {
                TimeLimitInSeconds = reader.ReadInt32();
            }

            LoadInfo();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Name);
            writer.Write(Group);
            writer.Write(FileName);
            writer.Write(RequiredMinLevel);
            writer.Write(RequiredMaxLevel);
            writer.Write(RequiredQuest);
            writer.Write((byte)RequiredClass);
            writer.Write((byte)Type);
            writer.Write(GotoMessage);
            writer.Write(KillMessage);
            writer.Write(ItemMessage);
            writer.Write(FlagMessage);
            writer.Write(TimeLimitInSeconds);
        }

        public void LoadInfo(bool clear = false)
        {
            if (clear) ClearInfo();

            if (!Directory.Exists(Settings.QuestPath)) return;

            string fileName = Path.Combine(Settings.QuestPath, FileName + ".txt");

            if (File.Exists(fileName))
            {
                List<string> lines = File.ReadAllLines(fileName).ToList();

                ParseFile(lines);
            }
            else
                MessageQueue.Enqueue(GameLanguage.Get("File Not Found: {0}, Quest: {1}", fileName, Name));
        }

        public void ClearInfo()
        {
            Description.Clear();
            KillTasks = new List<QuestKillTask>();
            ItemTasks = new List<QuestItemTask>();
            FlagTasks = new List<QuestFlagTask>();
            FixedRewards = new List<QuestItemReward>();
            SelectRewards = new List<QuestItemReward>();
            ExpReward = 0;
            GoldReward = 0;
            CreditReward = 0;
        }

        public void ParseFile(List<string> lines)
        {
            // Section order is the catalog's order, which is the order the runtime has always applied:
            // section by section, every occurrence of a section appended in file order.
            QuestScriptDocument document = QuestScriptParser.ParseLines(lines, FileName);

            foreach (QuestScriptSectionEntry entry in QuestScriptSectionCatalog.Entries)
            {
                foreach (QuestScriptSection section in document.Sections)
                {
                    if (section.Entry != entry) continue;

                    switch (entry.Kind)
                    {
                        case QuestScriptSectionKind.Description:
                            AddText(Description, section.Rows);
                            break;
                        case QuestScriptSectionKind.TaskDescription:
                            AddText(TaskDescription, section.Rows);
                            break;
                        case QuestScriptSectionKind.Completion:
                            AddText(CompletionDescription, section.Rows);
                            break;
                        case QuestScriptSectionKind.ReturnDescription:
                            AddText(ReturnDescription, section.Rows);
                            break;
                        case QuestScriptSectionKind.CarryItems:
                            AddItemTasks(CarryItems, section.Rows);
                            break;
                        case QuestScriptSectionKind.ItemTasks:
                            AddItemTasks(ItemTasks, section.Rows);
                            break;
                        case QuestScriptSectionKind.KillTasks:
                            AddKillTasks(section.Rows);
                            break;
                        case QuestScriptSectionKind.FlagTasks:
                            AddFlagTasks(section.Rows);
                            break;
                        case QuestScriptSectionKind.FixedRewards:
                            AddRewards(FixedRewards, section.Rows);
                            break;
                        case QuestScriptSectionKind.SelectRewards:
                            AddRewards(SelectRewards, section.Rows);
                            break;
                        case QuestScriptSectionKind.ExpReward:
                            ApplyNumber(section.Rows, ref ExpReward);
                            break;
                        case QuestScriptSectionKind.GoldReward:
                            ApplyNumber(section.Rows, ref GoldReward);
                            break;
                        case QuestScriptSectionKind.CreditReward:
                            ApplyNumber(section.Rows, ref CreditReward);
                            break;
                    }
                }
            }
        }

        private static void AddText(List<string> descriptions, IReadOnlyList<QuestScriptRow> rows)
        {
            foreach (QuestScriptRow row in rows) descriptions.Add(row.Source.Text);
        }

        private void AddItemTasks(List<QuestItemTask> tasks, IReadOnlyList<QuestScriptRow> rows)
        {
            foreach (QuestScriptRow row in rows)
            {
                ItemInfo info = Envir.GetItemInfo(row.Name);

                if (info == null) continue;

                tasks.Add(new QuestItemTask { Item = info, Count = (ushort)row.Count, Message = row.Message });
            }
        }

        private void AddKillTasks(IReadOnlyList<QuestScriptRow> rows)
        {
            foreach (QuestScriptRow row in rows)
            {
                MonsterInfo info = Envir.GetMonsterInfo(row.Name);

                if (info == null) continue;

                KillTasks.Add(new QuestKillTask { Monster = info, Count = row.Count, Message = row.Message });
            }
        }

        private void AddFlagTasks(IReadOnlyList<QuestScriptRow> rows)
        {
            foreach (QuestScriptRow row in rows)
            {
                if (row.Flag < 0 || row.Flag > Globals.FlagIndexCount - 1000) continue;

                FlagTasks.Add(new QuestFlagTask { Number = row.Flag, Message = row.Message });
            }
        }

        private void AddRewards(List<QuestItemReward> list, IReadOnlyList<QuestScriptRow> rows)
        {
            foreach (QuestScriptRow row in rows) AddReward(list, row);
        }

        private void AddReward(List<QuestItemReward> list, QuestScriptRow row)
        {
            ItemInfo info = Envir.GetItemInfo(row.Name);

            if (info == null)
            {
                info = Envir.GetItemInfo(row.Name + "(M)");
                if (info != null) list.Add(new QuestItemReward() { Item = info, Count = (ushort)row.Count });

                info = Envir.GetItemInfo(row.Name + "(F)");
                if (info != null) list.Add(new QuestItemReward() { Item = info, Count = (ushort)row.Count });
            }
            else
            {
                list.Add(new QuestItemReward() { Item = info, Count = (ushort)row.Count });
            }
        }

        private static void ApplyNumber(IReadOnlyList<QuestScriptRow> rows, ref uint value)
        {
            // Every line of a numeric section was assigned in turn, so the last one wins.
            foreach (QuestScriptRow row in rows) value = row.Number;
        }

        public void ParseReward(List<QuestItemReward> list, string line)
        {
            QuestScriptRow row = ParseQuestRow(QuestScriptSectionKind.FixedRewards, line);

            if (row != null) AddReward(list, row);
        }

        public QuestKillTask ParseKill(string line)
        {
            QuestScriptRow row = ParseQuestRow(QuestScriptSectionKind.KillTasks, line);

            if (row == null) return null;

            MonsterInfo mInfo = Envir.GetMonsterInfo(row.Name);

            return mInfo == null ? null : new QuestKillTask() { Monster = mInfo, Count = row.Count, Message = row.Message };
        }

        public QuestItemTask ParseItem(string line)
        {
            QuestScriptRow row = ParseQuestRow(QuestScriptSectionKind.ItemTasks, line);

            if (row == null) return null;

            ItemInfo mInfo = Envir.GetItemInfo(row.Name);

            return mInfo == null ? null : new QuestItemTask { Item = mInfo, Count = (ushort)row.Count, Message = row.Message };
        }

        public QuestFlagTask ParseFlag(string line)
        {
            QuestScriptRow row = ParseQuestRow(QuestScriptSectionKind.FlagTasks, line);

            if (row == null || row.Flag < 0 || row.Flag > Globals.FlagIndexCount - 1000) return null;

            return new QuestFlagTask { Number = row.Flag, Message = row.Message };
        }

        private static QuestScriptRow ParseQuestRow(QuestScriptSectionKind kind, string line)
        {
            return QuestScriptParser.ParseRow(QuestScriptSectionCatalog.Find(kind), line);
        }

        public bool CanAccept(PlayerObject player)
        {
            if (RequiredMinLevel > player.Level || RequiredMaxLevel < player.Level)
                return false;

            if (RequiredQuest > 0 && !player.CompletedQuests.Contains(RequiredQuest))
                return false;

            switch (player.Class)
            {
                case MirClass.Warrior:
                    if (!RequiredClass.HasFlag(RequiredClass.Warrior))
                        return false;
                    break;
                case MirClass.Wizard:
                    if (!RequiredClass.HasFlag(RequiredClass.Wizard))
                        return false;
                    break;
                case MirClass.Taoist:
                    if (!RequiredClass.HasFlag(RequiredClass.Taoist))
                        return false;
                    break;
                case MirClass.Assassin:
                    if (!RequiredClass.HasFlag(RequiredClass.Assassin))
                        return false;
                    break;
                case MirClass.Archer:
                    if (!RequiredClass.HasFlag(RequiredClass.Archer))
                        return false;
                    break;
            }

            return true;
        }

        public ClientQuestInfo CreateClientQuestInfo(PlayerObject viewer = null)
        {
            var description = LinkFormatter.ReplacePlaceholders(Description, (type, index) => EnsureLinkInfo(viewer, type, index)) ?? new List<string>();
            var taskDescription = LinkFormatter.ReplacePlaceholders(TaskDescription, (type, index) => EnsureLinkInfo(viewer, type, index)) ?? new List<string>();
            var returnDescription = LinkFormatter.ReplacePlaceholders(ReturnDescription, (type, index) => EnsureLinkInfo(viewer, type, index)) ?? new List<string>();
            var completionDescription = LinkFormatter.ReplacePlaceholders(CompletionDescription, (type, index) => EnsureLinkInfo(viewer, type, index)) ?? new List<string>();

            return new ClientQuestInfo
            {
                Index = Index,
                NPCIndex = NpcIndex,
                FinishNPCIndex = FinishNpcIndex,
                Name = Name,
                Group = Group,
                Description = description,
                TaskDescription = taskDescription,
                ReturnDescription = returnDescription,
                CompletionDescription = completionDescription,
                MinLevelNeeded = RequiredMinLevel,
                MaxLevelNeeded = RequiredMaxLevel,
                ClassNeeded = RequiredClass,
                QuestNeeded = RequiredQuest,
                Type = Type,
                TimeLimitInSeconds = TimeLimitInSeconds,
                RewardGold = GoldReward,
                RewardExp = ExpReward,
                RewardCredit = CreditReward,
                RewardsFixedItem = FixedRewards,
                RewardsSelectItem = SelectRewards
            };
        }

        private void EnsureLinkInfo(PlayerObject viewer, string linkType, int index)
        {
            if (viewer?.Connection == null) return;

            switch (linkType)
            {
                case "ITEM":
                    var itemInfo = Envir.GetItemInfo(index);
                    if (itemInfo != null)
                        viewer.Connection.CheckItemInfo(itemInfo);
                    break;
                case "MONSTER":
                    viewer.Connection.CheckMonsterInfo(index);
                    break;
                case "NPC":
                    viewer.Connection.CheckNPCInfo(index);
                    break;
            }
        }

        public static void FromText(string text)
        {
            FromCsvRow(CsvFile.ParseRow(text));
        }

        public static void FromCsvRow(string[] data)
        {
            if (data.Length < 13) return;
            QuestInfo info;
            bool isNew = false;
            if (!int.TryParse(data[0], out var index))
            {
                index = -1;
            }
            if (index == -1 || (info = EditEnvir.QuestInfoList.FirstOrDefault(it => it.Index == index)) == null)
            {
                info = new QuestInfo() { Index = ++EditEnvir.QuestIndex };
                isNew = true;

            }
            info.Name = data[1];
            info.Group = data[2];

            byte temp;

            byte.TryParse(data[3], out temp);

            info.Type = (QuestType)temp;

            info.FileName = data[4];
            info.GotoMessage = data[5];
            info.KillMessage = data[6];
            info.ItemMessage = data[7];
            info.FlagMessage = data[8];

            int.TryParse(data[9], out info.RequiredMinLevel);
            int.TryParse(data[10], out info.RequiredMaxLevel);
            int.TryParse(data[11], out info.RequiredQuest);

            byte.TryParse(data[12], out temp);

            info.RequiredClass = (RequiredClass)temp;

            if (isNew) EditEnvir.QuestInfoList.Add(info);
        }

        public string ToText()
        {
            return CsvFile.FormatRow(new[]
            {
                Index.ToString(), Name, Group, ((byte)Type).ToString(), FileName,
                GotoMessage, KillMessage, ItemMessage, FlagMessage, RequiredMinLevel.ToString(),
                RequiredMaxLevel.ToString(), RequiredQuest.ToString(), ((byte)RequiredClass).ToString()
            });
        }

        public override string ToString()
        {
            return string.Format("{0}:   {1}", Index, Name);
        }
    }

    public class QuestKillTask
    {
        public MonsterInfo Monster;
        public int Count;
        public string Message;
    }

    public class QuestItemTask
    {
        public ItemInfo Item;
        public ushort Count;
        public string Message;
    }

    public class QuestFlagTask
    {
        public int Number;
        public string Message;
    }
}
