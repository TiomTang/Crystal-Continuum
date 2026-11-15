namespace Server.MirObjects
{
    /// <summary>
    /// The &lt;$VALUE&gt; table used inside #SAY. Variables are not dispatched, so they carry no
    /// handler; the table exists for validation and for the editor's insertion list.
    /// </summary>
    public static class ScriptValueCatalog
    {
        /// <summary>Values available when the script runs for a player.</summary>
        public static readonly ScriptKeywordEntry[] PlayerEntries =
        {
            new ScriptKeywordEntry("MONSTERCOUNT()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(map)",
                "Number of monsters on a map.", 1, null, valueExecutor: ScriptSegment.ExecValue_MONSTERCOUNT),

            new ScriptKeywordEntry("CONQUESTGUARD()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, guard index)",
                "Name and repair cost of a conquest guard.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTGUARD),

            new ScriptKeywordEntry("CONQUESTGATE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, gate index)",
                "Name and repair cost of a conquest gate.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTGATE),

            new ScriptKeywordEntry("CONQUESTWALL()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, wall index)",
                "Name and repair cost of a conquest wall.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTWALL),

            new ScriptKeywordEntry("CONQUESTSIEGE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index, siege index)",
                "Name and repair cost of a conquest siege structure.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTSIEGE),

            new ScriptKeywordEntry("CONQUESTOWNER()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "Guild that owns a conquest.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTOWNER),

            new ScriptKeywordEntry("CONQUESTGOLD()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "Gold stored by a conquest.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTGOLD),

            new ScriptKeywordEntry("CONQUESTRATE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "NPC rate of a conquest.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTRATE),

            new ScriptKeywordEntry("CONQUESTSCHEDULE()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(conquest index)",
                "Attacking guild scheduled for a conquest, or its scheduling status.", 1, null, valueExecutor: ScriptSegment.ExecValue_CONQUESTSCHEDULE),

            new ScriptKeywordEntry("CASTLEWARDATE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Date of the next castle war.", 1, null, valueExecutor: ScriptSegment.ExecValue_CASTLEWARDATE),

            new ScriptKeywordEntry("OUTPUT()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(variable)",
                "Value of a script variable.", 1, null, valueExecutor: ScriptSegment.ExecValue_OUTPUT),

            new ScriptKeywordEntry("NPCNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the NPC the player is talking to.", 1, null, valueExecutor: ScriptSegment.ExecValue_NPCNAME),

            new ScriptKeywordEntry("USERNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the player.", 1, null, valueExecutor: ScriptSegment.ExecValue_USERNAME),

            new ScriptKeywordEntry("LEVEL", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Player level.", 1, null, valueExecutor: ScriptSegment.ExecValue_LEVEL),

            new ScriptKeywordEntry("CLASS", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Player class enum name.", 1, null, valueExecutor: ScriptSegment.ExecValue_CLASS),

            new ScriptKeywordEntry("MAP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map file name.", 1, null, valueExecutor: ScriptSegment.ExecValue_MAP),

            new ScriptKeywordEntry("X_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map X coordinate.", 1, null, valueExecutor: ScriptSegment.ExecValue_X_COORD),

            new ScriptKeywordEntry("Y_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current map Y coordinate.", 1, null, valueExecutor: ScriptSegment.ExecValue_Y_COORD),

            new ScriptKeywordEntry("HP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current hit points.", 1, null, valueExecutor: ScriptSegment.ExecValue_HP),

            new ScriptKeywordEntry("MAXHP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Maximum hit points.", 1, null, valueExecutor: ScriptSegment.ExecValue_MAXHP),

            new ScriptKeywordEntry("MP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current mana.", 1, null, valueExecutor: ScriptSegment.ExecValue_MP),

            new ScriptKeywordEntry("MAXMP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Maximum mana.", 1, null, valueExecutor: ScriptSegment.ExecValue_MAXMP),

            new ScriptKeywordEntry("GAMEGOLD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Gold the player carries.", 1, null, valueExecutor: ScriptSegment.ExecValue_GAMEGOLD),

            new ScriptKeywordEntry("CREDIT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Credit the player holds.", 1, null, valueExecutor: ScriptSegment.ExecValue_CREDIT),

            new ScriptKeywordEntry("ARMOUR", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped armour.", 1, null, valueExecutor: ScriptSegment.ExecValue_ARMOUR),

            new ScriptKeywordEntry("WEAPON", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped weapon.", 1, null, valueExecutor: ScriptSegment.ExecValue_USERWEAPON),

            new ScriptKeywordEntry("USERWEAPON", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped weapon, older alias of WEAPON.", 1, null, valueExecutor: ScriptSegment.ExecValue_USERWEAPON),

            new ScriptKeywordEntry("RING_L", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the left ring.", 1, null, valueExecutor: ScriptSegment.ExecValue_RING_L),

            new ScriptKeywordEntry("RING_R", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the right ring.", 1, null, valueExecutor: ScriptSegment.ExecValue_RING_R),

            new ScriptKeywordEntry("BRACELET_L", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the left bracelet.", 1, null, valueExecutor: ScriptSegment.ExecValue_BRACELET_L),

            new ScriptKeywordEntry("BRACELET_R", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the right bracelet.", 1, null, valueExecutor: ScriptSegment.ExecValue_BRACELET_R),

            new ScriptKeywordEntry("NECKLACE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the necklace.", 1, null, valueExecutor: ScriptSegment.ExecValue_NECKLACE),

            new ScriptKeywordEntry("BELT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the belt.", 1, null, valueExecutor: ScriptSegment.ExecValue_BELT),

            new ScriptKeywordEntry("BOOTS", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the boots.", 1, null, valueExecutor: ScriptSegment.ExecValue_BOOTS),

            new ScriptKeywordEntry("HELMET", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the helmet.", 1, null, valueExecutor: ScriptSegment.ExecValue_HELMET),

            new ScriptKeywordEntry("AMULET", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the amulet.", 1, null, valueExecutor: ScriptSegment.ExecValue_AMULET),

            new ScriptKeywordEntry("STONE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped stone, or a no-stone message.", 1, null, valueExecutor: ScriptSegment.ExecValue_STONE),

            new ScriptKeywordEntry("TORCH", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped torch, or a no-torch message.", 1, null, valueExecutor: ScriptSegment.ExecValue_TORCH),

            new ScriptKeywordEntry("DATE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current server date.", 1, null, valueExecutor: ScriptSegment.ExecValue_DATE),

            new ScriptKeywordEntry("MAPLIGHT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current server-wide light setting.", 1, null, valueExecutor: ScriptSegment.ExecValue_MAPLIGHT),

            new ScriptKeywordEntry("USERCOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Number of players online.", 1, null, valueExecutor: ScriptSegment.ExecValue_USERCOUNT),

            new ScriptKeywordEntry("PKPOINT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Player PK points.", 1, null, valueExecutor: ScriptSegment.ExecValue_PKPOINT),

            new ScriptKeywordEntry("GUILDWARTIME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Length of a guild war.", 1, null, valueExecutor: ScriptSegment.ExecValue_GUILDWARTIME),

            new ScriptKeywordEntry("GUILDWARFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Cost of starting a guild war.", 1, null, valueExecutor: ScriptSegment.ExecValue_GUILDWARFEE),

            new ScriptKeywordEntry("PARCELAMOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Number of parcels waiting to be collected.", 1, null, valueExecutor: ScriptSegment.ExecValue_PARCELAMOUNT),

            new ScriptKeywordEntry("GUILDNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the player's guild.", 1, null, valueExecutor: ScriptSegment.ExecValue_GUILDNAME),

            new ScriptKeywordEntry("ROLLRESULT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Result of the last die roll.", 1, null, valueExecutor: ScriptSegment.ExecValue_ROLLRESULT),

            new ScriptKeywordEntry("MOUNTLOYALTY", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current and maximum raw durability of the equipped mount, or a no-mount message.", 1, null, valueExecutor: ScriptSegment.ExecValue_MOUNTLOYALTY),

            new ScriptKeywordEntry("MOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the equipped mount.", 1, null, valueExecutor: ScriptSegment.ExecValue_MOUNT),

            new ScriptKeywordEntry("GUILDEXTENDFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Guild territory expiry and extension fee, or a none message.", 1, null, valueExecutor: ScriptSegment.ExecValue_GUILDEXTENDFEE),

            new ScriptKeywordEntry("GUILDRENTFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Rent of the guild territory.", 1, null, valueExecutor: ScriptSegment.ExecValue_GUILDRENTFEE),

            new ScriptKeywordEntry("GUILDGTRENTALDAYSLEFT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Days left on the guild territory rental.", 1, null, valueExecutor: ScriptSegment.ExecValue_GUILDGTRENTALDAYSLEFT),

            new ScriptKeywordEntry("AGITGUILDNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the player's guild, or a no-guild message.", 1, null, valueExecutor: ScriptSegment.ExecValue_AGITGUILDNAME),
        };

        /// <summary>Values available when the script runs for a monster.</summary>
        public static readonly ScriptKeywordEntry[] MonsterEntries =
        {
            new ScriptKeywordEntry("USERNAME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Name of the monster.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_USERNAME),

            new ScriptKeywordEntry("LEVEL", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Monster level.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_LEVEL),

            new ScriptKeywordEntry("MAP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Map file name the monster stands on.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_MAP),

            new ScriptKeywordEntry("X_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Map X coordinate of the monster.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_X_COORD),

            new ScriptKeywordEntry("Y_COORD", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Map Y coordinate of the monster.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_Y_COORD),

            new ScriptKeywordEntry("HP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current hit points of the monster.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_HP),

            new ScriptKeywordEntry("MAXHP", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Maximum hit points of the monster.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_MAXHP),

            new ScriptKeywordEntry("DATE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current server date.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_DATE),

            new ScriptKeywordEntry("MAPLIGHT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Current server-wide light setting.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_MAPLIGHT),

            new ScriptKeywordEntry("USERCOUNT", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Number of players online.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_USERCOUNT),

            new ScriptKeywordEntry("GUILDWARTIME", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Length of a guild war.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_GUILDWARTIME),
            new ScriptKeywordEntry("OUTPUT()", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "(variable)",
                "Value of a script variable.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_OUTPUT),

            new ScriptKeywordEntry("GUILDWARFEE", ScriptKeywordKind.Variable, ScriptKeywordCategory.Variable, "",
                "Cost of starting a guild war.", 1, null, valueExecutor: ScriptSegment.ExecMonsterValue_GUILDWARFEE),
        };

        private static readonly Dictionary<string, ScriptKeywordEntry> PlayerIndex = BuildIndex(PlayerEntries);
        private static readonly Dictionary<string, ScriptKeywordEntry> MonsterIndex = BuildIndex(MonsterEntries);

        private static Dictionary<string, ScriptKeywordEntry> BuildIndex(ScriptKeywordEntry[] entries)
        {
            var index = new Dictionary<string, ScriptKeywordEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptKeywordEntry entry in entries)
            {
                if (index.ContainsKey(entry.Name))
                {
#if DEBUG
                    throw new InvalidOperationException("Duplicate NPC script variable: " + entry.Name);
#else
                    continue;
#endif
                }

                index.Add(entry.Name, entry);
            }

            return index;
        }

        /// <summary>Value replaced for a player, or null when the name is unknown.</summary>
        public static ScriptKeywordEntry FindPlayer(string name)
        {
            ScriptKeywordEntry entry;

            return name != null && PlayerIndex.TryGetValue(name.Trim(), out entry) ? entry : null;
        }

        /// <summary>Value replaced when the script runs for a monster, or null.</summary>
        public static ScriptKeywordEntry FindMonster(string name)
        {
            ScriptKeywordEntry entry;

            return name != null && MonsterIndex.TryGetValue(name.Trim(), out entry) ? entry : null;
        }    }
}
