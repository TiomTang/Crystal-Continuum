namespace Server.Scripting
{

    /// <summary>Identity of a condition, used as the key of the #IF table.</summary>
    public enum ScriptCheckKind
    {
        IsAdmin,
        Level,
        CheckItem,
        CheckGold,
        CheckGuildGold,
        CheckCredit,
        CheckGender,
        CheckClass,
        CheckDay,
        CheckHour,
        CheckMinute,
        CheckNameList,
        CheckPkPoint,
        CheckRange,
        Check,
        CheckHum,
        CheckMon,
        CheckExactMon,
        Random,
        Groupleader,
        GroupCount,
        GroupCheckNearby,
        PetLevel,
        PetCount,
        CheckCalc,
        InGuild,
        CheckMap,
        CheckQuest,
        CheckRelationship,
        CheckWeddingRing,
        CheckPet,
        HasBagSpace,
        IsNewHuman,
        CheckConquest,
        AffordGuard,
        AffordGate,
        AffordWall,
        AffordSiege,
        CheckPermission,
        ConquestAvailable,
        ConquestOwner,
        CheckGuildNameList,
        CheckTimer,
        HeroLevel,
        CheckHeroClass,
        CheckHeroGender,
        CheckHeroItem,
        HasGT,
        CheckBuff,
        CheckMapLight
    }

    /// <summary>
    /// The #IF keyword table. Every entry names the handler that implements it, so the table doubles
    /// as the dispatch map and as the list the editor shows inside an #IF block.
    /// </summary>
    /// <summary>
    /// The #IF keyword table. Every entry names the handler that parses the line and the executor that
    /// runs it, so the table doubles as the dispatch map and as the list the editor offers inside #IF.
    /// </summary>
    public static class ScriptCheckCatalog
    {
        public static readonly ScriptKeywordEntry[] Entries =
        {
            new ScriptKeywordEntry("LEVEL", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <level>",
                "Compare the player's level with a value.", 3, ScriptCheckHandlers.Check_Level, check: ScriptCheckKind.Level, executor: ScriptSegment.Exec_Level),
            new ScriptKeywordEntry("CHECKGOLD", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <amount>",
                "Compare the gold the player carries.", 3, ScriptCheckHandlers.Check_Gold, check: ScriptCheckKind.CheckGold, executor: ScriptSegment.Exec_CheckGold),
            new ScriptKeywordEntry("CHECKCREDIT", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <amount>",
                "Compare the player's credit balance.", 3, ScriptCheckHandlers.Check_Credit, check: ScriptCheckKind.CheckCredit, executor: ScriptSegment.Exec_CheckCredit),
            new ScriptKeywordEntry("CHECKGENDER", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<Male|Female>",
                "Check the player's gender.", 2, ScriptCheckHandlers.Check_Gender, check: ScriptCheckKind.CheckGender, executor: ScriptSegment.Exec_CheckGender),
            new ScriptKeywordEntry("CHECKCLASS", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<class>",
                "Check the player's class.", 2, ScriptCheckHandlers.Check_Class, check: ScriptCheckKind.CheckClass, executor: ScriptSegment.Exec_CheckClass),
            new ScriptKeywordEntry("CHECKPKPOINT", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <points>",
                "Compare the player's PK points.", 3, ScriptCheckHandlers.Check_PkPoint, check: ScriptCheckKind.CheckPkPoint, executor: ScriptSegment.Exec_CheckPkPoint),
            new ScriptKeywordEntry("CHECK", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "[<flag index>] <0|1>",
                "Check a player flag; 0 is false and any nonzero unsigned value is true.", 3, ScriptCheckHandlers.Check_Flag, check: ScriptCheckKind.Check, executor: ScriptSegment.Exec_Check),
            new ScriptKeywordEntry("ISNEWHUMAN", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "",
                "Require the account to hold a single character.", 1, ScriptCheckHandlers.Check_IsNewHuman, check: ScriptCheckKind.IsNewHuman, executor: ScriptSegment.Exec_IsNewHuman),

            new ScriptKeywordEntry("CHECKITEM", ScriptKeywordKind.Check, ScriptKeywordCategory.Item, "<item> [count] [durability]",
                "Check carried items, optionally requiring minimum durability in whole points.", 2, ScriptCheckHandlers.Check_Item, check: ScriptCheckKind.CheckItem, executor: ScriptSegment.Exec_CheckItem, argument: ScriptArgumentKind.Item),
            new ScriptKeywordEntry("HASBAGSPACE", ScriptKeywordKind.Check, ScriptKeywordCategory.Item, "<operator> <free slots>",
                "Compare the number of free bag slots.", 3, ScriptCheckHandlers.Check_BagSpace, check: ScriptCheckKind.HasBagSpace, executor: ScriptSegment.Exec_HasBagSpace),

            new ScriptKeywordEntry("CHECKNAMELIST", ScriptKeywordKind.Check, ScriptKeywordCategory.Social, "<list>",
                "Check whether the player's name is in a name list.", 2, ScriptCheckHandlers.Check_NameList, check: ScriptCheckKind.CheckNameList, executor: ScriptSegment.Exec_CheckNameList),
            new ScriptKeywordEntry("CHECKRELATIONSHIP", ScriptKeywordKind.Check, ScriptKeywordCategory.Social, "",
                "Require the player to be married.", 1, ScriptCheckHandlers.Check_Relationship, check: ScriptCheckKind.CheckRelationship, executor: ScriptSegment.Exec_CheckRelationship),
            new ScriptKeywordEntry("CHECKWEDDINGRING", ScriptKeywordKind.Check, ScriptKeywordCategory.Social, "",
                "Require the player to be able to make a wedding ring.", 1, ScriptCheckHandlers.Check_WeddingRing, check: ScriptCheckKind.CheckWeddingRing, executor: ScriptSegment.Exec_CheckWeddingRing),

            new ScriptKeywordEntry("CHECKGUILDGOLD", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<operator> <amount>",
                "Compare the gold held by the guild.", 3, ScriptCheckHandlers.Check_GuildGold, check: ScriptCheckKind.CheckGuildGold, executor: ScriptSegment.Exec_CheckGuildGold),
            new ScriptKeywordEntry("CHECKGUILDNAMELIST", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<list>",
                "Check whether the player's guild is in a name list.", 2, ScriptCheckHandlers.Check_GuildNameList, check: ScriptCheckKind.CheckGuildNameList, executor: ScriptSegment.Exec_CheckGuildNameList),
            new ScriptKeywordEntry("INGUILD", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "[<guild name>]",
                "Check guild membership, optionally of one named guild.", 1, ScriptCheckHandlers.Check_InGuild, check: ScriptCheckKind.InGuild, executor: ScriptSegment.Exec_InGuild),
            new ScriptKeywordEntry("CHECKPERMISSION", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<permission>",
                "Check a permission of the player's guild rank.", 2, ScriptCheckHandlers.Check_Permission, check: ScriptCheckKind.CheckPermission, executor: ScriptSegment.Exec_CheckPermission),
            new ScriptKeywordEntry("HASGT", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "",
                "Require the player's guild to own a guild territory.", 1, ScriptCheckHandlers.Check_HasGT, check: ScriptCheckKind.HasGT, executor: ScriptSegment.Exec_HasGT),
            new ScriptKeywordEntry("AFFORDGUARD", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <guard index>",
                "Check whether the guild can pay to repair a guard.", 3, ScriptCheckHandlers.Check_AffordGuard, check: ScriptCheckKind.AffordGuard, executor: ScriptSegment.Exec_AffordGuard),
            new ScriptKeywordEntry("AFFORDGATE", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <gate index>",
                "Check whether the guild can pay to repair a gate.", 3, ScriptCheckHandlers.Check_AffordGate, check: ScriptCheckKind.AffordGate, executor: ScriptSegment.Exec_AffordGate),
            new ScriptKeywordEntry("AFFORDWALL", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <wall index>",
                "Check whether the guild can pay to repair a wall.", 3, ScriptCheckHandlers.Check_AffordWall, check: ScriptCheckKind.AffordWall, executor: ScriptSegment.Exec_AffordWall),
            new ScriptKeywordEntry("AFFORDSIEGE", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <gate index>",
                "Check whether the guild can pay to repair a siege gate.", 3, ScriptCheckHandlers.Check_AffordSiege, check: ScriptCheckKind.AffordSiege, executor: ScriptSegment.Exec_AffordSiege),

            new ScriptKeywordEntry("GROUPLEADER", ScriptKeywordKind.Check, ScriptKeywordCategory.Group, "",
                "Require the player to be the group leader.", 1, ScriptCheckHandlers.Check_GroupLeader, check: ScriptCheckKind.Groupleader, executor: ScriptSegment.Exec_Groupleader),
            new ScriptKeywordEntry("GROUPCOUNT", ScriptKeywordKind.Check, ScriptKeywordCategory.Group, "<operator> <count>",
                "Compare the size of the player's group.", 3, ScriptCheckHandlers.Check_GroupCount, check: ScriptCheckKind.GroupCount, executor: ScriptSegment.Exec_GroupCount),
            new ScriptKeywordEntry("GROUPCHECKNEARBY", ScriptKeywordKind.Check, ScriptKeywordCategory.Group, "",
                "Require every group member to stand within nine tiles of the NPC.", 1, ScriptCheckHandlers.Check_GroupNearby, check: ScriptCheckKind.GroupCheckNearby, executor: ScriptSegment.Exec_GroupCheckNearby),

            new ScriptKeywordEntry("PETCOUNT", ScriptKeywordKind.Check, ScriptKeywordCategory.Pet, "<operator> <count>",
                "Compare how many pets the player has.", 3, ScriptCheckHandlers.Check_PetCount, check: ScriptCheckKind.PetCount, executor: ScriptSegment.Exec_PetCount),
            new ScriptKeywordEntry("PETLEVEL", ScriptKeywordKind.Check, ScriptKeywordCategory.Pet, "<operator> <level>",
                "Compare the last pet level in the pet list; an empty pet list passes.", 3, ScriptCheckHandlers.Check_PetLevel, check: ScriptCheckKind.PetLevel, executor: ScriptSegment.Exec_PetLevel),
            new ScriptKeywordEntry("CHECKPET", ScriptKeywordKind.Check, ScriptKeywordCategory.Pet, "<pet name>",
                "Check whether the player owns a named pet.", 2, ScriptCheckHandlers.Check_Pet, check: ScriptCheckKind.CheckPet, executor: ScriptSegment.Exec_CheckPet),

            new ScriptKeywordEntry("HEROLEVEL", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<operator> <level>",
                "Compare the hero's level.", 3, ScriptCheckHandlers.Check_HeroLevel, check: ScriptCheckKind.HeroLevel, executor: ScriptSegment.Exec_HeroLevel),
            new ScriptKeywordEntry("CHECKHEROCLASS", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<class>",
                "Check the hero's class.", 2, ScriptCheckHandlers.Check_HeroClass, check: ScriptCheckKind.CheckHeroClass, executor: ScriptSegment.Exec_CheckHeroClass),
            new ScriptKeywordEntry("CHECKHEROGENDER", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<Male|Female>",
                "Check the hero's gender.", 2, ScriptCheckHandlers.Check_HeroGender, check: ScriptCheckKind.CheckHeroGender, executor: ScriptSegment.Exec_CheckHeroGender),
            new ScriptKeywordEntry("CHECKHEROITEM", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<item> [count] [durability]",
                "Check hero inventory items, optionally requiring minimum durability in whole points.", 2, ScriptCheckHandlers.Check_HeroItem, check: ScriptCheckKind.CheckHeroItem, executor: ScriptSegment.Exec_CheckHeroItem),

            new ScriptKeywordEntry("CHECKQUEST", ScriptKeywordKind.Check, ScriptKeywordCategory.Quest, "<quest index> <ACTIVE|COMPLETE>",
                "Check a quest: ACTIVE checks current quests; every other state token checks completed quests.", 3, ScriptCheckHandlers.Check_Quest, check: ScriptCheckKind.CheckQuest, executor: ScriptSegment.Exec_CheckQuest),

            new ScriptKeywordEntry("CHECKRANGE", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<x> <y> <range>",
                "Check whether the player stands within range of a point.", 4, ScriptCheckHandlers.Check_Range, check: ScriptCheckKind.CheckRange, executor: ScriptSegment.Exec_CheckRange),
            new ScriptKeywordEntry("CHECKMAP", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<map>",
                "Check the player's current map.", 2, ScriptCheckHandlers.Check_Map, check: ScriptCheckKind.CheckMap, executor: ScriptSegment.Exec_CheckMap),
            new ScriptKeywordEntry("MAPLIGHT", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<light>",
                "Check the server-wide light setting.", 2, ScriptCheckHandlers.Check_MapLight, check: ScriptCheckKind.CheckMapLight, executor: ScriptSegment.Exec_CheckMapLight),
            new ScriptKeywordEntry("CHECKHUM", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<operator> <count> <map> [instance]",
                "Compare how many players are on a map.", 4, ScriptCheckHandlers.Check_Hum, check: ScriptCheckKind.CheckHum, executor: ScriptSegment.Exec_CheckHum),
            new ScriptKeywordEntry("CHECKMON", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<operator> <count> <map> [instance]",
                "Compare how many monsters are on a map.", 4, ScriptCheckHandlers.Check_Mon, check: ScriptCheckKind.CheckMon, executor: ScriptSegment.Exec_CheckMon),
            new ScriptKeywordEntry("CHECKEXACTMON", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<monster> <operator> <count> <map> [instance]",
                "Compare how many of one named monster are on a map.", 5, ScriptCheckHandlers.Check_ExactMon, check: ScriptCheckKind.CheckExactMon, executor: ScriptSegment.Exec_CheckExactMon),
            new ScriptKeywordEntry("CHECKCONQUEST", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<conquest index>",
                "Require the conquest to exist and no conquest war to be running.", 2, ScriptCheckHandlers.Check_Conquest, check: ScriptCheckKind.CheckConquest, executor: ScriptSegment.Exec_CheckConquest),
            new ScriptKeywordEntry("CONQUESTAVAILABLE", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<conquest index>",
                "Check whether the player's guild may start a conquest attack.", 2, ScriptCheckHandlers.Check_ConquestAvailable, check: ScriptCheckKind.ConquestAvailable, executor: ScriptSegment.Exec_ConquestAvailable),
            new ScriptKeywordEntry("CONQUESTOWNER", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<conquest index>",
                "Check whether the player's guild owns a conquest.", 2, ScriptCheckHandlers.Check_ConquestOwner, check: ScriptCheckKind.ConquestOwner, executor: ScriptSegment.Exec_ConquestOwner),

            new ScriptKeywordEntry("DAYOFWEEK", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<day>",
                "Check the server day of the week by its English name.", 2, ScriptCheckHandlers.Check_DayOfWeek, check: ScriptCheckKind.CheckDay, executor: ScriptSegment.Exec_CheckDay),
            new ScriptKeywordEntry("HOUR", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<hour>",
                "Require the current server hour to equal the value (0-23).", 2, ScriptCheckHandlers.Check_Hour, check: ScriptCheckKind.CheckHour, executor: ScriptSegment.Exec_CheckHour),
            new ScriptKeywordEntry("MIN", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<minute>",
                "Require the current server minute to equal the value (0-59).", 2, ScriptCheckHandlers.Check_Minute, check: ScriptCheckKind.CheckMinute, executor: ScriptSegment.Exec_CheckMinute),
            new ScriptKeywordEntry("CHECKTIMER", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<timer> <seconds> <operator>",
                "Compare the time left on a timer.", 4, ScriptCheckHandlers.Check_Timer, check: ScriptCheckKind.CheckTimer, executor: ScriptSegment.Exec_CheckTimer),

            new ScriptKeywordEntry("CHECKBUFF", ScriptKeywordKind.Check, ScriptKeywordCategory.Buff, "<buff>",
                "Check whether the player has a buff.", 2, ScriptCheckHandlers.Check_Buff, check: ScriptCheckKind.CheckBuff, executor: ScriptSegment.Exec_CheckBuff),

            new ScriptKeywordEntry("ISADMIN", ScriptKeywordKind.Check, ScriptKeywordCategory.Admin, "",
                "Require the player to be a GM.", 1, ScriptCheckHandlers.Check_IsAdmin, check: ScriptCheckKind.IsAdmin, executor: ScriptSegment.Exec_IsAdmin),

            new ScriptKeywordEntry("RANDOM", ScriptKeywordKind.Check, ScriptKeywordCategory.Flow, "<chance>",
                "Succeed with a one-in-N chance.", 2, ScriptCheckHandlers.Check_Random, check: ScriptCheckKind.Random, executor: ScriptSegment.Exec_Random),
            new ScriptKeywordEntry("CHECKCALC", ScriptKeywordKind.Check, ScriptKeywordCategory.Flow, "<left> <operator> <right>",
                "Compare integers when both operands are numeric, otherwise compare text.", 4, ScriptCheckHandlers.Check_Calc, check: ScriptCheckKind.CheckCalc, executor: ScriptSegment.Exec_CheckCalc),
        };

        private static readonly Dictionary<string, ScriptKeywordEntry> Index = BuildIndex();
        private static readonly Dictionary<ScriptCheckKind, ScriptKeywordEntry> IndexByKind = BuildKindIndex();

        private static Dictionary<string, ScriptKeywordEntry> BuildIndex()
        {
            var index = new Dictionary<string, ScriptKeywordEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptKeywordEntry entry in Entries)
            {
                if (index.ContainsKey(entry.Name))
                {
#if DEBUG
                    throw new InvalidOperationException("Duplicate NPC script keyword: " + entry.Name);
#else
                    continue;
#endif
                }

                index.Add(entry.Name, entry);
            }

            return index;
        }

        /// <summary>Condition for a name as written in the script, or null.</summary>
        public static ScriptKeywordEntry Find(string name)
        {
            ScriptKeywordEntry entry;

            return name != null && Index.TryGetValue(name.Trim(), out entry) ? entry : null;
        }


        private static Dictionary<ScriptCheckKind, ScriptKeywordEntry> BuildKindIndex()
        {
            var index = new Dictionary<ScriptCheckKind, ScriptKeywordEntry>();

            foreach (ScriptKeywordEntry entry in Entries)
            {
                if (!entry.Check.HasValue) continue;

                index[entry.Check.Value] = entry;
            }

            return index;
        }        /// <summary>Condition for a kind, so the executor can be read from the same row.</summary>
        public static ScriptKeywordEntry Find(ScriptCheckKind kind)
        {
            ScriptKeywordEntry entry;

            return IndexByKind.TryGetValue(kind, out entry) ? entry : null;
        }
    }
}
