using System.Drawing;
﻿using Server.MirDatabase;
using Server.MirEnvir;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;
using Server.MirNetwork;
using Server.Library.MirDatabase;

namespace Server.MirObjects
{
    public partial class NPCSegment
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        protected static MessageQueue MessageQueue
        {
            get { return MessageQueue.Instance; }
        }

        public NPCPage Page;

        public readonly string Key;
        public List<NPCChecks> CheckList = new List<NPCChecks>();
        public List<NPCActions> ActList = new List<NPCActions>(), ElseActList = new List<NPCActions>();
        public List<string> Say, ElseSay, Buttons, ElseButtons, GotoButtons;

        public string Param1;
        public int Param1Instance, Param2, Param3;

        public List<string> Args = new List<string>();

        public NPCSegment(NPCPage page, List<string> say, List<string> buttons, List<string> elseSay, List<string> elseButtons, List<string> gotoButtons)
        {
            Page = page;

            Say = say;
            Buttons = buttons;

            ElseSay = elseSay;
            ElseButtons = elseButtons;

            GotoButtons = gotoButtons;
        }

        public string[] ParseArguments(string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].IndexOf("%ARG", StringComparison.OrdinalIgnoreCase) < 0) continue;

                words[i] = ScriptLexer.Argument.Replace(words[i], match =>
                {
                    int sequence;

                    if (!int.TryParse(match.Groups[1].Value, out sequence)) return match.Value;
                    if (Page == null || Page.Args.Count <= sequence) return match.Value;

                    return Page.Args[sequence];
                });
            }

            return words;
        }

        public void AddVariable(MapObject player, string key, string value)
        {
            if (!ScriptLexer.IsVariableKey(key)) return;

            for (int i = 0; i < player.NPCVar.Count; i++)
            {
                if (!String.Equals(player.NPCVar[i].Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;
                player.NPCVar[i] = new KeyValuePair<string, string>(player.NPCVar[i].Key, value);
                return;
            }

            player.NPCVar.Add(new KeyValuePair<string, string>(key, value));
        }

        public string FindVariable(MapObject player, string key)
        {
            if (!ScriptLexer.IsVariableKey(key)) return key;

            string tempKey = key.Substring(1);

            foreach (KeyValuePair<string, string> t in player.NPCVar)
            {
                if (String.Equals(t.Key, tempKey, StringComparison.CurrentCultureIgnoreCase)) return t.Value;
            }

            return key;
        }

        public void ParseCheck(string line)
        {
            string[] parts = ScriptLexer.Tokenize(line);

            parts = ParseArguments(parts);

            if (parts.Length == 0) return;

            ScriptKeywordRegistry.Dispatch(ScriptKeywordKind.Check, new ScriptParseContext
            {
                Parts = parts,
                Line = line,
                FileName = ScriptParseScope.FileName,
                PageKey = Page == null ? string.Empty : Page.Key,
                Segment = this,
            });
        }

        public void ParseAct(List<NPCActions> acts, string line)
        {
            string[] parts = ScriptLexer.Tokenize(line);

            parts = ParseArguments(parts);

            if (parts.Length == 0) return;

            ScriptKeywordRegistry.Dispatch(ScriptKeywordKind.Action, new ScriptParseContext
            {
                Parts = parts,
                Line = line,
                FileName = ScriptParseScope.FileName,
                PageKey = Page == null ? string.Empty : Page.Key,
                Segment = this,
                Acts = acts,
            });
        }

        public List<string> ParseSay(PlayerObject player, List<string> speech)
        {
            for (var i = 0; i < speech.Count; i++)
            {
                speech[i] = ReplaceValues(player, null, speech[i]);
            }

            return LinkFormatter.ReplacePlaceholders(speech, (type, index) => EnsureLinkInfo(player, type, index)) ?? speech;
        }

        private static void EnsureLinkInfo(PlayerObject player, string linkType, int index)
        {
            if (player?.Connection == null) return;

            switch (linkType)
            {
                case "ITEM":
                    var itemInfo = Envir.GetItemInfo(index);
                    if (itemInfo != null)
                        player.Connection.CheckItemInfo(itemInfo);
                    break;
                case "MONSTER":
                    player.Connection.CheckMonsterInfo(index);
                    break;
                case "NPC":
                    player.Connection.CheckNPCInfo(index);
                    break;
            }
        }

        /// <summary>
        /// Reports a &lt;$VALUE&gt; that no table knows. A value can also resolve to empty on purpose,
        /// so anything the tables list is left alone.
        /// </summary>
        private void ReportUnknownValue(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOf('(') >= 0) return;

            ScriptKeywordEntry keyword;

            if (ScriptKeywordRegistry.TryGetVariable(name, out keyword)) return;
            if (ScriptKeywordRegistry.TryGetMonsterVariable(name, out keyword)) return;

            ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.UnknownValue, Page == null ? string.Empty : Page.Key, name,
                "Unknown value <$" + name + ">");
        }

        public string ReplaceValue(PlayerObject player, string param)
        {
            return ReplaceValues(player, null, param);
        }

        public string ReplaceValue(MonsterObject Monster, string param)
        {
            return ReplaceValues(null, Monster, param);
        }

        /// <summary>
        /// Resolves every &lt;$VALUE&gt; in the text, one placeholder at a time. A token with two
        /// placeholders no longer collapses into a single broken lookup, and a placeholder whose argument
        /// contains a space resolves because the tokenizer kept it together.
        /// </summary>
        private string ReplaceValues(PlayerObject player, MonsterObject monster, string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("<$", StringComparison.Ordinal) < 0) return text;

            return ScriptLexer.Placeholder.Replace(text, match => Resolve(player, monster, match));
        }

        private string Resolve(PlayerObject player, MonsterObject monster, Match match)
        {
            string innerMatch = match.Groups[1].Value.ToUpper();

            Match varMatch = ScriptLexer.StatementValue.Match(innerMatch);
            Match oneValMatch = monster == null ? ScriptLexer.OneValue.Match(innerMatch) : null;
            Match twoValMatch = monster == null ? ScriptLexer.TwoValue.Match(innerMatch) : null;

            if (varMatch.Success)
                innerMatch = innerMatch.Replace(varMatch.Groups[2].Value.ToUpper(), "");
            else if (twoValMatch != null && twoValMatch.Success)
                innerMatch = innerMatch.Replace(twoValMatch.Groups[2].Value.ToUpper(), "");
            else if (oneValMatch != null && oneValMatch.Success)
                innerMatch = innerMatch.Replace(oneValMatch.Groups[2].Value.ToUpper(), "");

            string newValue = RunValue(player, monster, innerMatch, varMatch, oneValMatch, twoValMatch);

            if (string.IsNullOrEmpty(newValue))
            {
                ReportUnknownValue(innerMatch);
                return match.Value;
            }

            return newValue;
        }

        /// <summary>
        /// Looks a &lt;$VALUE&gt; up in the keyword registry and runs it. An unknown value answers with an
        /// empty string, which the caller reports and then leaves the placeholder untouched, exactly like
        /// the old switch did.
        /// </summary>
        private string RunValue(PlayerObject player, MonsterObject monster, string innerMatch, Match varMatch,
            Match oneValMatch, Match twoValMatch)
        {
            ScriptKeywordEntry keyword;

            bool found = monster != null
                ? ScriptKeywordRegistry.TryGetMonsterVariable(innerMatch, out keyword)
                : ScriptKeywordRegistry.TryGetVariable(innerMatch, out keyword);

            if (!found || keyword.ValueExecutor == null) return string.Empty;

            return keyword.ValueExecutor(new ScriptExecuteContext
            {
                Segment = this,
                Player = player,
                Monster = monster,
                InnerMatch = innerMatch,
                VariableMatch = varMatch,
                OneValueMatch = oneValMatch,
                TwoValueMatch = twoValMatch,
            });
        }

        public bool Check()
        {
            for (int i = 0; i < CheckList.Count; i++)
            {
                NPCChecks check = CheckList[i];
                List<string> param = check.Params.ToList();

                ScriptOutcome outcome = RunCheck(check, param, null, null);

                if (outcome == ScriptOutcome.Abort) return true;

                if (outcome == ScriptOutcome.Fail)
                {
                    Failed();
                    return false;
                }
            }

            Success();
            return true;
        }

        public bool Check(MonsterObject monster)
        {
            for (int i = 0; i < CheckList.Count; i++)
            {
                NPCChecks check = CheckList[i];
                List<string> param = check.Params.Select(t => FindVariable(monster, t)).ToList();

                for (int j = 0; j < param.Count; j++)
                {
                    param[j] = ReplaceValues(null, monster, param[j]);
                }

                ScriptOutcome outcome = RunCheck(check, param, null, monster);

                if (outcome == ScriptOutcome.Abort) return true;

                if (outcome == ScriptOutcome.Fail)
                {
                    Failed(monster);
                    return false;
                }
            }

            Success(monster);
            return true;
        }

        public bool Check(PlayerObject player)
        {
            for (int i = 0; i < CheckList.Count; i++)
            {
                NPCChecks check = CheckList[i];
                List<string> param = check.Params.Select(t => FindVariable(player, t)).ToList();

                for (int j = 0; j < param.Count; j++)
                {
                    param[j] = ReplaceValues(player, null, param[j]);
                }

                ScriptOutcome outcome = RunCheck(check, param, player, null);

                if (outcome == ScriptOutcome.Abort) return true;

                if (outcome == ScriptOutcome.Fail)
                {
                    Failed(player);
                    return false;
                }
            }

            Success(player);
            return true;
        }

        /// <summary>
        /// Looks the condition up in the keyword registry and runs it. A type without an executor is
        /// reported instead of being silently treated as satisfied.
        /// </summary>
        private ScriptOutcome RunCheck(NPCChecks check, List<string> param, PlayerObject player, MonsterObject monster)
        {
            Func<ScriptExecuteContext, ScriptOutcome> executor = ScriptKeywordRegistry.CheckExecutor(check.Type);

            if (executor == null)
            {
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.UnknownCheck, Page == null ? string.Empty : Page.Key,
                    check.Type.ToString(), "条件没有执行器：" + check.Type);

                return ScriptOutcome.Pass;
            }

            return executor(new ScriptExecuteContext
            {
                Segment = this,
                Check = check,
                Params = param,
                Player = player,
                Monster = monster,
            });
        }


                private void Act(IList<NPCActions> acts)
        {
            var state = new ScriptActionState();

            for (var i = 0; i < acts.Count; i++)
            {
                NPCActions act = acts[i];
                List<string> param = act.Params.ToList();
                if (RunAction(act, param, null, null, state) == ScriptOutcome.Abort) return;
            }
        }

                private void Act(IList<NPCActions> acts, PlayerObject player)
        {
            var state = new ScriptActionState();

            for (var i = 0; i < acts.Count; i++)
            {
                NPCActions act = acts[i];
                List<string> param = act.Params.Select(t => FindVariable(player, t)).ToList();

                for (int j = 0; j < param.Count; j++)
                {
                    param[j] = ReplaceValues(player, null, param[j]);

                    if (player.NPCData.TryGetValue("NPCInputStr", out object _npcInputStr))
                    {
                        param[j] = param[j].Replace("%INPUTSTR", (string)_npcInputStr);
                    }
                }
                if (RunAction(act, param, player, null, state) == ScriptOutcome.Abort) return;
            }
        }

        /// <summary>
        /// Looks the action up in the keyword registry and runs it. A type without an executor is reported
        /// instead of being skipped in silence.
        /// </summary>
        private ScriptOutcome RunAction(NPCActions act, List<string> param, PlayerObject player, MonsterObject monster,
            ScriptActionState state)
        {
            Func<ScriptExecuteContext, ScriptOutcome> executor = ScriptKeywordRegistry.ActionExecutor(act.Type, monster != null);

            if (executor == null)
            {
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.UnknownAction, Page == null ? string.Empty : Page.Key,
                    act.Type.ToString(), "动作没有执行器：" + act.Type);

                return ScriptOutcome.Pass;
            }

            return executor(new ScriptExecuteContext
            {
                Segment = this,
                Action = act,
                Params = param,
                Player = player,
                Monster = monster,
                State = state,
            });
        }

                private void Act(IList<NPCActions> acts, MonsterObject monster)
        {
            var state = new ScriptActionState();

            for (var i = 0; i < acts.Count; i++)
            {
                NPCActions act = acts[i];
                List<string> param = act.Params.Select(t => FindVariable(monster, t)).ToList();

                for (int j = 0; j < param.Count; j++)
                {
                    param[j] = ReplaceValues(null, monster, param[j]);
                }
                if (RunAction(act, param, null, monster, state) == ScriptOutcome.Abort) return;
            }
        }


        private void Success(PlayerObject player)
        {
            Act(ActList, player);

            var parseSay = new List<String>(Say);
            parseSay = ParseSay(player, parseSay);

            player.NPCSpeech.AddRange(parseSay);
        }

        private void Failed(PlayerObject player)
        {
            Act(ElseActList, player);

            var parseElseSay = new List<String>(ElseSay);
            parseElseSay = ParseSay(player, parseElseSay);

            player.NPCSpeech.AddRange(parseElseSay);
        }

        private void Success(MonsterObject Monster)
        {
            Act(ActList, Monster);
        }

        private void Failed(MonsterObject Monster)
        {
            Act(ElseActList, Monster);
        }

        private void Success()
        {
            Act(ActList);
        }

        private void Failed()
        {
            Act(ElseActList);
        }

        public static bool Compare<T>(string op, T left, T right) where T : IComparable<T>
        {
            switch (op)
            {
                case "<": return left.CompareTo(right) < 0;
                case ">": return left.CompareTo(right) > 0;
                case "<=": return left.CompareTo(right) <= 0;
                case ">=": return left.CompareTo(right) >= 0;
                case "==": return left.Equals(right);
                case "!=": return !left.Equals(right);
                default: throw new ArgumentException(string.Format("Invalid comparison operator: {0}", op));
            }
        }

        public static int Calculate(string op, int left, int right)
        {
            switch (op)
            {
                case "+": return left + right;
                case "-": return left - right;
                case "*": return left * right;
                case "/": return left / right;
                default: throw new ArgumentException(string.Format("Invalid sum operator: {0}", op));
            }
        }
    }
}