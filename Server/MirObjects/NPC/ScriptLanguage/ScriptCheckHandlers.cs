using System.IO;
using System.Text.RegularExpressions;

namespace Server.MirObjects
{
    /// <summary>
    /// Handlers for the #IF keyword table. The registry checks the minimum argument count before
    /// calling these, so each handler can read the parts it needs without repeating that guard.
    /// </summary>
    public static class ScriptCheckHandlers
    {
        private static readonly Regex FlagRegex = new Regex(@"\[(.*?)\]", RegexOptions.Compiled);
        private static readonly Regex QuoteRegex = new Regex("\"([^\"]*)\"", RegexOptions.Compiled);

        // ---------------------------------------------------------------- player

        public static ScriptParseOutcome Check_Level(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.Level, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Gold(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckGold, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Credit(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckCredit, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Gender(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckGender, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Class(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckClass, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_PkPoint(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckPkPoint, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Flag(ScriptParseContext ctx)
        {
            Match match = FlagRegex.Match(ctx.Parts[1]);

            if (match.Success)
            {
                string flagIndex = match.Groups[1].Captures[0].Value;
                ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.Check, flagIndex, ctx.Parts[2]));
            }

            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_IsNewHuman(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.IsNewHuman));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_IsAdmin(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.IsAdmin));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- items

        public static ScriptParseOutcome Check_Item(ScriptParseContext ctx)
        {
            string count = ctx.Parts.Length < 3 ? "1" : ctx.Parts[2];
            string durability = ctx.Parts.Length > 3 ? ctx.Parts[3] : string.Empty;

            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckItem, ctx.Parts[1], count, durability));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_BagSpace(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.HasBagSpace, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- name lists

        public static ScriptParseOutcome Check_NameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptCheckKind.CheckNameList);
        }

        public static ScriptParseOutcome Check_GuildNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptCheckKind.CheckGuildNameList);
        }

        private static ScriptParseOutcome AddNameList(ScriptParseContext ctx, ScriptCheckKind type)
        {
            string listPath = ctx.Parts[1];
            Match quoteMatch = QuoteRegex.Match(ctx.Line);

            if (quoteMatch.Success)
                listPath = quoteMatch.Groups[1].Captures[0].Value;

            string fileName = Settings.GetNameListPath(listPath);
            string directory = Path.GetDirectoryName(fileName);

            Directory.CreateDirectory(directory);

            ctx.Segment.CheckList.Add(new NPCChecks(type, fileName));

            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- social

        public static ScriptParseOutcome Check_Relationship(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckRelationship));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_WeddingRing(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckWeddingRing));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- guild

        public static ScriptParseOutcome Check_GuildGold(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckGuildGold, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_InGuild(ScriptParseContext ctx)
        {
            string guildName = string.Empty;

            if (ctx.Parts.Length > 1) guildName = ctx.Parts[1];

            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.InGuild, guildName));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Permission(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckPermission, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HasGT(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.HasGT));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordGuard(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.AffordGuard, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordGate(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.AffordGate, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordWall(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.AffordWall, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordSiege(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.AffordSiege, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- group

        public static ScriptParseOutcome Check_GroupLeader(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.Groupleader));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_GroupCount(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.GroupCount, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_GroupNearby(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.GroupCheckNearby));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- pets

        public static ScriptParseOutcome Check_PetCount(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.PetCount, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_PetLevel(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.PetLevel, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Pet(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckPet, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- hero

        public static ScriptParseOutcome Check_HeroLevel(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.HeroLevel, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HeroClass(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckHeroClass, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HeroGender(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckHeroGender, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HeroItem(ScriptParseContext ctx)
        {
            string count = ctx.Parts.Length < 3 ? "1" : ctx.Parts[2];
            string durability = ctx.Parts.Length > 3 ? ctx.Parts[3] : string.Empty;

            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckHeroItem, ctx.Parts[1], count, durability));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- quests

        public static ScriptParseOutcome Check_Quest(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckQuest, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- world

        public static ScriptParseOutcome Check_Range(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckRange, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Map(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckMap, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_MapLight(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckMapLight, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Hum(ScriptParseContext ctx)
        {
            string instance = ctx.Parts.Length < 5 ? "1" : ctx.Parts[4];

            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckHum, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], instance));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Mon(ScriptParseContext ctx)
        {
            string instance = ctx.Parts.Length < 5 ? "1" : ctx.Parts[4];

            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckMon, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], instance));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_ExactMon(ScriptParseContext ctx)
        {
            string instance = ctx.Parts.Length < 6 ? "1" : ctx.Parts[5];

            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckExactMon, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], ctx.Parts[4], instance));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Conquest(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckConquest, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_ConquestAvailable(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.ConquestAvailable, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_ConquestOwner(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.ConquestOwner, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- time

        public static ScriptParseOutcome Check_DayOfWeek(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckDay, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Hour(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckHour, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Minute(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckMinute, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Timer(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckTimer, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- buffs

        public static ScriptParseOutcome Check_Buff(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckBuff, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- flow

        public static ScriptParseOutcome Check_Random(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.Random, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Calc(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new NPCChecks(ScriptCheckKind.CheckCalc, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3]));
            return ScriptParseOutcome.Handled;
        }
    }
}
