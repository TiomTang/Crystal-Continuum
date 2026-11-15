using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Server.MirObjects;

namespace Server.MirForms.Scripts
{
    public sealed class ScriptBlock
    {
        public ScriptBlockKind Kind;
        public int StartLine;
        public int EndLine;
        public string Tag;
        public string PageKey;
    }

    public sealed class ScriptPageInfo
    {
        public string Key;
        public int Line;
        public int EndLine;
        public bool IsSection;
        public ScriptSectionKind Section;
        public readonly List<ScriptBlock> Blocks = new List<ScriptBlock>();
    }

    /// <summary>Facts the checker borrows from the running server; null means "cannot judge".</summary>
    public sealed class ScriptRules
    {
        public Func<string, bool> SpellExists;
        public Func<string, bool> MapExists;
        public Func<string, bool> ItemExists;
        // Retained for existing callers; CALL resolves a file path, never a database script ID.
        public Func<int, bool> ScriptExists;

        /// <summary>Whether the item has a usable recipe with ingredients; null means unavailable.</summary>
        public Func<string, bool> RecipeExists;

        /// <summary>Null when the quest list is unavailable.</summary>
        public Func<int, bool> QuestExists;
    }

    public sealed class ScriptIssue
    {
        public int Line;
        public bool IsError;
        public string Message;
        public string FileName;
        public string PageKey;
        public ScriptDiagnostic Diagnostic;

        public override string ToString()
        {
            return (!string.IsNullOrEmpty(FileName) ? FileName + "  " : string.Empty) +
                (Line > 0 ? "第 " + Line + " 行  " : string.Empty) + Message;
        }
    }

    /// <summary>
    /// Buffer-local navigation and caret context. Validation uses the engine's isolated loader;
    /// database and value checks only inspect its source-bound result, never activate NPC data.
    /// </summary>
    public sealed class ScriptProject
    {
        private string _path;
        public NPCScriptType Type { get; private set; }

        public string[] Lines = new string[0];
        public readonly List<ScriptBlock> Blocks = new List<ScriptBlock>();
        public readonly List<ScriptPageInfo> Pages = new List<ScriptPageInfo>();
        public readonly List<ScriptPageInfo> Sections = new List<ScriptPageInfo>();
        public readonly HashSet<string> PageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static ScriptProject Parse(string text, string path = null, NPCScriptType type = NPCScriptType.Normal)
        {
            var project = new ScriptProject { _path = path, Type = type };
            project.Lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            ScriptBlock current = null;
            ScriptPageInfo page = null;

            for (int i = 0; i < project.Lines.Length; i++)
            {
                // The engine reads a key exactly as written: "[@MAIN] " with a trailing space is text to
                // the loader, so the editor must not show it as a page.
                string raw = project.Lines[i];
                string trimmed = raw.Trim();

                ScriptSectionEntry section = FindSection(raw);

                if (section != null)
                {
                    if (current != null) current.EndLine = i;

                    current = null;

                    page = new ScriptPageInfo { Key = raw, Line = i + 1, IsSection = true, Section = section.Kind };
                    project.Pages.Add(page);
                    project.Sections.Add(page);
                    continue;
                }

                string pageKey;

                if (ScriptPageKeyCatalog.TryParseHeader(raw, out pageKey))
                {
                    if (current != null) current.EndLine = i;

                    current = null;

                    page = new ScriptPageInfo { Key = pageKey, Line = i + 1 };
                    project.Pages.Add(page);
                    project.PageKeys.Add(Normalize(pageKey));
                    continue;
                }

                if (page == null || page.IsSection || !trimmed.StartsWith("#")) continue;

                ScriptBlockKind kind;
                if (ScriptBlockCatalog.TryFind(raw, out kind) == null) continue;

                if (current != null) current.EndLine = i;

                current = new ScriptBlock
                {
                    Kind = kind,
                    StartLine = i + 1,
                    EndLine = project.Lines.Length,
                    Tag = ScriptBlockCatalog.Tag(kind),
                    PageKey = page == null ? string.Empty : page.Key,
                };

                project.Blocks.Add(current);
                if (page != null) page.Blocks.Add(current);
            }

            for (int i = 0; i < project.Pages.Count; i++)
            {
                ScriptPageInfo entry = project.Pages[i];
                entry.EndLine = i + 1 < project.Pages.Count ? project.Pages[i + 1].Line - 1 : project.Lines.Length;
                if (!entry.IsSection) continue;

                // TYPES and SPEECH can stop before another header (including at a comment).
                // Do not offer section values for lines the loader will never consume.
                int end = entry.Line;
                foreach (ScriptSectionLine body in ScriptSectionCatalog.Body(project.Lines, entry.Line - 1, entry.Section))
                    end = body.Index + 1;
                while (end < entry.EndLine && string.IsNullOrEmpty(project.Lines[end])) end++;
                entry.EndLine = Math.Min(entry.EndLine, end);
            }

            return project;
        }

