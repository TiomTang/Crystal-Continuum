namespace Server.MirObjects
{

    /// <summary>Identity of an action, used as the key of the #ACT table.</summary>
    public enum ScriptActionKind
    {
        Move,
        InstanceMove,
        GiveGold,
        TakeGold,
        GiveGuildGold,
        TakeGuildGold,
        GiveCredit,
        TakeCredit,
        GiveItem,
        TakeItem,
        GiveExp,
        GivePet,
        ClearPets,
        AddNameList,
        DelNameList,
        ClearNameList,
        GiveHP,
        GiveMP,
        ChangeLevel,
        SetPkPoint,
        ReducePkPoint,
        IncreasePkPoint,
        ChangeGender,
        ChangeClass,
        LocalMessage,
        Goto,
        Call,
        GiveSkill,
        RemoveSkill,
        Set,
        Param1,
        Param2,
        Param3,
        Mongen,
        TimeRecall,
        TimeRecallGroup,
        BreakTimeRecall,
        MonClear,
        GroupRecall,
        GroupTeleport,
        DelayGoto,
        Mov,
        Calc,
        GiveBuff,
        RemoveBuff,
        AddToGuild,
        RemoveFromGuild,
        RefreshEffects,
        ChangeHair,
        CanGainExp,
        ComposeMail,
        AddMailItem,
        AddMailGold,
        SendMail,
        GroupGoto,
        EnterMap,
        GivePearls,
        TakePearls,
        MakeWeddingRing,
        ForceDivorce,
        GlobalMessage,
        LoadValue,
        SaveValue,
        RemovePet,
        ConquestGuard,
        ConquestGate,
        ConquestWall,
        ConquestSiege,
        TakeConquestGold,
        SetConquestRate,
        StartConquest,
        ScheduleConquest,
        OpenGate,
        CloseGate,
        Break,
        AddGuildNameList,
        DelGuildNameList,
        ClearGuildNameList,
        OpenBrowser,
        GetRandomText,
        PlaySound,
        SetTimer,
        ExpireTimer,
        UnequipItem,
        RollDie,
        RollYut,
        Drop,
        ReviveHero,
        SealHero,
        DeleteHero,
        ConquestRepairAll,
        BuyGT,
        TeleportGT,
        ExtendGT,
        GTAllRecall,
        GTRecall,
        DisplayGTRentalDays,
        GTSale,
        GTCancelSale,
        HeroGiveSkill,
        HeroRemoveSkill,
        Close,
    }

    /// <summary>
    /// The #ACT keyword table. Every entry names the handler that implements it, so the table doubles
    /// as the dispatch map and as the list the editor shows inside an #ACT or #ELSEACT block.
    /// </summary>
    public static class ScriptActionCatalog
    {
        public static readonly ScriptKeywordEntry[] Entries =
        {
            new ScriptKeywordEntry("MOVE", ScriptKeywordKind.Action, ScriptKeywordCategory.Movement, "<map> [x] [y]",
                "Move the player to a map position.", 2, ScriptActionHandlers.Act_Move, action: ScriptActionKind.Move, executor: NPCSegment.Exec_Move, argument: ScriptArgumentKind.Map),

            new ScriptKeywordEntry("INSTANCEMOVE", ScriptKeywordKind.Action, ScriptKeywordCategory.Movement, "<map> <instance> <x> <y>",
                "Move the player to a map instance position.", 5, ScriptActionHandlers.Act_InstanceMove, action: ScriptActionKind.InstanceMove, executor: NPCSegment.Exec_InstanceMove, argument: ScriptArgumentKind.Map),

            new ScriptKeywordEntry("GIVEGOLD", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Give gold to the player.", 2, ScriptActionHandlers.Act_GiveGold, action: ScriptActionKind.GiveGold, executor: NPCSegment.Exec_GiveGold),

            new ScriptKeywordEntry("TAKEGOLD", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Take gold from the player.", 2, ScriptActionHandlers.Act_TakeGold, action: ScriptActionKind.TakeGold, executor: NPCSegment.Exec_TakeGold),

            new ScriptKeywordEntry("GIVEGUILDGOLD", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Give gold to the player's guild.", 2, ScriptActionHandlers.Act_GiveGuildGold, action: ScriptActionKind.GiveGuildGold, executor: NPCSegment.Exec_GiveGuildGold),

            new ScriptKeywordEntry("TAKEGUILDGOLD", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Take gold from the player's guild.", 2, ScriptActionHandlers.Act_TakeGuildGold, action: ScriptActionKind.TakeGuildGold, executor: NPCSegment.Exec_TakeGuildGold),

            new ScriptKeywordEntry("GIVECREDIT", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Give credit to the player.", 2, ScriptActionHandlers.Act_GiveCredit, action: ScriptActionKind.GiveCredit, executor: NPCSegment.Exec_GiveCredit),

            new ScriptKeywordEntry("TAKECREDIT", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Take credit from the player.", 2, ScriptActionHandlers.Act_TakeCredit, action: ScriptActionKind.TakeCredit, executor: NPCSegment.Exec_TakeCredit),

            new ScriptKeywordEntry("GIVEPEARLS", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Give pearls to the player.", 2, ScriptActionHandlers.Act_GivePearls, action: ScriptActionKind.GivePearls, executor: NPCSegment.Exec_GivePearls),

            new ScriptKeywordEntry("TAKEPEARLS", ScriptKeywordKind.Action, ScriptKeywordCategory.Currency, "<amount>",
                "Take pearls from the player.", 2, ScriptActionHandlers.Act_TakePearls, action: ScriptActionKind.TakePearls, executor: NPCSegment.Exec_TakePearls),

            new ScriptKeywordEntry("GIVEITEM", ScriptKeywordKind.Action, ScriptKeywordCategory.Item, "<item> [count]",
                "Give an item to the player.", 2, ScriptActionHandlers.Act_GiveItem, action: ScriptActionKind.GiveItem, executor: NPCSegment.Exec_GiveItem),

            new ScriptKeywordEntry("TAKEITEM", ScriptKeywordKind.Action, ScriptKeywordCategory.Item, "<item> <count> [durability]",
                "Take an item from the player.", 3, ScriptActionHandlers.Act_TakeItem, action: ScriptActionKind.TakeItem, executor: NPCSegment.Exec_TakeItem),

            new ScriptKeywordEntry("GIVEEXP", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<amount>",
                "Give experience to the player.", 2, ScriptActionHandlers.Act_GiveExp, action: ScriptActionKind.GiveExp, executor: NPCSegment.Exec_GiveExp),

            new ScriptKeywordEntry("GIVEPET", ScriptKeywordKind.Action, ScriptKeywordCategory.Pet, "<pet> [count] [level]",
                "Give a pet to the player.", 2, ScriptActionHandlers.Act_GivePet, action: ScriptActionKind.GivePet, executor: NPCSegment.Exec_GivePet),

            new ScriptKeywordEntry("REMOVEPET", ScriptKeywordKind.Action, ScriptKeywordCategory.Pet, "<pet>",
                "Remove a pet from the player.", 2, ScriptActionHandlers.Act_RemovePet, action: ScriptActionKind.RemovePet, executor: NPCSegment.Exec_RemovePet),

            new ScriptKeywordEntry("CLEARPETS", ScriptKeywordKind.Action, ScriptKeywordCategory.Pet, "",
                "Remove every pet the player owns.", 1, ScriptActionHandlers.Act_ClearPets, action: ScriptActionKind.ClearPets, executor: NPCSegment.Exec_ClearPets),

            new ScriptKeywordEntry("GOTO", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<page>",
                "Jump to another page of this script.", 2, ScriptActionHandlers.Act_Goto, action: ScriptActionKind.Goto, executor: NPCSegment.Exec_Goto, button: ScriptButtonKind.Page),

            new ScriptKeywordEntry("CALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<script>",
                "Run a page of another script.", 2, ScriptActionHandlers.Act_Call, action: ScriptActionKind.Call, executor: NPCSegment.Exec_Call),

            new ScriptKeywordEntry("BREAK", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "",
                "Stop processing the rest of the page.", 1, ScriptActionHandlers.Act_Break, action: ScriptActionKind.Break, executor: NPCSegment.Exec_Break),

            new ScriptKeywordEntry("CLOSE", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "",
                "Close the NPC dialog.", 1, ScriptActionHandlers.Act_Close, action: ScriptActionKind.Close, executor: NPCSegment.Exec_Close),

            new ScriptKeywordEntry("ADDNAMELIST", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<list>",
                "Add the player's name to a name list.", 2, ScriptActionHandlers.Act_AddNameList, action: ScriptActionKind.AddNameList, executor: NPCSegment.Exec_AddNameList),

            new ScriptKeywordEntry("ADDGUILDNAMELIST", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<list>",
                "Add the player's guild to a name list.", 2, ScriptActionHandlers.Act_AddGuildNameList, action: ScriptActionKind.AddGuildNameList, executor: NPCSegment.Exec_AddGuildNameList),

            new ScriptKeywordEntry("DELNAMELIST", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<list>",
                "Remove the player's name from a name list.", 2, ScriptActionHandlers.Act_DelNameList, action: ScriptActionKind.DelNameList, executor: NPCSegment.Exec_DelNameList),

            new ScriptKeywordEntry("DELGUILDNAMELIST", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<list>",
                "Remove the player's guild from a name list.", 2, ScriptActionHandlers.Act_DelGuildNameList, action: ScriptActionKind.DelGuildNameList, executor: NPCSegment.Exec_DelGuildNameList),

            new ScriptKeywordEntry("CLEARNAMELIST", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<list>",
                "Empty a name list.", 2, ScriptActionHandlers.Act_ClearNameList, action: ScriptActionKind.ClearNameList, executor: NPCSegment.Exec_ClearNameList),

            new ScriptKeywordEntry("CLEARGUILDNAMELIST", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<list>",
                "Empty a guild name list.", 2, ScriptActionHandlers.Act_ClearGuildNameList, action: ScriptActionKind.ClearGuildNameList, executor: NPCSegment.Exec_ClearGuildNameList),

            new ScriptKeywordEntry("GIVEHP", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<amount>",
                "Restore hit points.", 2, ScriptActionHandlers.Act_GiveHP, action: ScriptActionKind.GiveHP, executor: NPCSegment.Exec_GiveHP),

            new ScriptKeywordEntry("GIVEMP", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<amount>",
                "Restore mana.", 2, ScriptActionHandlers.Act_GiveMP, action: ScriptActionKind.GiveMP, executor: NPCSegment.Exec_GiveMP),

            new ScriptKeywordEntry("CHANGELEVEL", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<level>",
                "Set the player's level.", 2, ScriptActionHandlers.Act_ChangeLevel, action: ScriptActionKind.ChangeLevel, executor: NPCSegment.Exec_ChangeLevel),

            new ScriptKeywordEntry("SETPKPOINT", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<points>",
                "Set the player's PK points.", 2, ScriptActionHandlers.Act_SetPkPoint, action: ScriptActionKind.SetPkPoint, executor: NPCSegment.Exec_SetPkPoint),

            new ScriptKeywordEntry("REDUCEPKPOINT", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<points>",
                "Reduce the player's PK points.", 2, ScriptActionHandlers.Act_ReducePkPoint, action: ScriptActionKind.ReducePkPoint, executor: NPCSegment.Exec_ReducePkPoint),

            new ScriptKeywordEntry("INCREASEPKPOINT", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<points>",
                "Increase the player's PK points.", 2, ScriptActionHandlers.Act_IncreasePkPoint, action: ScriptActionKind.IncreasePkPoint, executor: NPCSegment.Exec_IncreasePkPoint),

            new ScriptKeywordEntry("CHANGEGENDER", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "",
                "Switch the player's gender.", 1, ScriptActionHandlers.Act_ChangeGender, action: ScriptActionKind.ChangeGender, executor: NPCSegment.Exec_ChangeGender),

            new ScriptKeywordEntry("CHANGECLASS", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<class>",
                "Change the player's class.", 2, ScriptActionHandlers.Act_ChangeClass, action: ScriptActionKind.ChangeClass, executor: NPCSegment.Exec_ChangeClass),

            new ScriptKeywordEntry("CHANGEHAIR", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "[hair]",
                "Change the player's hair style.", 1, ScriptActionHandlers.Act_ChangeHair, action: ScriptActionKind.ChangeHair, executor: NPCSegment.Exec_ChangeHair),

            new ScriptKeywordEntry("LOCALMESSAGE", ScriptKeywordKind.Action, ScriptKeywordCategory.Message, "\"<text>\" [channel]",
                "Show a message to players near the NPC.", 1, ScriptActionHandlers.Act_LocalMessage, action: ScriptActionKind.LocalMessage, executor: NPCSegment.Exec_LocalMessage),

            new ScriptKeywordEntry("GLOBALMESSAGE", ScriptKeywordKind.Action, ScriptKeywordCategory.Message, "\"<text>\" [channel]",
                "Show a message to every player.", 1, ScriptActionHandlers.Act_GlobalMessage, action: ScriptActionKind.GlobalMessage, executor: NPCSegment.Exec_GlobalMessage, monsterExecutor: NPCSegment.ExecMonster_GlobalMessage),

            new ScriptKeywordEntry("GIVESKILL", ScriptKeywordKind.Action, ScriptKeywordCategory.Skill, "<skill> [level]",
                "Teach the player a skill.", 3, ScriptActionHandlers.Act_GiveSkill, action: ScriptActionKind.GiveSkill, executor: NPCSegment.Exec_GiveSkill, argument: ScriptArgumentKind.Spell),

            new ScriptKeywordEntry("REMOVESKILL", ScriptKeywordKind.Action, ScriptKeywordCategory.Skill, "<skill>",
                "Remove a skill from the player.", 2, ScriptActionHandlers.Act_RemoveSkill, action: ScriptActionKind.RemoveSkill, executor: NPCSegment.Exec_RemoveSkill, argument: ScriptArgumentKind.Spell),

            new ScriptKeywordEntry("SET", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "[<flag index>] <true|false>",
                "Set a player flag.", 3, ScriptActionHandlers.Act_Set, action: ScriptActionKind.Set, executor: NPCSegment.Exec_Set),

            new ScriptKeywordEntry("PARAM1", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<value> [instance]",
                "Store the first page parameter.", 2, ScriptActionHandlers.Act_Param1, action: ScriptActionKind.Param1, executor: NPCSegment.Exec_Param1),

            new ScriptKeywordEntry("PARAM2", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<value>",
                "Store the second page parameter.", 2, ScriptActionHandlers.Act_Param2, action: ScriptActionKind.Param2, executor: NPCSegment.Exec_Param2),

            new ScriptKeywordEntry("PARAM3", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<value>",
                "Store the third page parameter.", 2, ScriptActionHandlers.Act_Param3, action: ScriptActionKind.Param3, executor: NPCSegment.Exec_Param3),

            new ScriptKeywordEntry("MONGEN", ScriptKeywordKind.Action, ScriptKeywordCategory.World, "<monster> [count]",
                "Spawn monsters on the current map.", 2, ScriptActionHandlers.Act_MonGen, action: ScriptActionKind.Mongen, executor: NPCSegment.Exec_Mongen, monsterExecutor: NPCSegment.ExecMonster_Mongen),

            new ScriptKeywordEntry("TIMERECALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<seconds> [page]",
                "Move the player after a delay.", 2, ScriptActionHandlers.Act_TimeRecall, action: ScriptActionKind.TimeRecall, executor: NPCSegment.Exec_TimeRecall, button: ScriptButtonKind.DelayedPage),

            new ScriptKeywordEntry("TIMERECALLGROUP", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<seconds> [page]",
                "Move the group after a delay.", 2, ScriptActionHandlers.Act_TimeRecallGroup, action: ScriptActionKind.TimeRecallGroup, executor: NPCSegment.Exec_TimeRecallGroup, button: ScriptButtonKind.DelayedPage),

            new ScriptKeywordEntry("BREAKTIMERECALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "",
                "Cancel a pending recall.", 1, ScriptActionHandlers.Act_BreakTimeRecall, action: ScriptActionKind.BreakTimeRecall, executor: NPCSegment.Exec_BreakTimeRecall),

            new ScriptKeywordEntry("DELAYGOTO", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<seconds> <page>",
                "Go to a page after a delay.", 3, ScriptActionHandlers.Act_DelayGoto, action: ScriptActionKind.DelayGoto, executor: NPCSegment.Exec_DelayGoto, button: ScriptButtonKind.DelayedPage),

            new ScriptKeywordEntry("MONCLEAR", ScriptKeywordKind.Action, ScriptKeywordCategory.World, "<map> [instance] [monster]",
                "Remove monsters from a map.", 2, ScriptActionHandlers.Act_MonClear, action: ScriptActionKind.MonClear, executor: NPCSegment.Exec_MonClear, monsterExecutor: NPCSegment.ExecMonster_MonClear),

            new ScriptKeywordEntry("GROUPRECALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Movement, "",
                "Bring every group member to the player.", 1, ScriptActionHandlers.Act_GroupRecall, action: ScriptActionKind.GroupRecall, executor: NPCSegment.Exec_GroupRecall),

            new ScriptKeywordEntry("GROUPTELEPORT", ScriptKeywordKind.Action, ScriptKeywordCategory.Movement, "<map> [instance] [x] [y]",
                "Move the group to a map position.", 2, ScriptActionHandlers.Act_GroupTeleport, action: ScriptActionKind.GroupTeleport, executor: NPCSegment.Exec_GroupTeleport),

            new ScriptKeywordEntry("MOV", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<variable> <value>",
                "Store a value in a script variable.", 3, ScriptActionHandlers.Act_Mov, action: ScriptActionKind.Mov, executor: NPCSegment.Exec_Mov),

            new ScriptKeywordEntry("CALC", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<variable> <operator> <value>",
                "Combine a script variable with a value.", 4, ScriptActionHandlers.Act_Calc, action: ScriptActionKind.Calc, executor: NPCSegment.Exec_Calc),

            new ScriptKeywordEntry("GIVEBUFF", ScriptKeywordKind.Action, ScriptKeywordCategory.Buff, "<buff> <seconds> [value]",
                "Give the player a buff.", 3, ScriptActionHandlers.Act_GiveBuff, action: ScriptActionKind.GiveBuff, executor: NPCSegment.Exec_GiveBuff, monsterExecutor: NPCSegment.ExecMonster_GiveBuff),

            new ScriptKeywordEntry("REMOVEBUFF", ScriptKeywordKind.Action, ScriptKeywordCategory.Buff, "<buff>",
                "Remove a buff from the player.", 2, ScriptActionHandlers.Act_RemoveBuff, action: ScriptActionKind.RemoveBuff, executor: NPCSegment.Exec_RemoveBuff),

            new ScriptKeywordEntry("ADDTOGUILD", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<guild>",
                "Add the player to a guild.", 2, ScriptActionHandlers.Act_AddToGuild, action: ScriptActionKind.AddToGuild, executor: NPCSegment.Exec_AddToGuild),

            new ScriptKeywordEntry("REMOVEFROMGUILD", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "<guild>",
                "Remove the player from a guild.", 2, ScriptActionHandlers.Act_RemoveFromGuild, action: ScriptActionKind.RemoveFromGuild, executor: NPCSegment.Exec_RemoveFromGuild),

            new ScriptKeywordEntry("REFRESHEFFECTS", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "",
                "Refresh the player's visible effects.", 1, ScriptActionHandlers.Act_RefreshEffects, action: ScriptActionKind.RefreshEffects, executor: NPCSegment.Exec_RefreshEffects),

            new ScriptKeywordEntry("CANGAINEXP", ScriptKeywordKind.Action, ScriptKeywordCategory.Player, "<true|false>",
                "Allow or block experience gain.", 2, ScriptActionHandlers.Act_CanGainExp, action: ScriptActionKind.CanGainExp, executor: NPCSegment.Exec_CanGainExp),

            new ScriptKeywordEntry("COMPOSEMAIL", ScriptKeywordKind.Action, ScriptKeywordCategory.Mail, "\"<subject>\" [sender]",
                "Start composing a mail.", 1, ScriptActionHandlers.Act_ComposeMail, action: ScriptActionKind.ComposeMail, executor: NPCSegment.Exec_ComposeMail),

            new ScriptKeywordEntry("ADDMAILGOLD", ScriptKeywordKind.Action, ScriptKeywordCategory.Mail, "<amount>",
                "Attach gold to the composed mail.", 2, ScriptActionHandlers.Act_AddMailGold, action: ScriptActionKind.AddMailGold, executor: NPCSegment.Exec_AddMailGold),

            new ScriptKeywordEntry("ADDMAILITEM", ScriptKeywordKind.Action, ScriptKeywordCategory.Mail, "<item> <count>",
                "Attach an item to the composed mail.", 3, ScriptActionHandlers.Act_AddMailItem, action: ScriptActionKind.AddMailItem, executor: NPCSegment.Exec_AddMailItem),

            new ScriptKeywordEntry("SENDMAIL", ScriptKeywordKind.Action, ScriptKeywordCategory.Mail, "",
                "Send the composed mail.", 1, ScriptActionHandlers.Act_SendMail, action: ScriptActionKind.SendMail, executor: NPCSegment.Exec_SendMail),

            new ScriptKeywordEntry("GROUPGOTO", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<page>",
                "Send the whole group to a page.", 2, ScriptActionHandlers.Act_GroupGoto, action: ScriptActionKind.GroupGoto, executor: NPCSegment.Exec_GroupGoto, button: ScriptButtonKind.Page),

            new ScriptKeywordEntry("ENTERMAP", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "",
                "Enter the map chosen in the map dialog.", 1, ScriptActionHandlers.Act_EnterMap, action: ScriptActionKind.EnterMap, executor: NPCSegment.Exec_EnterMap),

            new ScriptKeywordEntry("MAKEWEDDINGRING", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "",
                "Create a wedding ring for the couple.", 1, ScriptActionHandlers.Act_MakeWeddingRing, action: ScriptActionKind.MakeWeddingRing, executor: NPCSegment.Exec_MakeWeddingRing),

            new ScriptKeywordEntry("FORCEDIVORCE", ScriptKeywordKind.Action, ScriptKeywordCategory.Social, "",
                "Divorce the player from their partner.", 1, ScriptActionHandlers.Act_ForceDivorce, action: ScriptActionKind.ForceDivorce, executor: NPCSegment.Exec_ForceDivorce),

            new ScriptKeywordEntry("LOADVALUE", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<variable> \"<file>\" <group> <key>",
                "Read a stored value into a variable.", 5, ScriptActionHandlers.Act_LoadValue, action: ScriptActionKind.LoadValue, executor: NPCSegment.Exec_LoadValue, monsterExecutor: NPCSegment.ExecMonster_LoadValue),

            new ScriptKeywordEntry("SAVEVALUE", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<variable> \"<file>\" <group> <key>",
                "Write a variable into the value file.", 5, ScriptActionHandlers.Act_SaveValue, action: ScriptActionKind.SaveValue, executor: NPCSegment.Exec_SaveValue, monsterExecutor: NPCSegment.ExecMonster_SaveValue),

            new ScriptKeywordEntry("CONQUESTGUARD", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<conquest> <guard>",
                "Repair a conquest guard.", 3, ScriptActionHandlers.Act_ConquestGuard, action: ScriptActionKind.ConquestGuard, executor: NPCSegment.Exec_ConquestGuard),

            new ScriptKeywordEntry("CONQUESTGATE", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<conquest> <gate>",
                "Repair a conquest gate.", 3, ScriptActionHandlers.Act_ConquestGate, action: ScriptActionKind.ConquestGate, executor: NPCSegment.Exec_ConquestGate),

            new ScriptKeywordEntry("CONQUESTWALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<conquest> <wall>",
                "Repair a conquest wall.", 3, ScriptActionHandlers.Act_ConquestWall, action: ScriptActionKind.ConquestWall, executor: NPCSegment.Exec_ConquestWall),

            new ScriptKeywordEntry("TAKECONQUESTGOLD", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<amount>",
                "Take gold from the conquest treasury.", 2, ScriptActionHandlers.Act_TakeConquestGold, action: ScriptActionKind.TakeConquestGold, executor: NPCSegment.Exec_TakeConquestGold),

            new ScriptKeywordEntry("SETCONQUESTRATE", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<rate> <value>",
                "Set a conquest rate.", 3, ScriptActionHandlers.Act_SetConquestRate, action: ScriptActionKind.SetConquestRate, executor: NPCSegment.Exec_SetConquestRate),

            new ScriptKeywordEntry("STARTCONQUEST", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<conquest>",
                "Start a conquest war.", 2, ScriptActionHandlers.Act_StartConquest, action: ScriptActionKind.StartConquest, executor: NPCSegment.Exec_StartConquest),

            new ScriptKeywordEntry("SCHEDULECONQUEST", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<conquest>",
                "Schedule a conquest war.", 2, ScriptActionHandlers.Act_ScheduleConquest, action: ScriptActionKind.ScheduleConquest, executor: NPCSegment.Exec_ScheduleConquest),

            new ScriptKeywordEntry("OPENGATE", ScriptKeywordKind.Action, ScriptKeywordCategory.World, "<conquest> <gate>",
                "Open a conquest gate.", 3, ScriptActionHandlers.Act_OpenGate, action: ScriptActionKind.OpenGate, executor: NPCSegment.Exec_OpenGate),

            new ScriptKeywordEntry("CLOSEGATE", ScriptKeywordKind.Action, ScriptKeywordCategory.World, "<conquest> <gate>",
                "Close a conquest gate.", 3, ScriptActionHandlers.Act_CloseGate, action: ScriptActionKind.CloseGate, executor: NPCSegment.Exec_CloseGate),

            new ScriptKeywordEntry("OPENBROWSER", ScriptKeywordKind.Action, ScriptKeywordCategory.Message, "<url>",
                "Open a web page for the player.", 2, ScriptActionHandlers.Act_OpenBrowser, action: ScriptActionKind.OpenBrowser, executor: NPCSegment.Exec_OpenBrowser),

            new ScriptKeywordEntry("GETRANDOMTEXT", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<variable> <file>",
                "Store a random line of a file in a variable.", 3, ScriptActionHandlers.Act_GetRandomText, action: ScriptActionKind.GetRandomText, executor: NPCSegment.Exec_GetRandomText),

            new ScriptKeywordEntry("PLAYSOUND", ScriptKeywordKind.Action, ScriptKeywordCategory.Message, "<sound>",
                "Play a sound for the player.", 2, ScriptActionHandlers.Act_PlaySound, action: ScriptActionKind.PlaySound, executor: NPCSegment.Exec_PlaySound),

            new ScriptKeywordEntry("SETTIMER", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<key> <seconds> <type> [global]",
                "Start or update a timer.", 4, ScriptActionHandlers.Act_SetTimer, action: ScriptActionKind.SetTimer, executor: NPCSegment.Exec_SetTimer),

            new ScriptKeywordEntry("EXPIRETIMER", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<key>",
                "Expire a timer.", 2, ScriptActionHandlers.Act_ExpireTimer, action: ScriptActionKind.ExpireTimer, executor: NPCSegment.Exec_ExpireTimer),

            new ScriptKeywordEntry("UNEQUIPITEM", ScriptKeywordKind.Action, ScriptKeywordCategory.Item, "[slot]",
                "Unequip an item slot.", 1, ScriptActionHandlers.Act_UnequipItem, action: ScriptActionKind.UnequipItem, executor: NPCSegment.Exec_UnequipItem),

            new ScriptKeywordEntry("ROLLDIE", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<page> [autoroll]",
                "Roll a six-sided die.", 3, ScriptActionHandlers.Act_RollDie, action: ScriptActionKind.RollDie, executor: NPCSegment.Exec_RollDie, button: ScriptButtonKind.Roll),

            new ScriptKeywordEntry("ROLLYUT", ScriptKeywordKind.Action, ScriptKeywordCategory.Flow, "<page> [autoroll]",
                "Play a round of yut.", 3, ScriptActionHandlers.Act_RollYut, action: ScriptActionKind.RollYut, executor: NPCSegment.Exec_RollYut, button: ScriptButtonKind.Roll),

            new ScriptKeywordEntry("DROP", ScriptKeywordKind.Action, ScriptKeywordCategory.Item, "<drop file>",
                "Drop items from a drop file.", 2, ScriptActionHandlers.Act_Drop, action: ScriptActionKind.Drop, executor: NPCSegment.Exec_Drop),

            new ScriptKeywordEntry("REVIVEHERO", ScriptKeywordKind.Action, ScriptKeywordCategory.Hero, "",
                "Revive the player's hero.", 1, ScriptActionHandlers.Act_ReviveHero, action: ScriptActionKind.ReviveHero, executor: NPCSegment.Exec_ReviveHero),

            new ScriptKeywordEntry("SEALHERO", ScriptKeywordKind.Action, ScriptKeywordCategory.Hero, "",
                "Seal the player's hero.", 1, ScriptActionHandlers.Act_SealHero, action: ScriptActionKind.SealHero, executor: NPCSegment.Exec_SealHero),

            new ScriptKeywordEntry("DELETEHERO", ScriptKeywordKind.Action, ScriptKeywordCategory.Hero, "",
                "Delete the player's hero.", 1, ScriptActionHandlers.Act_DeleteHero, action: ScriptActionKind.DeleteHero, executor: NPCSegment.Exec_DeleteHero),

            new ScriptKeywordEntry("CONQUESTREPAIRALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Conquest, "<conquest>",
                "Repair every structure of a conquest.", 2, ScriptActionHandlers.Act_ConquestRepairAll, action: ScriptActionKind.ConquestRepairAll, executor: NPCSegment.Exec_ConquestRepairAll),

            new ScriptKeywordEntry("BUYGT", ScriptKeywordKind.Action, ScriptKeywordCategory.Guild, "",
                "Buy the guild territory.", 1, ScriptActionHandlers.Act_BuyGT, action: ScriptActionKind.BuyGT, executor: NPCSegment.Exec_BuyGT),

            new ScriptKeywordEntry("TELEPORTGT", ScriptKeywordKind.Action, ScriptKeywordCategory.Movement, "",
                "Teleport to the guild territory.", 1, ScriptActionHandlers.Act_TeleportGT, action: ScriptActionKind.TeleportGT, executor: NPCSegment.Exec_TeleportGT),

            new ScriptKeywordEntry("EXTENDGT", ScriptKeywordKind.Action, ScriptKeywordCategory.Guild, "",
                "Extend the territory rental.", 1, ScriptActionHandlers.Act_ExtendGT, action: ScriptActionKind.ExtendGT, executor: NPCSegment.Exec_ExtendGT),

            new ScriptKeywordEntry("GTRECALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Guild, "<player>",
                "Summon a guild member to the territory.", 2, ScriptActionHandlers.Act_GTRecall, action: ScriptActionKind.GTRecall, executor: NPCSegment.Exec_GTRecall),

            new ScriptKeywordEntry("GTALLRECALL", ScriptKeywordKind.Action, ScriptKeywordCategory.Guild, "",
                "Summon every guild member to the territory.", 1, ScriptActionHandlers.Act_GTAllRecall, action: ScriptActionKind.GTAllRecall, executor: NPCSegment.Exec_GTAllRecall),

            new ScriptKeywordEntry("GTSALE", ScriptKeywordKind.Action, ScriptKeywordCategory.Guild, "<price>",
                "Offer the territory for sale.", 2, ScriptActionHandlers.Act_GTSale, action: ScriptActionKind.GTSale, executor: NPCSegment.Exec_GTSale),

            new ScriptKeywordEntry("CANCELGTSALE", ScriptKeywordKind.Action, ScriptKeywordCategory.Guild, "",
                "Cancel the territory sale.", 1, ScriptActionHandlers.Act_CancelGTSale, action: ScriptActionKind.GTCancelSale, executor: NPCSegment.Exec_GTCancelSale),

            new ScriptKeywordEntry("HEROGIVESKILL", ScriptKeywordKind.Action, ScriptKeywordCategory.Hero, "<skill> [level]",
                "Teach the hero a skill.", 3, ScriptActionHandlers.Act_HeroGiveSkill, action: ScriptActionKind.HeroGiveSkill, executor: NPCSegment.Exec_HeroGiveSkill, argument: ScriptArgumentKind.Spell),

            new ScriptKeywordEntry("HEROREMOVESKILL", ScriptKeywordKind.Action, ScriptKeywordCategory.Hero, "<skill>",
                "Remove a skill from the hero.", 2, ScriptActionHandlers.Act_HeroRemoveSkill, action: ScriptActionKind.HeroRemoveSkill, executor: NPCSegment.Exec_HeroRemoveSkill),
        };

        private static readonly Dictionary<string, ScriptKeywordEntry> Index = BuildIndex();
        private static readonly Dictionary<ScriptActionKind, ScriptKeywordEntry> IndexByKind = BuildKindIndex();

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

        /// <summary>Action for a name as written in the script, or null.</summary>
        public static ScriptKeywordEntry Find(string name)
        {
            ScriptKeywordEntry entry;

            return name != null && Index.TryGetValue(name.Trim(), out entry) ? entry : null;
        }


        private static Dictionary<ScriptActionKind, ScriptKeywordEntry> BuildKindIndex()
        {
            var index = new Dictionary<ScriptActionKind, ScriptKeywordEntry>();

            foreach (ScriptKeywordEntry entry in Entries)
            {
                if (!entry.Action.HasValue) continue;

                index[entry.Action.Value] = entry;
            }

            return index;
        }        /// <summary>Action for a kind, so the executor can be read from the same row.</summary>
        public static ScriptKeywordEntry Find(ScriptActionKind kind)
        {
            ScriptKeywordEntry entry;

            return IndexByKind.TryGetValue(kind, out entry) ? entry : null;
        }
    }
}
