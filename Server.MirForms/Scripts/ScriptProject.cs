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

        /// <summary>Null when the quest list is unavailable.</summary>
        public Func<int, bool> QuestExists;
    }

    public sealed class ScriptIssue
    {
        public int Line;
        public bool IsError;
        public string Message;

        public override string ToString()
        {
            return (Line > 0 ? "第 " + Line + " 行  " : string.Empty) + Message;
        }
    }

    /// <summary>
    /// Line model of one NPC script file: pages, blocks, the caret context and the rule checks the
    /// editor runs before the engine parser sees the buffer.
    /// </summary>
    public sealed class ScriptProject
    {
        private static readonly HashSet<string> KnownTags = BuildKnownTags();

        /// <summary>Page keys of the files #INSERT names, kept until the file itself changes.</summary>
        private static readonly Dictionary<string, KeyValuePair<DateTime, HashSet<string>>> IncludeFileCache =
            new Dictionary<string, KeyValuePair<DateTime, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);

        private static HashSet<string> BuildKnownTags()
        {
            // INCLUDE and INSERT are read by the loader before a page is split, so they never reach
            // ParseSegment and have no block of their own.
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptDirectiveEntry directive in ScriptDirectiveCatalog.Entries) tags.Add(directive.Tag.Substring(1));

            foreach (ScriptBlockEntry tag in ScriptBlockCatalog.Entries) tags.Add(tag.Text.Substring(1));

            return tags;
        }

        public string[] Lines = new string[0];
        public readonly List<ScriptBlock> Blocks = new List<ScriptBlock>();
        public readonly List<ScriptPageInfo> Pages = new List<ScriptPageInfo>();
        public readonly List<ScriptPageInfo> Sections = new List<ScriptPageInfo>();
        public readonly HashSet<string> PageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static ScriptProject Parse(string text, string path = null)
        {
            var project = new ScriptProject();
            project.Lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            ScriptBlock current = null;
            ScriptPageInfo page = null;

            for (int i = 0; i < project.Lines.Length; i++)
            {
                // The engine reads a key exactly as written: "[@MAIN] " with a trailing space is text to
                // the loader, so the editor must not show it as a page.
                string raw = project.Lines[i].TrimStart('\uFEFF');
                string trimmed = raw.Trim();

                ScriptSectionEntry section = FindSection(trimmed);

                if (section != null)
                {
                    if (current != null) current.EndLine = i;

                    current = null;

                    page = new ScriptPageInfo { Key = trimmed, Line = i + 1, IsSection = true, Section = section.Kind };
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

                if (!trimmed.StartsWith("#")) continue;

                string tag = trimmed.Substring(1).Split(' ', '\t')[0];
                ScriptBlockKind kind;

                if (!ScriptBlockCatalog.TryParse(tag, out kind)) continue;

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
                project.Pages[i].EndLine = i + 1 < project.Pages.Count ? project.Pages[i + 1].Line - 1 : project.Lines.Length;
            }

            project.AddIncludedPageKeys(path);

            return project;
        }

        /// <summary>Section whose header the line is, by the loader's own rule.</summary>
        private static ScriptSectionEntry FindSection(string trimmed)
        {
            foreach (ScriptSectionEntry entry in ScriptSectionCatalog.Ordered)
            {
                if (entry.MatchesHeader(trimmed)) return entry;
            }

            return null;
        }

        /// <summary>
        /// Pages that come from #INSERT files are not in this text, so a button pointing at one would be
        /// reported as missing. Read the files the directive names and register their keys; a file that
        /// cannot be resolved is left for the server to report.
        /// </summary>
        private void AddIncludedPageKeys(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string envir = IncludeRoot(path);

            foreach (string line in Lines)
            {
                ScriptDirectiveEntry directive;

                if (!ScriptDirectiveCatalog.TryParse(line, out directive)) continue;
                if (directive != ScriptDirectiveCatalog.Insert) continue;

                string[] parts = ScriptLexer.Tokenize(line);

                if (parts.Length < 2) continue;

                string file;

                try
                {
                    file = Path.GetFullPath(Path.Combine(envir, parts[1].Trim('"', '[', ']')));
                }
                catch (Exception)
                {
                    continue;
                }

                HashSet<string> keys = IncludedKeys(file);

                if (keys != null) PageKeys.UnionWith(keys);
            }
        }

        /// <summary>
        /// Page keys of one inserted file, cached until the file changes. The editor parses on idle for
        /// its palette, so a script that inserts ten files must not read them ten times per keystroke.
        /// </summary>
        private static HashSet<string> IncludedKeys(string file)
        {
            DateTime stamp;

            try
            {
                if (!File.Exists(file)) return null;

                stamp = File.GetLastWriteTimeUtc(file);
            }
            catch (Exception)
            {
                return null;
            }

            KeyValuePair<DateTime, HashSet<string>> cached;

            if (IncludeFileCache.TryGetValue(file, out cached) && cached.Key == stamp) return cached.Value;

            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string included in File.ReadAllLines(file))
            {
                string key;

                if (ScriptPageKeyCatalog.TryParseHeader(included.TrimStart('\uFEFF'), out key))
                    keys.Add(Normalize(key));
            }

            IncludeFileCache[file] = new KeyValuePair<DateTime, HashSet<string>>(stamp, keys);

            return keys;
        }

        /// <summary>The Envir folder a directive path is relative to: the parent of the NPCs tree.</summary>
        private static string IncludeRoot(string path)
        {
            string directory = Path.GetDirectoryName(path);

            DirectoryInfo info = directory == null ? null : new DirectoryInfo(directory);

            while (info != null && !string.Equals(info.Name, "NPCs", StringComparison.OrdinalIgnoreCase))
                info = info.Parent;

            return info != null && info.Parent != null ? info.Parent.FullName : directory ?? ".";
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

        /// <summary>
        /// Lines already written in a section, used both to spot duplicates and to keep the candidate
        /// list free of repeats. The whole line counts: the same potion with a different stack size is a
        /// separate entry on purpose.
        /// </summary>
        public HashSet<string> SectionValues(ScriptPageInfo section)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = section.Line + 1; i <= section.EndLine && i <= Lines.Length; i++)
            {
                string key = SectionKey(Lines[i - 1]);

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

            ScriptBlockKind kind = ScriptBlockKind.Page;

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
            string key = pageKey.Trim().Trim('[', ']');

            int paren = key.IndexOf('(');
            if (paren > 0) key = key.Substring(0, paren) + ")";

            return key.ToUpperInvariant();
        }

        /// <summary>Rule checks that do not need the engine: unknown keywords, arity, links and values.</summary>
        public List<ScriptIssue> Check(ScriptRules rules = null)
        {
            var issues = new List<ScriptIssue>();
            var seenPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptPageInfo page in Pages)
            {
                if (!seenPages.Add(Normalize(page.Key)))
                {
                    issues.Add(new ScriptIssue { Line = page.Line, Message = "页面重复定义：" + page.Key });
                }
            }

            foreach (ScriptPageInfo section in Sections)
            {
                CheckSection(section, rules, issues);
            }

            foreach (ScriptBlock block in Blocks)
            {
                for (int i = block.StartLine; i < block.EndLine; i++)
                {
                    string raw = Lines[i - 1];
                    string trimmed = raw.Trim();

                    if (trimmed.Length == 0) continue;

                    if (ScriptLexer.IsComment(trimmed)) continue;

                    if (trimmed.StartsWith("#"))
                    {
                        string tag = trimmed.Substring(1).Split(' ', '\t')[0].ToUpperInvariant();

                        if (!KnownTags.Contains(tag))
                        {
                            issues.Add(new ScriptIssue
                            {
                                Line = i,
                                IsError = true,
                                Message = "引擎不认识的指令：#" + tag + "（这一行会被跳过）",
                            });
                        }

                        continue;
                    }

                    CheckKeywords(block, i, trimmed, rules, issues);
                    CheckLinks(i, trimmed, issues);
                    CheckValues(i, trimmed, issues);
                }
            }

            return issues;
        }

        /// <summary>
        /// Sections are plain data: item type numbers, goods names, quest numbers. The engine drops
        /// what it cannot read without a word, so they are worth checking here.
        /// </summary>
        private void CheckSection(ScriptPageInfo section, ScriptRules rules, List<ScriptIssue> issues)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = section.Line + 1; i <= section.EndLine && i <= Lines.Length; i++)
            {
                string line = Lines[i - 1].Trim();

                if (line.Length == 0 || ScriptLexer.IsComment(line)) continue;

                switch (section.Section)
                {
                    case ScriptSectionKind.Types:
                    case ScriptSectionKind.UsedTypes:
                        if (!int.TryParse(line, out int type))
                        {
                            issues.Add(new ScriptIssue { Line = i, IsError = true, Message = "物品种类只能填编号：" + line });
                        }
                        else if (!Enum.IsDefined(typeof(ItemType), (ItemType)type))
                        {
                            issues.Add(new ScriptIssue { Line = i, Message = "没有这个物品种类编号：" + type });
                        }

                        ReportDuplicate(seen, line, i, issues);
                        break;

                    case ScriptSectionKind.Quests:
                        if (!int.TryParse(line, out int questIndex))
                        {
                            issues.Add(new ScriptIssue { Line = i, IsError = true, Message = "任务编号只能是数字：" + line });
                        }
                        else
                        {
                            int number = Math.Abs(questIndex);

                            if (rules?.QuestExists != null && !rules.QuestExists(number))
                            {
                                issues.Add(new ScriptIssue
                                {
                                    Line = i,
                                    IsError = true,
                                    Message = "没有这个任务：" + number + "（引擎遇到它会直接丢掉后面所有任务行）",
                                });
                            }

                            ReportDuplicate(seen, line, i, issues);
                        }

                        break;

                    case ScriptSectionKind.Trade:
                    case ScriptSectionKind.Recipe:
                        string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length == 0) break;

                        if (parts.Length > 1 && !int.TryParse(parts[1], out _))
                        {
                            issues.Add(new ScriptIssue { Line = i, Message = "数量必须是数字：" + parts[1] });
                        }

                        if (rules?.ItemExists != null && !rules.ItemExists(parts[0]))
                        {
                            issues.Add(new ScriptIssue { Line = i, Message = "没有这个物品：" + parts[0] });
                        }

                        ReportDuplicate(seen, line, i, issues);
                        break;
                }
            }
        }

        private static void ReportDuplicate(HashSet<string> seen, string line, int number, List<ScriptIssue> issues)
        {
            string key = SectionKey(line);

            if (key.Length == 0 || seen.Add(key)) return;

            issues.Add(new ScriptIssue { Line = number, Message = "重复的条目：" + key + "（这一行不起作用）" });
        }

        private void CheckKeywords(ScriptBlock block, int line, string trimmed, ScriptRules rules, List<ScriptIssue> issues)
        {
            if (block.Kind != ScriptBlockKind.Check &&
                block.Kind != ScriptBlockKind.Act &&
                block.Kind != ScriptBlockKind.ElseAct) return;

            string[] parts = ScriptLexer.Tokenize(trimmed);

            if (parts.Length == 0) return;

            if (ScriptLexer.IsComment(parts[0])) return;

            ScriptKeywordEntry keyword;

            bool found = block.Kind == ScriptBlockKind.Check
                ? ScriptKeywordRegistry.TryGetCheck(parts[0], out keyword)
                : ScriptKeywordRegistry.TryGetAction(parts[0], out keyword);

            if (!found)
            {
                issues.Add(new ScriptIssue
                {
                    Line = line,
                    IsError = true,
                    Message = (block.Kind == ScriptBlockKind.Check ? "未知条件：" : "未知动作：") + parts[0],
                });

                return;
            }

            if (parts.Length < keyword.MinParts)
            {
                issues.Add(new ScriptIssue
                {
                    Line = line,
                    Message = keyword.Name + " 需要 " + (keyword.MinParts - 1) + " 个参数，当前 " + (parts.Length - 1) + " 个",
                });
            }

            if (rules == null) return;

            if (keyword.Argument == ScriptArgumentKind.Spell && parts.Length > 1 && rules.SpellExists != null && !rules.SpellExists(parts[1]))
            {
                issues.Add(new ScriptIssue { Line = line, Message = "引擎不认识这个技能：" + parts[1] });
            }

            if (keyword.Argument == ScriptArgumentKind.Map && parts.Length > 1 && rules.MapExists != null && !rules.MapExists(parts[1]))
            {
                issues.Add(new ScriptIssue { Line = line, Message = "找不到这个地图：" + parts[1] });
            }
        }

        private void CheckLinks(int line, string trimmed, List<ScriptIssue> issues)
        {
            foreach (Match match in ScriptLexer.Button.Matches(trimmed))
            {
                string target = match.Groups[1].Value.Split('/')[0];

                if (string.Equals(target, "@Exit", StringComparison.OrdinalIgnoreCase)) continue;

                if (!PageKeys.Contains(Normalize(target)))
                {
                    issues.Add(new ScriptIssue { Line = line, Message = "按钮指向不存在的页面：" + target });
                }
            }

            string[] parts = ScriptLexer.Tokenize(trimmed);

            if (parts.Length == 0) return;

            string targetKey = null;

            ScriptKeywordEntry action = ScriptActionCatalog.Find(parts[0]);

            if (action == null) return;

            if (action.Button == ScriptButtonKind.DelayedPage && parts.Length > 2)
                targetKey = parts[2];
            else if ((action.Button == ScriptButtonKind.Page || action.Button == ScriptButtonKind.Roll) && parts.Length > 1)
                targetKey = parts[1];

            if (targetKey == null || targetKey.StartsWith("#")) return;

            if (!PageKeys.Contains(Normalize(targetKey)))
            {
                issues.Add(new ScriptIssue { Line = line, Message = parts[0] + " 指向不存在的页面：" + targetKey });
            }
        }

        private void CheckValues(int line, string trimmed, List<ScriptIssue> issues)
        {
            if (!trimmed.Contains("<$")) return;

            foreach (Match match in ScriptLexer.Placeholder.Matches(trimmed))
            {
                string name = match.Groups[1].Value.ToUpperInvariant();
                int paren = name.IndexOf('(');

                if (paren >= 0) name = name.Substring(0, paren) + "()";

                ScriptKeywordEntry keyword;

                if (ScriptKeywordRegistry.TryGetVariable(name, out keyword)) continue;
                if (ScriptKeywordRegistry.TryGetMonsterVariable(name, out keyword)) continue;

                issues.Add(new ScriptIssue { Line = line, Message = "未知变量：<$" + match.Groups[1].Value + ">" });
            }
        }
    }
}
