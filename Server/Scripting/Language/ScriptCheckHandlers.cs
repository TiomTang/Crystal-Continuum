using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Server.Scripting
{
    /// <summary>
    /// Handlers for the #IF keyword table. The registry checks the minimum argument count before
    /// calling these, so each handler can read the parts it needs without repeating that guard.
    /// </summary>
    public static class ScriptCheckHandlers
    {
        internal static readonly Regex FlagRegex = new Regex(@"\[(.*?)\]", RegexOptions.Compiled);
        private static readonly Regex QuoteRegex = new Regex("\"([^\"]*)\"", RegexOptions.Compiled);

        // ---------------------------------------------------------------- player

        public static ScriptParseOutcome Check_Level(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.Level, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Gold(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckGold, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Credit(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckCredit, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Gender(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckGender, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Class(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckClass, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_PkPoint(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckPkPoint, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Flag(ScriptParseContext ctx)
        {
            Match match = FlagRegex.Match(ctx.Parts[1]);

            if (match.Success)
            {
                string flagIndex = match.Groups[1].Captures[0].Value;
                ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.Check, flagIndex, ctx.Parts[2]));
            }

            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_IsNewHuman(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.IsNewHuman));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_IsAdmin(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.IsAdmin));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- items

        public static ScriptParseOutcome Check_Item(ScriptParseContext ctx)
        {
            string count = ctx.Parts.Length < 3 ? "1" : ctx.Parts[2];
            string durability = ctx.Parts.Length > 3 ? ctx.Parts[3] : string.Empty;

            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckItem, ctx.Parts[1], count, durability));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_BagSpace(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.HasBagSpace, ctx.Parts[1], ctx.Parts[2]));
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

            string fileName = ScriptValidationScope.Current != null
                ? ScriptValidationScope.Current.NameListPath(listPath) : Settings.GetNameListPath(listPath);


            ctx.Segment.CheckList.Add(new ScriptChecks(type, fileName));

            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- social

        public static ScriptParseOutcome Check_Relationship(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckRelationship));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_WeddingRing(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckWeddingRing));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- guild

        public static ScriptParseOutcome Check_GuildGold(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckGuildGold, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_InGuild(ScriptParseContext ctx)
        {
            string guildName = string.Empty;

            if (ctx.Parts.Length > 1) guildName = ctx.Parts[1];

            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.InGuild, guildName));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Permission(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckPermission, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HasGT(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.HasGT));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordGuard(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.AffordGuard, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordGate(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.AffordGate, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordWall(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.AffordWall, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_AffordSiege(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.AffordSiege, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- group

        public static ScriptParseOutcome Check_GroupLeader(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.Groupleader));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_GroupCount(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.GroupCount, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_GroupNearby(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.GroupCheckNearby));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- pets

        public static ScriptParseOutcome Check_PetCount(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.PetCount, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_PetLevel(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.PetLevel, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Pet(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckPet, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- hero

        public static ScriptParseOutcome Check_HeroLevel(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.HeroLevel, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HeroClass(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckHeroClass, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HeroGender(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckHeroGender, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_HeroItem(ScriptParseContext ctx)
        {
            string count = ctx.Parts.Length < 3 ? "1" : ctx.Parts[2];
            string durability = ctx.Parts.Length > 3 ? ctx.Parts[3] : string.Empty;

            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckHeroItem, ctx.Parts[1], count, durability));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- quests

        public static ScriptParseOutcome Check_Quest(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckQuest, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- world

        public static ScriptParseOutcome Check_Range(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckRange, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Map(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckMap, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_MapLight(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckMapLight, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Hum(ScriptParseContext ctx)
        {
            string instance = ctx.Parts.Length < 5 ? "1" : ctx.Parts[4];

            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckHum, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], instance));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Mon(ScriptParseContext ctx)
        {
            string instance = ctx.Parts.Length < 5 ? "1" : ctx.Parts[4];

            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckMon, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], instance));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_ExactMon(ScriptParseContext ctx)
        {
            string instance = ctx.Parts.Length < 6 ? "1" : ctx.Parts[5];

            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckExactMon, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], ctx.Parts[4], instance));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Conquest(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckConquest, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_ConquestAvailable(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.ConquestAvailable, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_ConquestOwner(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.ConquestOwner, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- time

        public static ScriptParseOutcome Check_DayOfWeek(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckDay, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Hour(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckHour, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Minute(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckMinute, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Timer(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckTimer, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- buffs

        public static ScriptParseOutcome Check_Buff(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckBuff, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- flow

        public static ScriptParseOutcome Check_Random(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.Random, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Check_Calc(ScriptParseContext ctx)
        {
            ctx.Segment.CheckList.Add(new ScriptChecks(ScriptCheckKind.CheckCalc, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3]));
            return ScriptParseOutcome.Handled;
        }
    }
}
