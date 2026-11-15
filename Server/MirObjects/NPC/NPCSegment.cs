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
        public ScriptSourceInfo Source;
        public List<ScriptSourceInfo> SaySources = new List<ScriptSourceInfo>();
        public List<ScriptSourceInfo> ElseSaySources = new List<ScriptSourceInfo>();

        public string Param1;
        public int Param1Instance, Param2, Param3;

        public List<string> Args = new List<string>();

        internal bool HasInvalidChecks { get; private set; }
        internal bool HasInvalidActions { get; private set; }
        internal bool IsInvalid { get { return HasInvalidChecks || HasInvalidActions; } }
        internal ScriptDiagnostic InvalidDiagnostic { get; private set; }

        [ThreadStatic] private static ScriptCallContext _executingCall;
        [ThreadStatic] private static bool _executingDisplay;
        [ThreadStatic] private static List<string> _executionInputs;

        internal void InvalidateChecks()
        {
            HasInvalidChecks = true;
        }

        internal void InvalidateActions()
        {
            HasInvalidActions = true;
        }

        internal void MarkInvalid(ScriptDiagnostic diagnostic, bool check)
        {
            if (check) InvalidateChecks();
            else InvalidateActions();
            if (InvalidDiagnostic == null) InvalidDiagnostic = diagnostic;
        }

        /// <summary>Remove invocation-only input before diagnostic exception text is retained.</summary>
        internal static string RedactExecutionInput(string text)
        {
            text = ScriptInteraction.Current?.RedactInput(text) ?? text;
            if (text == null || _executionInputs == null) return text;
            foreach (string input in _executionInputs)
                if (!string.IsNullOrEmpty(input)) text = text.Replace(input, "[redacted]", StringComparison.Ordinal);
            return text;
        }

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

        public void ParseCheck(string line, int lineNumber = 0, ScriptSourceInfo source = null)
        {
            string[] parts = ScriptLexer.Tokenize(line);

            parts = ParseArguments(parts);

            if (parts.Length == 0) return;

            ScriptKeywordRegistry.Dispatch(ScriptKeywordKind.Check, new ScriptParseContext
            {
                Parts = parts,
                Line = line,
                LineNumber = lineNumber,
                FileName = source?.FileName ?? ScriptParseScope.FileName ?? Page?.FileName,
                Source = source,
                PageKey = Page == null ? string.Empty : Page.Key,
                Segment = this,
            });
        }

        public void ParseAct(List<NPCActions> acts, string line, int lineNumber = 0, ScriptSourceInfo source = null)
        {
            string[] parts = ScriptLexer.Tokenize(line);

            parts = ParseArguments(parts);

            if (parts.Length == 0) return;

            ScriptKeywordRegistry.Dispatch(ScriptKeywordKind.Action, new ScriptParseContext
            {
                Parts = parts,
                Line = line,
                LineNumber = lineNumber,
                FileName = source?.FileName ?? ScriptParseScope.FileName ?? Page?.FileName,
                Source = source,
                PageKey = Page == null ? string.Empty : Page.Key,
                Segment = this,
                Acts = acts,
            });
        }

        public List<string> ParseSay(PlayerObject player, List<string> speech)
        {
            return ParseSay(player, speech, null, null);
        }

        private List<string> ParseSay(PlayerObject player, List<string> speech, IList<ScriptSourceInfo> sources,
            ScriptCallContext call)
        {
            call ??= new ScriptCallContext
            {
                LastCompletedAction = ScriptInteraction.Current?.LastCompletedAction,
                HasSideEffects = ScriptInteraction.Current?.HasSideEffects ?? false,
            };
            for (var i = 0; i < speech.Count; i++)
            {
                ScriptSourceInfo source = sources != null && i < sources.Count ? sources[i] : Source ?? Page?.Source;
                using (ScriptDiagnostics.BeginExecution(CreateEvidence(source, ScriptDiagnosticPhase.Display, null, call)))
                {
                    ScriptCallContext previousCall = _executingCall;
                    bool previousDisplay = _executingDisplay;
                    _executingCall = call;
                    _executingDisplay = true;
                    try
                    {
                        speech[i] = ReplaceValues(player, null, speech[i]);
                    }
                    catch (Exception exception)
                    {
                        call.Abort(ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException, Page?.Key, "SAY",
                            "Failed to resolve display text.", Page?.FileName, exception: exception));
                        return new List<string>();
                    }
                    finally
                    {
                        _executingCall = previousCall;
                        _executingDisplay = previousDisplay;
                    }
                }
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
        private void ReportUnknownValue(string name, bool forMonster)
        {
            if (string.IsNullOrEmpty(name)) return;

            ScriptKeywordEntry keyword;

            if (forMonster ? ScriptKeywordRegistry.TryGetMonsterVariable(name, out keyword)
                : ScriptKeywordRegistry.TryGetVariable(name, out keyword)) return;

            ScriptDiagnostic diagnostic = ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.UnknownValue,
                Page == null ? string.Empty : Page.Key, name, "Unknown value <$" + name + ">", Page?.FileName);
            if (!_executingDisplay) _executingCall?.Abort(diagnostic);
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
                ReportUnknownValue(innerMatch, monster != null);
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

            if (!found) return string.Empty;
            if (keyword.ValueExecutor == null)
            {
                ScriptDiagnostic diagnostic = ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.MissingExecutor,
                    Page?.Key, innerMatch, "No value executor registered for " + innerMatch, Page?.FileName);
                _executingCall?.Abort(diagnostic);
                return string.Empty;
            }

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
            return Check(new ScriptCallContext());
        }

        internal bool Check(ScriptCallContext call)
        {
            return Check(null, null, call);
        }

        public bool Check(MonsterObject monster)
        {
            return Check(monster, new ScriptCallContext());
        }

        internal bool Check(MonsterObject monster, ScriptCallContext call)
        {
            return Check(null, monster, call);
        }

        public bool Check(PlayerObject player)
        {
            return Check(player, new ScriptCallContext());
        }

        internal bool Check(PlayerObject player, ScriptCallContext call)
        {
            return Check(player, null, call);
        }

        private bool Check(PlayerObject player, MonsterObject monster, ScriptCallContext call)
        {
            call.ConditionAborted = false;
            if (ExecutionStopped(call)) return false;
            if (IsInvalid)
            {
                ReportInvalidSegment(call);
                return false;
            }

            foreach (NPCChecks check in CheckList)
            {
                ScriptOutcome outcome = RunCheck(check, player, monster, call);
                if (outcome == ScriptOutcome.Abort || ExecutionStopped(call))
                {
                    call.ConditionAborted = true;
                    return false;
                }
                if (outcome != ScriptOutcome.Fail) continue;

                Act(ElseActList, player, monster, call);
                if (player != null && !ExecutionStopped(call)) AddSpeech(player, ElseSay, ElseSaySources, call);
                return false;
            }

            Act(ActList, player, monster, call);
            if (ExecutionStopped(call)) return false;
            if (player != null) AddSpeech(player, Say, SaySources, call);
            return !ExecutionStopped(call);
        }

        private void AddSpeech(PlayerObject player, List<string> speech, IList<ScriptSourceInfo> sources, ScriptCallContext call)
        {
            List<string> parsed = ParseSay(player, new List<string>(speech), sources, call);
            if (!ExecutionStopped(call)) player.NPCSpeech.AddRange(parsed);
        }

        private void ReportInvalidSegment(ScriptCallContext call)
        {
            ScriptDiagnostic diagnostic = InvalidDiagnostic;
            var source = diagnostic == null ? null : new ScriptSourceInfo(diagnostic.FileName, diagnostic.Line,
                diagnostic.Text, diagnostic.SourceContext, diagnostic.SourceVersion);
            using (ScriptDiagnostics.BeginExecution(CreateEvidence(source,
                HasInvalidChecks ? ScriptDiagnosticPhase.Check : ScriptDiagnosticPhase.Action, null, call)))
            {
                call.Abort(ScriptDiagnostics.ReportRuntime(diagnostic?.Kind ?? ScriptDiagnosticKind.InvalidArguments,
                    Page?.Key, diagnostic?.Keyword ?? string.Empty,
                    "Segment disabled by load diagnostic " + (diagnostic?.Id ?? "unknown") + ". " + diagnostic?.Message,
                    Page?.FileName, source: source));
            }
        }

        private ScriptExecutionEvidence CreateEvidence(ScriptSourceInfo source, ScriptDiagnosticPhase phase,
            IList<string> arguments, ScriptCallContext call)
        {
            return new ScriptExecutionEvidence
            {
                Source = source ?? Source ?? Page?.Source,
                ScriptIdentity = Page?.FileName,
                PageKey = Page?.Key,
                Segment = Page == null ? 0 : Page.SegmentList.IndexOf(this) + 1,
                Phase = phase,
                Arguments = arguments?.Select(value => value?.Replace("%INPUTSTR", "[redacted]", StringComparison.Ordinal)).ToList(),
                LastCompletedAction = call.LastCompletedAction,
                CallPath = ScriptInteraction.Current?.CallPath,
                HasSideEffects = call.HasSideEffects,
            };
        }

        private static bool ExecutionStopped(ScriptCallContext call)
        {
            if (ScriptInteraction.Current?.Failed == true) call.ExecutionAborted = true;
            return call.ExecutionAborted;
        }

        private static void SyncInteraction(ScriptCallContext call)
        {
            if (ScriptInteraction.Current == null) return;
            ScriptInteraction.Current.LastCompletedAction = call.LastCompletedAction;
            call.HasSideEffects |= ScriptInteraction.Current.HasSideEffects;
            ScriptInteraction.Current.HasSideEffects |= call.HasSideEffects;
        }

        private ScriptOutcome RunCheck(NPCChecks check, PlayerObject player, MonsterObject monster, ScriptCallContext call)
        {
            return RunKeyword(check, null, player, monster, null, call);
        }

        private ScriptOutcome RunAction(NPCActions action, PlayerObject player, MonsterObject monster,
            ScriptActionState state, ScriptCallContext call)
        {
            return RunKeyword(null, action, player, monster, state, call);
        }

        /// <summary>
        /// Bind source and invocation evidence before resolving arguments: a value executor can fail too.
        /// Abort from an action alone retains its old list-return meaning; a reported failure aborts the call.
        /// </summary>
        private ScriptOutcome RunKeyword(NPCChecks check, NPCActions action, PlayerObject player, MonsterObject monster,
            ScriptActionState state, ScriptCallContext call)
        {
            bool condition = check != null;
            string keyword = condition ? check.Type.ToString() : action.Type.ToString();
            IList<string> raw = condition ? check.Params : action.Params;
            ScriptExecutionEvidence evidence = CreateEvidence(condition ? check.Source : action.Source,
                condition ? ScriptDiagnosticPhase.Check : ScriptDiagnosticPhase.Action, raw, call);
            ScriptCallContext previousCall = _executingCall;
            bool previousDisplay = _executingDisplay;
            List<string> previousInputs = _executionInputs;
            int thread = Environment.CurrentManagedThreadId;
            ScriptInteraction ownerInteraction = ScriptInteraction.Current;
            Action<ScriptDiagnostic> failed = diagnostic =>
            {
                if (thread == Environment.CurrentManagedThreadId && ReferenceEquals(ScriptInteraction.Current, ownerInteraction) &&
                    ReferenceEquals(_executingCall, call)) call.Abort(diagnostic);
            };

            using (ScriptDiagnostics.BeginExecution(evidence))
            {
                _executingCall = call;
                _executingDisplay = false;
                _executionInputs = previousInputs == null ? new List<string>() : new List<string>(previousInputs);
                if (ownerInteraction != null) ownerInteraction.LastSource = evidence.Source;
                ScriptDiagnostics.Reported += failed;
                try
                {
                    string input = null;
                    if (!condition && player != null && player.NPCData.TryGetValue("NPCInputStr", out object inputValue))
                    {
                        input = (string)inputValue;
                        ownerInteraction?.RememberInput(input);
                        if (!string.IsNullOrEmpty(input)) _executionInputs.Add(input);
                    }

                    var parameters = new List<string>(raw.Count);
                    for (int i = 0; i < raw.Count; i++)
                    {
                        string value = raw[i];
                        if (player != null) value = FindVariable(player, value);
                        else if (monster != null) value = FindVariable(monster, value);
                        value = ReplaceValues(player, monster, value);
                        if (ExecutionStopped(call)) return ScriptOutcome.Abort;

                        // Evidence never contains the substituted player input, even if a later action throws.
                        evidence.Arguments[i] = RedactExecutionInput(value?.Replace("%INPUTSTR", "[redacted]", StringComparison.Ordinal));
                        if (input != null) value = value.Replace("%INPUTSTR", input, StringComparison.Ordinal);
                        parameters.Add(value);
                    }

                    Func<ScriptExecuteContext, ScriptOutcome> executor = condition
                        ? ScriptKeywordRegistry.CheckExecutor(check.Type)
                        : ScriptKeywordRegistry.ActionExecutor(action.Type, monster != null);
                    if (executor == null)
                    {
                        call.Abort(ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.MissingExecutor, Page?.Key, keyword,
                            "No executor registered for " + keyword, Page?.FileName));
                        return ScriptOutcome.Abort;
                    }

                    // Conservative: a throwing or early-returning executor may already have changed the world.
                    if (!condition) evidence.HasSideEffects = call.HasSideEffects = true;
                    SyncInteraction(call);
                    ScriptOutcome outcome = executor(new ScriptExecuteContext
                    {
                        Segment = this,
                        Check = check,
                        Action = action,
                        Params = parameters,
                        Player = player,
                        Monster = monster,
                        State = state,
                        Call = call,
                    });
                    if (ExecutionStopped(call)) return ScriptOutcome.Abort;
                    if (outcome == ScriptOutcome.InvalidArguments)
                    {
                        string usage = condition ? ScriptCheckCatalog.Find(check.Type)?.Usage : ScriptActionCatalog.Find(action.Type)?.Usage;
                        call.Abort(ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.InvalidArguments, Page?.Key, keyword,
                            "Invalid argument type or value; expected " + usage, Page?.FileName));
                        return ScriptOutcome.Abort;
                    }
                    if (condition && outcome == ScriptOutcome.Abort)
                    {
                        call.Abort(ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException, Page?.Key, keyword,
                            "Condition aborted; neither branch will run.", Page?.FileName));
                    }
                    else if (!condition && outcome != ScriptOutcome.Abort)
                    {
                        call.LastCompletedAction = keyword;
                    }
                    return outcome;
                }
                catch (Exception exception)
                {
                    call.Abort(ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException, Page?.Key, keyword,
                        "Failed to execute " + (condition ? "condition " : "action ") + keyword,
                        Page?.FileName, exception: exception));
                    return ScriptOutcome.Abort;
                }
                finally
                {
                    ScriptDiagnostics.Reported -= failed;
                    _executingCall = previousCall;
                    _executingDisplay = previousDisplay;
                    _executionInputs = previousInputs;
                    SyncInteraction(call);
                }
            }
        }

        private void Act(IList<NPCActions> acts, PlayerObject player, MonsterObject monster, ScriptCallContext call)
        {
            var state = new ScriptActionState();
            foreach (NPCActions action in acts)
            {
                if (ExecutionStopped(call)) return;
                if (RunAction(action, player, monster, state, call) == ScriptOutcome.Abort || ExecutionStopped(call)) return;
            }
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