        /// <summary>Section whose header the line is, by the loader's own rule.</summary>
        private static ScriptSectionEntry FindSection(string line)
        {
            foreach (ScriptSectionEntry entry in ScriptSectionCatalog.Ordered)
            {
                if (entry.MatchesHeader(line)) return entry;
            }

            return null;
        }

        /// <summary>The Envir root for NPCs and SystemScripts, including unsaved file paths.</summary>
        public static string IncludeRoot(string path)
        {
            string directory = string.IsNullOrEmpty(path)
                ? Path.GetFullPath("Envir")
                : Path.GetDirectoryName(Path.GetFullPath(path));

            DirectoryInfo info = directory == null ? null : new DirectoryInfo(directory);

            while (info != null)
            {
                if ((string.Equals(info.Name, "NPCs", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(info.Name, "SystemScripts", StringComparison.OrdinalIgnoreCase)) && info.Parent != null)
                    return info.Parent.FullName;
                if (string.Equals(info.Name, "Envir", StringComparison.OrdinalIgnoreCase)) return info.FullName;
                info = info.Parent;
            }

            return directory ?? ".";
        }

        /// <summary>Section the given 1-based line belongs to, if any.</summary>
        public ScriptSectionKind SectionAt(int line)
        {
            return SectionPageAt(line)?.Section ?? ScriptSectionKind.None;
        }

        public ScriptPageInfo SectionPageAt(int line)
        {
            foreach (ScriptPageInfo section in Sections)
            {
                if (line >= section.Line && line <= section.EndLine) return section;
            }

            return null;
        }

        /// <summary>Consumed data lines, for candidate filtering; repeated entries remain legal.</summary>
        public HashSet<string> SectionValues(ScriptPageInfo section)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (section == null || !section.IsSection) return values;

            foreach (ScriptSectionLine line in ScriptSectionCatalog.Body(Lines, section.Line - 1, section.Section))
            {
                string key = SectionKey(line.Text);
                if (key.Length > 0) values.Add(key);
            }

            return values;
        }

