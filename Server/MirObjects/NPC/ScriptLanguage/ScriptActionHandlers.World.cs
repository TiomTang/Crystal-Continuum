using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Server.MirObjects
{
    public static partial class ScriptActionHandlers
    {
        // ---------------------------------------------------------------- flow

        public static ScriptParseOutcome Act_Goto(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.Goto, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_Call(ScriptParseContext ctx)
        {
            Match quoteMatch = QuoteRegex.Match(ctx.Line);

            string listPath = ctx.Parts[1];

            if (quoteMatch.Success)
                listPath = quoteMatch.Groups[1].Captures[0].Value;

            string root = ScriptValidationScope.Current == null
                ? ScriptParseScope.Root ?? Settings.NPCPath
                : Path.Combine(ScriptValidationScope.Current.EnvirPath, "NPCs");

            string fileName = Path.Combine(root, listPath + ".txt");

            // The root script may still be an unsaved buffer, so ask the session before the disk.
            if (!ScriptValidationScope.Exists(fileName))
            {
                ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingScript, ctx, listPath,
                    "Called script not found: " + listPath);

                return ScriptParseOutcome.Skipped;
            }

            // The path is the only thing the parser records. Binding it to a loaded script (and to its
            // runtime id) is the loader's job, so compiling an unsaved buffer never registers anything.
            if (ScriptValidationScope.IsActive)
                ScriptValidationScope.Current.ValidateCall(fileName, ctx);

            ctx.Acts.Add(new NPCActions(ScriptActionKind.Call, listPath));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_Break(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.Break));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_Close(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.Close));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_DelayGoto(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.DelayGoto, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_GroupGoto(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.GroupGoto, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_EnterMap(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.EnterMap));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- timers

        public static ScriptParseOutcome Act_TimeRecall(ScriptParseContext ctx)
        {
            string page = ctx.Parts.Length > 2 ? ctx.Parts[2] : string.Empty;

            ctx.Acts.Add(new NPCActions(ScriptActionKind.TimeRecall, ctx.Parts[1], page));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_TimeRecallGroup(ScriptParseContext ctx)
        {
            string page = ctx.Parts.Length > 2 ? ctx.Parts[2] : string.Empty;

            ctx.Acts.Add(new NPCActions(ScriptActionKind.TimeRecallGroup, ctx.Parts[1], page));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_BreakTimeRecall(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.BreakTimeRecall));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_SetTimer(ScriptParseContext ctx)
        {
            string global = ctx.Parts.Length < 5 ? string.Empty : ctx.Parts[4];

            ctx.Acts.Add(new NPCActions(ScriptActionKind.SetTimer, ctx.Parts[1], ctx.Parts[2], ctx.Parts[3], global));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ExpireTimer(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ExpireTimer, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- dice

        public static ScriptParseOutcome Act_RollDie(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.RollDie, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_RollYut(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.RollYut, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_GetRandomText(ScriptParseContext ctx)
        {
            Match match = VariableRegex.Match(ctx.Parts[2]);

            if (match.Success)
                ctx.Acts.Add(new NPCActions(ScriptActionKind.GetRandomText, ctx.Parts[1], ctx.Parts[2]));

            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- values

        public static ScriptParseOutcome Act_LoadValue(ScriptParseContext ctx)
        {
            Match quoteMatch = QuoteRegex.Match(ctx.Line);

            if (!quoteMatch.Success) return ScriptParseOutcome.Skipped;

            string fileName = ScriptValidationScope.Current != null
                ? ScriptValidationScope.Current.Resolve("Values", quoteMatch.Groups[1].Captures[0].Value)
                : Path.Combine(Settings.ValuePath, quoteMatch.Groups[1].Captures[0].Value);

            string group = ctx.Parts[ctx.Parts.Length - 2];
            string key = ctx.Parts[ctx.Parts.Length - 1];

            string directory = Path.GetDirectoryName(fileName);
            if (!ScriptValidationScope.IsActive) Directory.CreateDirectory(directory);

            if (!ScriptValidationScope.IsActive && !File.Exists(fileName))
                File.Create(fileName).Close();

            ctx.Acts.Add(new NPCActions(ScriptActionKind.LoadValue, ctx.Parts[1], fileName, group, key));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_SaveValue(ScriptParseContext ctx)
        {
            MatchCollection matches = QuoteRegex.Matches(ctx.Line);

            if (matches.Count == 0 || !matches[0].Success) return ScriptParseOutcome.Skipped;

            string fileName = ScriptValidationScope.Current != null
                ? ScriptValidationScope.Current.Resolve("Values", matches[0].Groups[1].Captures[0].Value)
                : Path.Combine(Settings.ValuePath, matches[0].Groups[1].Captures[0].Value);

            string value = ctx.Parts[ctx.Parts.Length - 1];

            if (matches.Count > 1 && matches[1].Success)
                value = matches[1].Groups[1].Captures[0].Value;

            string[] newParts = ctx.Line.Replace(value, string.Empty).Replace("\"", string.Empty).Trim().Split(' ');

            string group = newParts[newParts.Length - 2];
            string key = newParts[newParts.Length - 1];

            string directory = Path.GetDirectoryName(fileName);
            if (!ScriptValidationScope.IsActive) Directory.CreateDirectory(directory);

            if (!ScriptValidationScope.IsActive && !File.Exists(fileName))
                File.Create(fileName).Close();

            ctx.Acts.Add(new NPCActions(ScriptActionKind.SaveValue, fileName, group, key, value));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- monsters and gates

        public static ScriptParseOutcome Act_MonGen(ScriptParseContext ctx)
        {
            string count = ctx.Parts.Length < 3 ? "1" : ctx.Parts[2];

            ctx.Acts.Add(new NPCActions(ScriptActionKind.Mongen, ctx.Parts[1], count));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_MonClear(ScriptParseContext ctx)
        {
            string instanceId = ctx.Parts.Length < 3 ? "1" : ctx.Parts[2];
            string monster = ctx.Parts.Length < 4 ? string.Empty : ctx.Parts[3];

            ctx.Acts.Add(new NPCActions(ScriptActionKind.MonClear, ctx.Parts[1], instanceId, monster));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_OpenGate(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.OpenGate, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_CloseGate(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.CloseGate, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- conquest

        public static ScriptParseOutcome Act_ConquestGuard(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ConquestGuard, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ConquestGate(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ConquestGate, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ConquestWall(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ConquestWall, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_SetConquestRate(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.SetConquestRate, ctx.Parts[1], ctx.Parts[2]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_StartConquest(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.StartConquest, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ScheduleConquest(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ScheduleConquest, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ConquestRepairAll(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ConquestRepairAll, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        // ---------------------------------------------------------------- guild territory

        public static ScriptParseOutcome Act_BuyGT(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.BuyGT));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_ExtendGT(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.ExtendGT));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_GTRecall(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.GTRecall, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_GTAllRecall(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.GTAllRecall));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_GTSale(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.GTSale, ctx.Parts[1]));
            return ScriptParseOutcome.Handled;
        }

        public static ScriptParseOutcome Act_CancelGTSale(ScriptParseContext ctx)
        {
            ctx.Acts.Add(new NPCActions(ScriptActionKind.GTCancelSale));
            return ScriptParseOutcome.Handled;
        }
    }
}
