namespace Server.MirObjects
{
    /// <summary>
    /// The #IF keyword table. Every entry names the handler that implements it, so the table doubles
    /// as the dispatch map and as the list the editor shows inside an #IF block.
    /// </summary>
    public static class ScriptCheckCatalog
    {
        public static readonly ScriptKeyword[] Entries =
        {
            new ScriptKeyword("LEVEL", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <level>",
                "Compare the player's level with a value.", 3, ScriptCheckHandlers.Check_Level, check: CheckType.Level, executor: NPCSegment.Exec_Level),
            new ScriptKeyword("CHECKGOLD", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <amount>",
                "Compare the gold the player carries.", 3, ScriptCheckHandlers.Check_Gold, check: CheckType.CheckGold, executor: NPCSegment.Exec_CheckGold),
            new ScriptKeyword("CHECKCREDIT", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <amount>",
                "Compare the player's credit balance.", 3, ScriptCheckHandlers.Check_Credit, check: CheckType.CheckCredit, executor: NPCSegment.Exec_CheckCredit),
            new ScriptKeyword("CHECKGENDER", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<Male|Female>",
                "Check the player's gender.", 2, ScriptCheckHandlers.Check_Gender, check: CheckType.CheckGender, executor: NPCSegment.Exec_CheckGender),
            new ScriptKeyword("CHECKCLASS", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<class>",
                "Check the player's class.", 2, ScriptCheckHandlers.Check_Class, check: CheckType.CheckClass, executor: NPCSegment.Exec_CheckClass),
            new ScriptKeyword("CHECKPKPOINT", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "<operator> <points>",
                "Compare the player's PK points.", 3, ScriptCheckHandlers.Check_PkPoint, check: CheckType.CheckPkPoint, executor: NPCSegment.Exec_CheckPkPoint),
            new ScriptKeyword("CHECK", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "[<flag index>] <true|false>",
                "Check a player flag.", 3, ScriptCheckHandlers.Check_Flag, check: CheckType.Check, executor: NPCSegment.Exec_Check),
            new ScriptKeyword("ISNEWHUMAN", ScriptKeywordKind.Check, ScriptKeywordCategory.Player, "",
                "Require the account to hold a single character.", 1, ScriptCheckHandlers.Check_IsNewHuman, check: CheckType.IsNewHuman, executor: NPCSegment.Exec_IsNewHuman),

            new ScriptKeyword("CHECKITEM", ScriptKeywordKind.Check, ScriptKeywordCategory.Item, "<item> [count] [durability]",
                "Check whether the player carries an item.", 2, ScriptCheckHandlers.Check_Item, check: CheckType.CheckItem, executor: NPCSegment.Exec_CheckItem),
            new ScriptKeyword("HASBAGSPACE", ScriptKeywordKind.Check, ScriptKeywordCategory.Item, "<operator> <free slots>",
                "Compare the number of free bag slots.", 3, ScriptCheckHandlers.Check_BagSpace, check: CheckType.HasBagSpace, executor: NPCSegment.Exec_HasBagSpace),

            new ScriptKeyword("CHECKNAMELIST", ScriptKeywordKind.Check, ScriptKeywordCategory.Social, "<list>",
                "Check whether the player's name is in a name list.", 2, ScriptCheckHandlers.Check_NameList, check: CheckType.CheckNameList, executor: NPCSegment.Exec_CheckNameList),
            new ScriptKeyword("CHECKRELATIONSHIP", ScriptKeywordKind.Check, ScriptKeywordCategory.Social, "",
                "Require the player to be married.", 1, ScriptCheckHandlers.Check_Relationship, check: CheckType.CheckRelationship, executor: NPCSegment.Exec_CheckRelationship),
            new ScriptKeyword("CHECKWEDDINGRING", ScriptKeywordKind.Check, ScriptKeywordCategory.Social, "",
                "Require the player to be able to make a wedding ring.", 1, ScriptCheckHandlers.Check_WeddingRing, check: CheckType.CheckWeddingRing, executor: NPCSegment.Exec_CheckWeddingRing),

            new ScriptKeyword("CHECKGUILDGOLD", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<operator> <amount>",
                "Compare the gold held by the guild.", 3, ScriptCheckHandlers.Check_GuildGold, check: CheckType.CheckGuildGold, executor: NPCSegment.Exec_CheckGuildGold),
            new ScriptKeyword("CHECKGUILDNAMELIST", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<list>",
                "Check whether the player's guild is in a name list.", 2, ScriptCheckHandlers.Check_GuildNameList, check: CheckType.CheckGuildNameList, executor: NPCSegment.Exec_CheckGuildNameList),
            new ScriptKeyword("INGUILD", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "[<guild name>]",
                "Check guild membership, optionally of one named guild.", 1, ScriptCheckHandlers.Check_InGuild, check: CheckType.InGuild, executor: NPCSegment.Exec_InGuild),
            new ScriptKeyword("CHECKPERMISSION", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<permission>",
                "Check a permission of the player's guild rank.", 2, ScriptCheckHandlers.Check_Permission, check: CheckType.CheckPermission, executor: NPCSegment.Exec_CheckPermission),
            new ScriptKeyword("HASGT", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "",
                "Require the player's guild to own a guild territory.", 1, ScriptCheckHandlers.Check_HasGT, check: CheckType.HasGT, executor: NPCSegment.Exec_HasGT),
            new ScriptKeyword("AFFORDGUARD", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <guard index>",
                "Check whether the guild can pay to repair a guard.", 3, ScriptCheckHandlers.Check_AffordGuard, check: CheckType.AffordGuard, executor: NPCSegment.Exec_AffordGuard),
            new ScriptKeyword("AFFORDGATE", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <gate index>",
                "Check whether the guild can pay to repair a gate.", 3, ScriptCheckHandlers.Check_AffordGate, check: CheckType.AffordGate, executor: NPCSegment.Exec_AffordGate),
            new ScriptKeyword("AFFORDWALL", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <wall index>",
                "Check whether the guild can pay to repair a wall.", 3, ScriptCheckHandlers.Check_AffordWall, check: CheckType.AffordWall, executor: NPCSegment.Exec_AffordWall),
            new ScriptKeyword("AFFORDSIEGE", ScriptKeywordKind.Check, ScriptKeywordCategory.Guild, "<conquest index> <gate index>",
                "Check whether the guild can pay to repair a siege gate.", 3, ScriptCheckHandlers.Check_AffordSiege, check: CheckType.AffordSiege, executor: NPCSegment.Exec_AffordSiege),

            new ScriptKeyword("GROUPLEADER", ScriptKeywordKind.Check, ScriptKeywordCategory.Group, "",
                "Require the player to be the group leader.", 1, ScriptCheckHandlers.Check_GroupLeader, check: CheckType.Groupleader, executor: NPCSegment.Exec_Groupleader),
            new ScriptKeyword("GROUPCOUNT", ScriptKeywordKind.Check, ScriptKeywordCategory.Group, "<operator> <count>",
                "Compare the size of the player's group.", 3, ScriptCheckHandlers.Check_GroupCount, check: CheckType.GroupCount, executor: NPCSegment.Exec_GroupCount),
            new ScriptKeyword("GROUPCHECKNEARBY", ScriptKeywordKind.Check, ScriptKeywordCategory.Group, "",
                "Require every group member to stand within nine tiles of the NPC.", 1, ScriptCheckHandlers.Check_GroupNearby, check: CheckType.GroupCheckNearby, executor: NPCSegment.Exec_GroupCheckNearby),

            new ScriptKeyword("PETCOUNT", ScriptKeywordKind.Check, ScriptKeywordCategory.Pet, "<operator> <count>",
                "Compare how many pets the player has.", 3, ScriptCheckHandlers.Check_PetCount, check: CheckType.PetCount, executor: NPCSegment.Exec_PetCount),
            new ScriptKeyword("PETLEVEL", ScriptKeywordKind.Check, ScriptKeywordCategory.Pet, "<operator> <level>",
                "Compare the level of the player's pet.", 3, ScriptCheckHandlers.Check_PetLevel, check: CheckType.PetLevel, executor: NPCSegment.Exec_PetLevel),
            new ScriptKeyword("CHECKPET", ScriptKeywordKind.Check, ScriptKeywordCategory.Pet, "<pet name>",
                "Check whether the player owns a named pet.", 2, ScriptCheckHandlers.Check_Pet, check: CheckType.CheckPet, executor: NPCSegment.Exec_CheckPet),

            new ScriptKeyword("HEROLEVEL", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<operator> <level>",
                "Compare the hero's level.", 3, ScriptCheckHandlers.Check_HeroLevel, check: CheckType.HeroLevel, executor: NPCSegment.Exec_HeroLevel),
            new ScriptKeyword("CHECKHEROCLASS", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<class>",
                "Check the hero's class.", 2, ScriptCheckHandlers.Check_HeroClass, check: CheckType.CheckHeroClass, executor: NPCSegment.Exec_CheckHeroClass),
            new ScriptKeyword("CHECKHEROGENDER", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<Male|Female>",
                "Check the hero's gender.", 2, ScriptCheckHandlers.Check_HeroGender, check: CheckType.CheckHeroGender, executor: NPCSegment.Exec_CheckHeroGender),
            new ScriptKeyword("CHECKHEROITEM", ScriptKeywordKind.Check, ScriptKeywordCategory.Hero, "<item> [count] [durability]",
                "Check whether the hero carries an item.", 2, ScriptCheckHandlers.Check_HeroItem, check: CheckType.CheckHeroItem, executor: NPCSegment.Exec_CheckHeroItem),

            new ScriptKeyword("CHECKQUEST", ScriptKeywordKind.Check, ScriptKeywordCategory.Quest, "<quest index> <ACTIVE|COMPLETE>",
                "Check a quest's state.", 3, ScriptCheckHandlers.Check_Quest, check: CheckType.CheckQuest, executor: NPCSegment.Exec_CheckQuest),

            new ScriptKeyword("CHECKRANGE", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<x> <y> <range>",
                "Check whether the player stands within range of a point.", 4, ScriptCheckHandlers.Check_Range, check: CheckType.CheckRange, executor: NPCSegment.Exec_CheckRange),
            new ScriptKeyword("CHECKMAP", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<map>",
                "Check the player's current map.", 2, ScriptCheckHandlers.Check_Map, check: CheckType.CheckMap, executor: NPCSegment.Exec_CheckMap),
            new ScriptKeyword("MAPLIGHT", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<light>",
                "Check the current map light setting.", 2, ScriptCheckHandlers.Check_MapLight, check: CheckType.CheckMapLight, executor: NPCSegment.Exec_CheckMapLight),
            new ScriptKeyword("CHECKHUM", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<operator> <count> <map> [instance]",
                "Compare how many players are on a map.", 4, ScriptCheckHandlers.Check_Hum, check: CheckType.CheckHum, executor: NPCSegment.Exec_CheckHum),
            new ScriptKeyword("CHECKMON", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<operator> <count> <map> [instance]",
                "Compare how many monsters are on a map.", 4, ScriptCheckHandlers.Check_Mon, check: CheckType.CheckMon, executor: NPCSegment.Exec_CheckMon),
            new ScriptKeyword("CHECKEXACTMON", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<monster> <operator> <count> <map> [instance]",
                "Compare how many of one named monster are on a map.", 5, ScriptCheckHandlers.Check_ExactMon, check: CheckType.CheckExactMon, executor: NPCSegment.Exec_CheckExactMon),
            new ScriptKeyword("CHECKCONQUEST", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<conquest index>",
                "Check whether a conquest war is running.", 2, ScriptCheckHandlers.Check_Conquest, check: CheckType.CheckConquest, executor: NPCSegment.Exec_CheckConquest),
            new ScriptKeyword("CONQUESTAVAILABLE", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<conquest index>",
                "Check whether the player's guild may start a conquest attack.", 2, ScriptCheckHandlers.Check_ConquestAvailable, check: CheckType.ConquestAvailable, executor: NPCSegment.Exec_ConquestAvailable),
            new ScriptKeyword("CONQUESTOWNER", ScriptKeywordKind.Check, ScriptKeywordCategory.World, "<conquest index>",
                "Check whether the player's guild owns a conquest.", 2, ScriptCheckHandlers.Check_ConquestOwner, check: CheckType.ConquestOwner, executor: NPCSegment.Exec_ConquestOwner),

            new ScriptKeyword("DAYOFWEEK", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<day>",
                "Check the day of the week.", 2, ScriptCheckHandlers.Check_DayOfWeek, check: CheckType.CheckDay, executor: NPCSegment.Exec_CheckDay),
            new ScriptKeyword("HOUR", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<operator> <hour>",
                "Compare the current hour.", 2, ScriptCheckHandlers.Check_Hour, check: CheckType.CheckHour, executor: NPCSegment.Exec_CheckHour),
            new ScriptKeyword("MIN", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<operator> <minute>",
                "Compare the current minute.", 2, ScriptCheckHandlers.Check_Minute, check: CheckType.CheckMinute, executor: NPCSegment.Exec_CheckMinute),
            new ScriptKeyword("CHECKTIMER", ScriptKeywordKind.Check, ScriptKeywordCategory.Time, "<timer> <seconds> <operator>",
                "Compare the time left on a timer.", 4, ScriptCheckHandlers.Check_Timer, check: CheckType.CheckTimer, executor: NPCSegment.Exec_CheckTimer),

            new ScriptKeyword("CHECKBUFF", ScriptKeywordKind.Check, ScriptKeywordCategory.Buff, "<buff>",
                "Check whether the player has a buff.", 2, ScriptCheckHandlers.Check_Buff, check: CheckType.CheckBuff, executor: NPCSegment.Exec_CheckBuff),

            new ScriptKeyword("ISADMIN", ScriptKeywordKind.Check, ScriptKeywordCategory.Admin, "",
                "Require the player to be a GM.", 1, ScriptCheckHandlers.Check_IsAdmin, check: CheckType.IsAdmin, executor: NPCSegment.Exec_IsAdmin),

            new ScriptKeyword("RANDOM", ScriptKeywordKind.Check, ScriptKeywordCategory.Flow, "<chance>",
                "Succeed with a one-in-N chance.", 2, ScriptCheckHandlers.Check_Random, check: CheckType.Random, executor: NPCSegment.Exec_Random),
            new ScriptKeyword("CHECKCALC", ScriptKeywordKind.Check, ScriptKeywordCategory.Flow, "<left> <operator> <right>",
                "Compare two numbers or stored variables.", 4, ScriptCheckHandlers.Check_Calc, check: CheckType.CheckCalc, executor: NPCSegment.Exec_CheckCalc),
        };
    }
}