        private static string SectionKey(string line)
        {
            string trimmed = line.Trim();

            if (trimmed.Length == 0 || ScriptLexer.IsComment(trimmed)) return string.Empty;

            return string.Join(" ", trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>Page or data section that owns the given 1-based line.</summary>
        public ScriptPageInfo PageAt(int line)
        {
            foreach (ScriptPageInfo page in Pages)
            {
                if (line >= page.Line && line <= page.EndLine) return page;
            }

            return null;
        }

        /// <summary>
        /// Block that owns the given 1-based line, or the page when the line sits before the first tag.
        /// The search stops at the page edges: a fresh page sits in page context even when the page
        /// before it ended inside #ACT.
        /// </summary>
        public ScriptBlockKind ContextAt(int line, out string pageKey)
        {
            pageKey = string.Empty;

            ScriptPageInfo page = PageAt(line);

            if (page == null) return ScriptBlockKind.Page;

            pageKey = page.Key;

            ScriptBlockKind kind = line == page.Line || page.IsSection ? ScriptBlockKind.Page : ScriptBlockKind.Say;

            foreach (ScriptBlock block in page.Blocks)
            {
                if (block.StartLine > line) break;

                kind = block.Kind;
            }

            return kind;
        }

        /// <summary>
        /// Blocks of the segment that owns the given line. The loader splits a page into a new segment at
        /// every #IF, and one segment holds at most one SAY, ACT, ELSESAY and ELSEACT.
        /// </summary>
        public List<ScriptBlock> SegmentAt(int line)
        {
            var segment = new List<ScriptBlock>();
            ScriptPageInfo page = PageAt(line);

            if (page == null) return segment;

            int first = 0;

            for (int i = 0; i < page.Blocks.Count; i++)
            {
                if (page.Blocks[i].StartLine > line) break;

                if (page.Blocks[i].Kind == ScriptBlockKind.Check) first = i;
            }

            for (int i = first; i < page.Blocks.Count; i++)
            {
                if (i > first && page.Blocks[i].Kind == ScriptBlockKind.Check) break;

                segment.Add(page.Blocks[i]);
            }

            return segment;
        }

        public static string Normalize(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey)) return string.Empty;
            string key = pageKey.Trim();
            if (!key.StartsWith("[")) key = "[" + key + "]";
            return NPCPage.NormalizeSectionName(key, out _).Trim('[', ']').ToUpperInvariant();
        }

        /// <summary>Engine diagnostics, plus read-only database and value facts it cannot check at load time.</summary>
        public List<ScriptIssue> Check(ScriptRules rules = null)
        {
            var issues = new List<ScriptIssue>();
            ScriptValidationResult validation;
            string fileName = _path;
            try
            {
                fileName = Path.GetFullPath(string.IsNullOrEmpty(fileName)
                    ? Path.Combine("Envir", "NPCs", "__editor__.txt") : fileName);
                validation = NPCScript.Validate(string.Join("\n", Lines), fileName, IncludeRoot(fileName), Type);
            }
            catch (Exception exception)
            {
                // Invalid paths fail before the engine creates its scope. Keep even that failure local.
                var diagnostic = new ScriptDiagnostic
                {
                    Kind = ScriptDiagnosticKind.LoadFailed, FileName = fileName,
                    Category = ScriptDiagnosticCategory.RuntimeFailure, Phase = ScriptDiagnosticPhase.Load,
                    Effect = ScriptDiagnosticEffect.ScriptNotLoaded, Message = exception.Message,
                    ExceptionType = exception.GetType().FullName,
                };
                issues.Add(FromDiagnostic(diagnostic));
                return issues;
            }


            // This is the expanded source, not a second INSERT/INCLUDE implementation or stale cache.
            // Navigation stays buffer-local: included line numbers must never become editor offsets.
            PageKeys.Clear();
            foreach (ScriptSourceLine line in validation.SourceLines)
                if (ScriptPageKeyCatalog.TryParseHeader(line.Text, out string key) && FindSection(line.Text) == null)
                    PageKeys.Add(Normalize(key));

            CollectValidationIssues(validation, rules, issues);
            return issues;
        }

        private void CollectValidationIssues(ScriptValidationResult validation, ScriptRules rules, List<ScriptIssue> issues)
        {
            foreach (ScriptDiagnostic diagnostic in validation.Diagnostics)
                issues.Add(FromDiagnostic(diagnostic));
            if (validation.Type == NPCScriptType.Normal || validation.Type == NPCScriptType.Called)
                CheckSections(validation.SourceLines, rules, issues);

            var seen = new HashSet<(string File, int Line, string Page, string Message)>();
            foreach (NPCPage page in validation.Pages)
            {
                foreach (NPCSegment segment in page.SegmentList)
                {
                    foreach (ScriptSourceInfo source in segment.SaySources.Concat(segment.ElseSaySources))
                        CheckValues(source, segment, true, issues, seen);
                    foreach (NPCChecks check in segment.CheckList)
                        CheckStatement(check.Source, check.Params, segment, ScriptCheckCatalog.Find(check.Type), rules, issues, seen);
                    foreach (NPCActions action in segment.ActList.Concat(segment.ElseActList))
                        CheckStatement(action.Source, action.Params, segment, ScriptActionCatalog.Find(action.Type), rules, issues, seen);
                }
            }

            foreach (ScriptValidationResult called in validation.CalledScripts)
                new ScriptProject { Type = called.Type }.CollectValidationIssues(called, rules, issues);
        }

