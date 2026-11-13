namespace Server.MirObjects
{
    /// <summary>
    /// The &lt;$VALUE&gt; table used inside #SAY. Variables are not dispatched, so they carry no
    /// handler; the table exists for validation and for the editor's insertion list.
    /// </summary>
    public static class ScriptValueCatalog
    {
        /// <summary>Values available when the script runs for a player.</summary>
        public static readonly ScriptKeyword[] PlayerEntries =
        {
            new ScriptKeyword("MONSTERCOUNT()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(map)",
                "Number of monsters on a map.", 1, null, valueExecutor: NPCSegment.ExecValue_MONSTERCOUNT),

            new ScriptKeyword("CONQUESTGUARD()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, guard index)",
                "Name and repair cost of a conquest guard.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTGUARD),

            new ScriptKeyword("CONQUESTGATE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, gate index)",
                "Name and repair cost of a conquest gate.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTGATE),

            new ScriptKeyword("CONQUESTWALL()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, wall index)",
                "Name and repair cost of a conquest wall.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTWALL),

            new ScriptKeyword("CONQUESTSIEGE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, gate index)",
                "Name and repair cost of a siege gate.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTSIEGE),

            new ScriptKeyword("CONQUESTOWNER()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "Guild that owns a conquest.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTOWNER),

            new ScriptKeyword("CONQUESTGOLD()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "Gold stored by a conquest.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTGOLD),

            new ScriptKeyword("CONQUESTRATE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "NPC rate of a conquest.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTRATE),

            new ScriptKeyword("CONQUESTSCHEDULE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "War scheduled for a conquest.", 1, null, valueExecutor: NPCSegment.ExecValue_CONQUESTSCHEDULE),

            new ScriptKeyword("CASTLEWARDATE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Date of the next castle war.", 1, null, valueExecutor: NPCSegment.ExecValue_CASTLEWARDATE),

            new ScriptKeyword("OUTPUT()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(variable)",
                "Value of a script variable.", 1, null, valueExecutor: NPCSegment.ExecValue_OUTPUT),

            new ScriptKeyword("NPCNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the NPC the player is talking to.", 1, null, valueExecutor: NPCSegment.ExecValue_NPCNAME),

            new ScriptKeyword("USERNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the player.", 1, null, valueExecutor: NPCSegment.ExecValue_USERNAME),

            new ScriptKeyword("LEVEL", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Player level.", 1, null, valueExecutor: NPCSegment.ExecValue_LEVEL),

            new ScriptKeyword("CLASS", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Player class name.", 1, null, valueExecutor: NPCSegment.ExecValue_CLASS),

            new ScriptKeyword("MAP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map file name.", 1, null, valueExecutor: NPCSegment.ExecValue_MAP),

            new ScriptKeyword("X_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map X coordinate.", 1, null, valueExecutor: NPCSegment.ExecValue_X_COORD),

            new ScriptKeyword("Y_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map Y coordinate.", 1, null, valueExecutor: NPCSegment.ExecValue_Y_COORD),

            new ScriptKeyword("HP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current hit points.", 1, null, valueExecutor: NPCSegment.ExecValue_HP),

            new ScriptKeyword("MAXHP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Maximum hit points.", 1, null, valueExecutor: NPCSegment.ExecValue_MAXHP),

            new ScriptKeyword("MP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current mana.", 1, null, valueExecutor: NPCSegment.ExecValue_MP),

            new ScriptKeyword("MAXMP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Maximum mana.", 1, null, valueExecutor: NPCSegment.ExecValue_MAXMP),

            new ScriptKeyword("GAMEGOLD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Gold the player carries.", 1, null, valueExecutor: NPCSegment.ExecValue_GAMEGOLD),

            new ScriptKeyword("CREDIT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Credit the player holds.", 1, null, valueExecutor: NPCSegment.ExecValue_CREDIT),

            new ScriptKeyword("ARMOUR", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped armour.", 1, null, valueExecutor: NPCSegment.ExecValue_ARMOUR),

            new ScriptKeyword("WEAPON", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped weapon.", 1, null, valueExecutor: NPCSegment.ExecValue_USERWEAPON),

            new ScriptKeyword("USERWEAPON", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped weapon, older alias of WEAPON.", 1, null, valueExecutor: NPCSegment.ExecValue_USERWEAPON),

            new ScriptKeyword("RING_L", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the left ring.", 1, null, valueExecutor: NPCSegment.ExecValue_RING_L),

            new ScriptKeyword("RING_R", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the right ring.", 1, null, valueExecutor: NPCSegment.ExecValue_RING_R),

            new ScriptKeyword("BRACELET_L", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the left bracelet.", 1, null, valueExecutor: NPCSegment.ExecValue_BRACELET_L),

            new ScriptKeyword("BRACELET_R", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the right bracelet.", 1, null, valueExecutor: NPCSegment.ExecValue_BRACELET_R),

            new ScriptKeyword("NECKLACE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the necklace.", 1, null, valueExecutor: NPCSegment.ExecValue_NECKLACE),

            new ScriptKeyword("BELT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the belt.", 1, null, valueExecutor: NPCSegment.ExecValue_BELT),

            new ScriptKeyword("BOOTS", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the boots.", 1, null, valueExecutor: NPCSegment.ExecValue_BOOTS),

            new ScriptKeyword("HELMET", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the helmet.", 1, null, valueExecutor: NPCSegment.ExecValue_HELMET),

            new ScriptKeyword("AMULET", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the amulet.", 1, null, valueExecutor: NPCSegment.ExecValue_AMULET),

            new ScriptKeyword("STONE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped stone, or the empty text.", 1, null, valueExecutor: NPCSegment.ExecValue_STONE),

            new ScriptKeyword("TORCH", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped torch, or the empty text.", 1, null, valueExecutor: NPCSegment.ExecValue_TORCH),

            new ScriptKeyword("DATE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current server date.", 1, null, valueExecutor: NPCSegment.ExecValue_DATE),

            new ScriptKeyword("MAPLIGHT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map light setting.", 1, null, valueExecutor: NPCSegment.ExecValue_MAPLIGHT),

            new ScriptKeyword("USERCOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Number of players online.", 1, null, valueExecutor: NPCSegment.ExecValue_USERCOUNT),

            new ScriptKeyword("PKPOINT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Player PK points.", 1, null, valueExecutor: NPCSegment.ExecValue_PKPOINT),

            new ScriptKeyword("GUILDWARTIME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Length of a guild war.", 1, null, valueExecutor: NPCSegment.ExecValue_GUILDWARTIME),

            new ScriptKeyword("GUILDWARFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Cost of starting a guild war.", 1, null, valueExecutor: NPCSegment.ExecValue_GUILDWARFEE),

            new ScriptKeyword("PARCELAMOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Number of parcels waiting to be collected.", 1, null, valueExecutor: NPCSegment.ExecValue_PARCELAMOUNT),

            new ScriptKeyword("GUILDNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the player's guild.", 1, null, valueExecutor: NPCSegment.ExecValue_GUILDNAME),

            new ScriptKeyword("ROLLRESULT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Result of the last die roll.", 1, null, valueExecutor: NPCSegment.ExecValue_ROLLRESULT),

            new ScriptKeyword("MOUNTLOYALTY", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Loyalty of the equipped mount.", 1, null, valueExecutor: NPCSegment.ExecValue_MOUNTLOYALTY),

            new ScriptKeyword("MOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped mount.", 1, null, valueExecutor: NPCSegment.ExecValue_MOUNT),

            new ScriptKeyword("GUILDEXTENDFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Cost of extending the guild territory.", 1, null, valueExecutor: NPCSegment.ExecValue_GUILDEXTENDFEE),

            new ScriptKeyword("GUILDRENTFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Rent of the guild territory.", 1, null, valueExecutor: NPCSegment.ExecValue_GUILDRENTFEE),

            new ScriptKeyword("GUILDGTRENTALDAYSLEFT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Days left on the guild territory rental.", 1, null, valueExecutor: NPCSegment.ExecValue_GUILDGTRENTALDAYSLEFT),

            new ScriptKeyword("AGITGUILDNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Guild that owns the territory.", 1, null, valueExecutor: NPCSegment.ExecValue_AGITGUILDNAME),
        };

        /// <summary>Values available when the script runs for a monster.</summary>
        public static readonly ScriptKeyword[] MonsterEntries =
        {
            new ScriptKeyword("USERNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the monster.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_USERNAME),

            new ScriptKeyword("LEVEL", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Monster level.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_LEVEL),

            new ScriptKeyword("MAP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Map file name the monster stands on.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_MAP),

            new ScriptKeyword("X_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Map X coordinate of the monster.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_X_COORD),

            new ScriptKeyword("Y_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Map Y coordinate of the monster.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_Y_COORD),

            new ScriptKeyword("HP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current hit points of the monster.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_HP),

            new ScriptKeyword("MAXHP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Maximum hit points of the monster.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_MAXHP),

            new ScriptKeyword("DATE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current server date.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_DATE),

            new ScriptKeyword("MAPLIGHT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map light setting.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_MAPLIGHT),

            new ScriptKeyword("USERCOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Number of players online.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_USERCOUNT),

            new ScriptKeyword("GUILDWARTIME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Length of a guild war.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_GUILDWARTIME),
            new ScriptKeyword("OUTPUT()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(variable)",
                "Value of a script variable.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_OUTPUT),

            new ScriptKeyword("GUILDWARFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Cost of starting a guild war.", 1, null, valueExecutor: NPCSegment.ExecMonsterValue_GUILDWARFEE),
        };
    }
}
