using System.Linq;

namespace Server.Scripting
{
    public static partial class ScriptActionHandlers
    {
        // ---------------------------------------------------------------- name lists

        public static ScriptParseOutcome Act_AddNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptActionKind.AddNameList, true);
        }

        public static ScriptParseOutcome Act_AddGuildNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptActionKind.AddGuildNameList, true);
        }

        public static ScriptParseOutcome Act_DelNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptActionKind.DelNameList, false);
        }

        public static ScriptParseOutcome Act_DelGuildNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptActionKind.DelGuildNameList, false);
        }

        public static ScriptParseOutcome Act_ClearNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptActionKind.ClearNameList, false);
        }

        public static ScriptParseOutcome Act_ClearGuildNameList(ScriptParseContext ctx)
        {
            return AddNameList(ctx, ScriptActionKind.ClearGuildNameList, false);
        }

        // ---------------------------------------------------------------- guild and marriage

        public static ScriptParseOutcome Act_AddToGuild(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.AddToGuild, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_RemoveFromGuild(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.RemoveFromGuild, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_MakeWeddingRing(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.MakeWeddingRing));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ForceDivorce(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.ForceDivorce));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- messages

        public static ScriptParseOutcome Act_LocalMessage(ScriptParseContext ctx)
        {
            return AddQuotedMessage(ctx, ScriptActionKind.LocalMessage);
        }

        public static ScriptParseOutcome Act_GlobalMessage(ScriptParseContext ctx)
        {
            return AddQuotedMessage(ctx, ScriptActionKind.GlobalMessage);
        }

        public static ScriptParseOutcome Act_OpenBrowser(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.OpenBrowser, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_PlaySound(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.PlaySound, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- mail

        public static ScriptParseOutcome Act_ComposeMail(ScriptParseContext ctx)
        {
            return AddQuotedMessage(ctx, ScriptActionKind.ComposeMail);
        }

        public static ScriptParseOutcome Act_AddMailGold(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.AddMailGold, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_AddMailItem(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.AddMailItem, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_SendMail(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.SendMail));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- buffs

        public static ScriptParseOutcome Act_GiveBuff(ScriptParseContext ctx)
        {
            var buffParams = ctx.Parts.Skip(1).ToList();

            while (buffParams.Count < 5) buffParams.Add(string.Empty);

            ctx.Acts.Add(new ScriptActions(ScriptActionKind.GiveBuff, buffParams.ToArray()));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_RemoveBuff(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new ScriptActions(ScriptActionKind.RemoveBuff, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }
    }
}