        private static ScriptIssue FromDiagnostic(ScriptDiagnostic diagnostic)
        {
            return new ScriptIssue
            {
                Line = diagnostic.Line, FileName = diagnostic.FileName, PageKey = diagnostic.PageKey,
                Diagnostic = diagnostic, Message = diagnostic.Message,
                IsError = diagnostic.Effect == ScriptDiagnosticEffect.SegmentDisabled ||
                    diagnostic.Effect == ScriptDiagnosticEffect.SegmentAborted ||
                    diagnostic.Effect == ScriptDiagnosticEffect.CallChainStopped ||
                    diagnostic.Effect == ScriptDiagnosticEffect.ScriptNotLoaded ||
                    diagnostic.Kind == ScriptDiagnosticKind.MissingPage || diagnostic.Suppressed,
            };
        }

        /// <summary>Read the same bodies and conversions as the section loaders, without calling them.</summary>
        private static void CheckSections(IReadOnlyList<ScriptSourceLine> source, ScriptRules rules, List<ScriptIssue> issues)
        {
            var lines = source.Select(line => line.Text).ToList();
            var seen = new Dictionary<ScriptSectionKind, HashSet<string>>();
            for (int header = 0; header < lines.Count; header++)
            {
                ScriptSectionEntry section = FindSection(lines[header]);
                if (section == null) continue;
                if (!seen.TryGetValue(section.Kind, out HashSet<string> values))
                    seen.Add(section.Kind, values = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                ScriptSectionLine[] bodyLines = ScriptSectionCatalog.Body(lines, header, section.Kind).ToArray();
                CheckSectionBoundary(source, lines, header, section, bodyLines, issues);
                foreach (ScriptSectionLine body in bodyLines)
                {
                    ScriptSourceInfo location = source[body.Index].Source;
                    string line = body.Text;
                    if (ScriptLexer.IsComment(line)) continue;
                    // Repetition is legal in these loaders. Surface it as an editing hint, not a no-op.
                    string duplicateKey = string.Join(" ", line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    if (section.Kind == ScriptSectionKind.Types || section.Kind == ScriptSectionKind.UsedTypes ||
                        section.Kind == ScriptSectionKind.Quests)
                    {
                        if (int.TryParse(line, out int number)) duplicateKey = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (section.Kind == ScriptSectionKind.Recipe)
                    {
                        string[] recipe = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (recipe.Length > 0) duplicateKey = recipe[0];
                    }
                    if (duplicateKey.Length > 0 && !values.Add(duplicateKey))
                        AddSectionIssue(issues, location, section.Key,
                            "重复编写的条目：" + duplicateKey + "（请确认是否有意重复；引擎仍按原规则处理）", false);

                    switch (section.Kind)
                    {
                        case ScriptSectionKind.Types:
                        case ScriptSectionKind.UsedTypes:
                            // Body already guarantees an Int32; the loader casts it to the byte enum.
                            int type = int.Parse(line);
                            if (!Enum.IsDefined(typeof(ItemType), (ItemType)type) || type != (int)(ItemType)type)
                                AddSectionIssue(issues, location, section.Key,
                                    "物品种类编号 " + type + " 会被引擎转换为 " + (byte)(ItemType)type +
                                    "（" + (ItemType)type + "）", false);
                            break;

                        case ScriptSectionKind.Quests:
                            if (!int.TryParse(line, out int quest))
                                AddSectionIssue(issues, location, section.Key, "无效任务编号，当前行会被忽略：" + line, false);
                            else if (quest == int.MinValue)
                                AddSectionIssue(issues, location, section.Key, "任务编号绝对值超出 Int32 范围：" + line, true);
                            else if (quest != 0 && rules?.QuestExists != null && !rules.QuestExists(Math.Abs(quest)))
                                AddSectionIssue(issues, location, section.Key, "没有这个任务，当前行会被忽略：" + Math.Abs(quest), false);
                            break;

                        case ScriptSectionKind.Trade:
                        case ScriptSectionKind.Recipe:
                            // These loaders split on spaces, not tabs; recipe suffixes are not counts.
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 0) break;
                            bool missingItem = rules?.ItemExists != null && !rules.ItemExists(parts[0]);
                            if (missingItem)
                                AddSectionIssue(issues, location, section.Key, "没有这个物品，当前行不会加载：" + parts[0], false);
                            if (section.Kind == ScriptSectionKind.Trade && parts.Length == 2 && !ushort.TryParse(parts[1], out _))
                                AddSectionIssue(issues, location, section.Key,
                                    "库存数量不是 UInt16（0–65535），引擎会将数量设为 0：" + parts[1], false);
                            if (section.Kind == ScriptSectionKind.Recipe && !missingItem &&
                                rules?.RecipeExists != null && !rules.RecipeExists(parts[0]))
                                AddSectionIssue(issues, location, section.Key, "没有可用配方或配方没有材料，当前行不会加载：" + parts[0], false);
                            break;
                    }
                }
            }
        }

        private static void CheckSectionBoundary(IReadOnlyList<ScriptSourceLine> source, IList<string> lines,
            int header, ScriptSectionEntry section, ScriptSectionLine[] body, List<ScriptIssue> issues)
        {
            if (section.IsEntry == null) return;
            int stop = body.Length == 0 ? header + 1 : body[body.Length - 1].Index + 1;
            while (stop < lines.Count && string.IsNullOrEmpty(lines[stop])) stop++;
            if (stop >= lines.Count || ScriptPageKeyCatalog.TryParseHeader(lines[stop], out _) ||
                FindSection(lines[stop]) != null) return;
            AddSectionIssue(issues, source[stop].Source, section.Key,
                "该行不符合 " + section.Key + " 条目格式，数据段在此提前结束；后续条目不会由此数据段加载。", false);
        }

        private static void AddSectionIssue(List<ScriptIssue> issues, ScriptSourceInfo source, string page,
            string message, bool error)
        {
            issues.Add(new ScriptIssue
            {
                Line = source.Line, FileName = source.FileName, PageKey = page, Message = message, IsError = error,
                Diagnostic = LocalDiagnostic(source, page, ScriptDiagnosticKind.MalformedSection, message,
                    ScriptDiagnosticPhase.Load),
            });
        }

        private void CheckStatement(ScriptSourceInfo source, IList<string> parameters, NPCSegment segment, ScriptKeywordEntry keyword,
            ScriptRules rules, List<ScriptIssue> issues, HashSet<(string File, int Line, string Page, string Message)> seen)
        {
            if (source == null) return;
            ScriptDiagnosticPhase phase = keyword?.Kind == ScriptKeywordKind.Check
                ? ScriptDiagnosticPhase.Check : ScriptDiagnosticPhase.Action;
            // Execution resolves only the arguments retained by the loader, not ignored trailing text.
            foreach (string parameter in parameters)
                CheckValues(source, segment, false, issues, seen, phase, parameter);
            if (rules == null || keyword == null || keyword.Argument == ScriptArgumentKind.None) return;

            string[] parts = segment.ParseArguments(ScriptLexer.Tokenize(source.Text));
            if (parts.Length < 2 || IsDynamic(parts[1])) return;
            string message = null;
            switch (keyword.Argument)
            {
                case ScriptArgumentKind.Spell:
                    if (rules.SpellExists != null && !rules.SpellExists(parts[1])) message = "引擎不认识这个技能：" + parts[1];
                    break;
                case ScriptArgumentKind.Map:
                    if (rules.MapExists != null && !rules.MapExists(parts[1])) message = "找不到这个地图：" + parts[1];
                    break;
                case ScriptArgumentKind.Item:
                    if (rules.ItemExists != null && !rules.ItemExists(parts[1])) message = "没有这个物品：" + parts[1];
                    break;
            }
            if (message != null)
                AddLocalIssue(issues, seen, source, segment.Page.Key, ScriptDiagnosticKind.InvalidArguments, message,
                    keyword.Kind == ScriptKeywordKind.Check ? ScriptDiagnosticPhase.Check : ScriptDiagnosticPhase.Action, false);
        }

        private static bool IsDynamic(string value)
        {
            return !string.IsNullOrEmpty(value) && (value.Contains("<$") || value.Contains('%'));
        }

        private void CheckValues(ScriptSourceInfo source, NPCSegment segment, bool display, List<ScriptIssue> issues,
            HashSet<(string File, int Line, string Page, string Message)> seen,
            ScriptDiagnosticPhase phase = ScriptDiagnosticPhase.Action, string consumedText = null)
        {
            if (source == null) return;
            string text = consumedText ?? segment.ParseArguments(new[] { source.Text })[0];
            bool monster = Type == NPCScriptType.AutoMonster;
            foreach (Match match in ScriptLexer.Placeholder.Matches(text))
            {
                // Keep the resolver's normalisation, including its narrower monster value grammar.
                string name = match.Groups[1].Value.ToUpperInvariant();
                Match argument = ScriptLexer.StatementValue.Match(name);
                if (!argument.Success && !monster) argument = ScriptLexer.TwoValue.Match(name);
                if (!argument.Success && !monster) argument = ScriptLexer.OneValue.Match(name);
                if (argument.Success) name = name.Replace(argument.Groups[2].Value, string.Empty);
                if (IsDynamic(name)) continue;

                ScriptKeywordEntry keyword;
                bool found = monster ? ScriptKeywordRegistry.TryGetMonsterVariable(name, out keyword)
                    : ScriptKeywordRegistry.TryGetVariable(name, out keyword);
                if (found) continue;
                AddLocalIssue(issues, seen, source, segment.Page.Key, ScriptDiagnosticKind.UnknownValue,
                    "当前脚本类型不支持这个变量：<$" + match.Groups[1].Value + ">",
                    display ? ScriptDiagnosticPhase.Display : phase, !display);
            }
        }

        private static void AddLocalIssue(List<ScriptIssue> issues,
            HashSet<(string File, int Line, string Page, string Message)> seen, ScriptSourceInfo source, string page,
            ScriptDiagnosticKind kind, string message, ScriptDiagnosticPhase phase, bool error)
        {
            if (!seen.Add((source.FileName.ToUpperInvariant(), source.Line, page, message))) return;
            issues.Add(new ScriptIssue
            {
                Line = source.Line, FileName = source.FileName, PageKey = page, Message = message, IsError = error,
                Diagnostic = LocalDiagnostic(source, page, kind, message, phase),
            });
        }

        private static ScriptDiagnostic LocalDiagnostic(ScriptSourceInfo source, string page,
            ScriptDiagnosticKind kind, string message, ScriptDiagnosticPhase phase)
        {
            // Supplements belong to this check only: never use the global diagnostic collector.
            return new ScriptDiagnostic
            {
                Kind = kind, Category = kind == ScriptDiagnosticKind.UnknownValue
                    ? ScriptDiagnosticCategory.Unsupported : ScriptDiagnosticCategory.ScriptError,
                Phase = phase, Effect = kind == ScriptDiagnosticKind.UnknownValue
                    ? (phase == ScriptDiagnosticPhase.Display ? ScriptDiagnosticEffect.DisplayFallback : ScriptDiagnosticEffect.CallChainStopped)
                    : ScriptDiagnosticEffect.Notice,
                FileName = source.FileName, Line = source.Line, PageKey = page, Text = source.Text,
                SourceContext = source.Context, SourceVersion = source.ContentVersion, Message = message,
            };
        }
    }
}
