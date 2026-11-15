using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Server.MirEnvir;

namespace Server.MirObjects
{
    /// <summary>
    /// Turns one script file into pages: page headers, segment splitting and keyword parsing. It reads
    /// no world state and publishes nothing, so the same compile serves a live load and a tool buffer.
    /// </summary>
    internal sealed class ScriptCompiler
    {
        private readonly string _fileName;
        private readonly NPCScriptType _type;

        /// <summary>
        /// Pages land in the host's list, because a page chain must see the pages already parsed for
        /// the same file and the host keeps them for execution.
        /// </summary>
        private readonly List<ScriptPage> _pages;

        /// <summary>Source lines with their file numbers, shared by every page of one load.</summary>
        private List<ScriptSourceLine> _scriptLines;
        private List<ScriptSourceLine> _expandedSourceLines;

        /// <summary>Normalised page header to its line in <see cref="_scriptLines"/>.</summary>
        private Dictionary<string, int> _pageHeaderIndex;

        internal ScriptCompiler(string fileName, NPCScriptType type, List<ScriptPage> pages)
        {
            _fileName = fileName;
            _type = type;
            _pages = pages;
        }

        /// <summary>
        /// Parses one file into pages and, for automatic scripts, records the markers the loader has to
        /// apply. A tool compiles the same way but only checks arguments, so it never looks up a map, a
        /// monster or a robot: those reads are what the live plan exists for.
        /// </summary>
        internal ScriptCompilation Compile(List<ScriptSourceLine> sourceLines, ScriptCompileMode mode)
        {
            PrepareScriptSourceLines(sourceLines);

            var compilation = new ScriptCompilation
            {
                FileName = _fileName + ".txt",
                Type = _type,
                SourceLines = sourceLines,
                Lines = sourceLines.Select(line => line.Text).ToList(),
            };

            List<string> lines = compilation.Lines;

            ReportDuplicatePages(lines);

            if (_type == NPCScriptType.Normal || _type == NPCScriptType.Called)
            {
                ParsePages(lines);
                return compilation;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (ScriptPageKeyCatalog.Find(lines[i]) != ScriptPageKeyCatalog.Auto) continue;

                ScriptAutoMarkerLine markerLine;
                ScriptAutoMarkerEntry marker = ScriptAutoMarkerCatalog.Find(_type, lines[i], out markerLine);

                if (marker != null)
                {
                    if (mode == ScriptCompileMode.Tool)
                    {
                        // Same row, same argument checks, no world read.
                        marker.ValidateArguments?.Invoke(markerLine);
                        ParsePages(lines, lines[i]);
                        continue;
                    }

                    object payload = marker.ReadPlan(markerLine);

                    if (payload != null)
                    {
                        compilation.AutoPlans.Add(new ScriptAutoMarkerPlan(marker, markerLine, payload));
                        continue;
                    }
                }

                ParsePages(lines, lines[i]);
            }

            return compilation;
        }

        private static List<ScriptSourceLine> BuildScriptLines(IList<string> scriptLines)
        {
            var lines = new List<ScriptSourceLine>(scriptLines.Count);

            for (int i = 0; i < scriptLines.Count; i++)
            {
                if (string.IsNullOrEmpty(scriptLines[i])) continue;

                lines.Add(new ScriptSourceLine(scriptLines[i], i + 1, ScriptParseScope.FileName));
            }

            return lines;
        }

        private void PrepareScriptSourceLines(List<ScriptSourceLine> sourceLines)
        {
            _expandedSourceLines = sourceLines;
            _scriptLines = sourceLines.Where(line => !string.IsNullOrEmpty(line.Text)).ToList();
            _pageHeaderIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _scriptLines.Count; i++)
            {
                string header;
                string[] args;

                if (!ScriptPageKeyCatalog.TryParseHeader(_scriptLines[i].Text, out header)) continue;

                string normalized = ScriptPage.NormalizeSectionName(header, out args);

                if (!_pageHeaderIndex.ContainsKey(normalized)) _pageHeaderIndex[normalized] = i;
            }
        }

        /// <summary>
        /// First line whose header matches the normalised key. The index is only trusted when the
        /// header is written exactly as the key, so a page defined with arguments still falls back to
        /// the original prefix scan instead of changing what counts as a match.
        /// </summary>
        private int FindPageHeader(string tempSectionName, IList<ScriptSourceLine> lines)
        {
            int index;

            if (ReferenceEquals(lines, _scriptLines) && _pageHeaderIndex != null && _pageHeaderIndex.TryGetValue(tempSectionName, out index))
            {
                if (string.Equals(_scriptLines[index].Text, tempSectionName, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Text;

                if (ScriptLexer.IsComment(line)) continue;

                if (line.StartsWith(tempSectionName, StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }

        /// <summary>
        /// Parses the pages no button points at, only to report what is wrong inside them. Nothing opens a
        /// dead page, so without this its unknown keywords stay invisible until someone wires it up.
        /// </summary>
        internal void ReportUnreachablePages(IList<string> lines)
        {
            var parsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ScriptPage page in _pages) parsed.Add(page.Key);

            for (int i = 0; i < lines.Count; i++)
            {
                string key;

                if (!ScriptPageKeyCatalog.TryParseHeader(lines[i], out key)) continue;
                if (!parsed.Add(key)) continue;

                ScriptDiagnostics.DeepScanned++;

                ParsePage(lines, key);
            }
        }

        /// <summary>
        /// Reports a page header that appears more than once in the same file. The loader keeps the
        /// first copy, so without this the later bodies vanish without a word.
        /// </summary>
        private void ReportDuplicatePages(IList<string> lines)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Count; i++)
            {
                string key;

                if (!ScriptPageKeyCatalog.TryParseHeader(lines[i], out key)) continue;
                if (seen.Add(key)) continue;

                ScriptDiagnostics.Report(ScriptDiagnosticKind.DuplicatePage, new ScriptParseContext
                {
                    Line = lines[i],
                    LineNumber = SourceAt(lines, i).Number,
                    FileName = SourceAt(lines, i).FileName,
                    Source = SourceAt(lines, i).Source,
                }, key, "Duplicate page \"" + key + "\"");
            }
        }

        /// <summary>
        /// Reports a button or jump target that no page header answers to. Without this the target
        /// used to become an empty page, so the click did nothing and nothing was logged.
        /// </summary>
        private void ReportMissingPage(string key)
        {
            // A key built from a run-time value has no page yet; only a literal target can be dead.
            if (string.IsNullOrEmpty(key) || key.IndexOf("<$", StringComparison.Ordinal) >= 0) return;
            if (ScriptValidationScope.IsActive && (ScriptLexer.Argument.IsMatch(key) ||
                key.IndexOf("%INPUTSTR", StringComparison.Ordinal) >= 0)) return;

            string target = key.Trim('[', ']');
            ScriptSourceInfo source = _scriptLines?.FirstOrDefault(line =>
                line.Text.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0).Source;
            ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingPage, new ScriptParseContext
            {
                FileName = ScriptParseScope.FileName,
                Source = source,
            }, key, "Page not found: " + key);
        }

        /// <summary>
        /// Parses one page and the pages its buttons open, then appends them to the host's list. Every
        /// exit appends, so a page without a body still reaches the caller as an empty carrier.
        /// </summary>
        internal List<ScriptPage> ParsePages(IList<string> lines, string key = null)
        {
            List<ScriptPage> pages = ParsePageChain(lines, key);

            _pages.AddRange(pages);

            return pages;
        }

        private List<ScriptPage> ParsePageChain(IList<string> lines, string key)
        {
            if (key == null) key = ScriptSpecialPageCatalog.Key(ScriptSpecialPageKind.Main);

            List<ScriptPage> pages = new List<ScriptPage>();
            List<string> buttons = new List<string>();
            var visited = new HashSet<string>(_pages.Select(existing => existing.Key), StringComparer.OrdinalIgnoreCase);

            if (!visited.Add(key)) return pages;
            if (ScriptValidationScope.Current?.TryConsumePage(lines.Count, key,
                _scriptLines?.FirstOrDefault().Source) == false) return pages;

            ScriptPage page = ParsePage(lines, key);

            if (page == null)
            {
                // [@MAIN] has no body only when the file is not a dialogue script; the engine still
                // needs its page object so a caller can open an empty window instead of failing.
                if (ScriptSpecialPageCatalog.Find(key) == null) ReportMissingPage(key);
                else pages.Add(new ScriptPage(key) { FileName = _fileName + ".txt" });

                return pages;
            }

            pages.Add(page);

            buttons.AddRange(page.Buttons);

            for (int i = 0; i < buttons.Count; i++)
            {
                string section = buttons[i];

                if (!visited.Add(section)) continue;
                if (ScriptValidationScope.Current?.TryConsumePage(lines.Count, section, page?.Source) == false) break;

                page = ParsePage(lines, section);

                if (page == null)
                {
                    // A special page is answered by the engine, not by script text: it keeps an empty
                    // page as its carrier so Response still reaches ProcessSpecial. @Exit is closed
                    // by the client and never travels here.
                    if (ScriptSpecialPageCatalog.Find(section) != null ||
                        section.StartsWith("[@@", StringComparison.OrdinalIgnoreCase))
                    {
                        // Input pages are driven by the client's text prompt, so they need the same
                        // empty carrier a special page does.
                        pages.Add(new ScriptPage(section) { FileName = _fileName + ".txt" });
                    }
                    else if (!section.StartsWith("[@_", StringComparison.OrdinalIgnoreCase) &&
                             !string.Equals(section, "[@EXIT]", StringComparison.OrdinalIgnoreCase))
                    {
                        ReportMissingPage(section);
                    }

                    continue;
                }

                buttons.AddRange(page.Buttons);

                pages.Add(page);
            }

            return pages;
        }

        private ScriptPage ParsePage(IList<string> scriptLines, string sectionName)
        {
            // Numbers travel with the text: empty lines are dropped here, so the index in this list is
            // no longer the line the script file has. The list is built once per load and shared by
            // every page, which is why a page no longer rebuilds it.
            var lines = _scriptLines ?? BuildScriptLines(scriptLines);

            ScriptPage Page = new ScriptPage(sectionName) { FileName = _fileName + ".txt" };

            //Cleans arguments out of search page name
            string tempSectionName = Page.ArgumentParse(sectionName);

            int header = FindPageHeader(tempSectionName, lines);

            if (header < 0) return null;
            Page.Source = lines[header].Source;

            // Establish the page interval before looking at block boundaries. Looking only at the
            // next line lets an adjacent empty page absorb the following page's first #IF block.
            int end = header + 1;

            while (end < lines.Count && !ScriptPageKeyCatalog.TryParseHeader(lines[end].Text, out _)) end++;

            var segmentLines = new List<ScriptSourceLine>();

            for (int i = header + 1; i < end; i++)
            {
                if (ScriptBlockCatalog.SplitsSegment(lines[i].Text) && segmentLines.Count > 0)
                {
                    AddPageSegment(Page, segmentLines);
                    segmentLines.Clear();
                }

                segmentLines.Add(lines[i]);
            }

            if (segmentLines.Count > 0) AddPageSegment(Page, segmentLines);

            return Page;
        }

        private void AddPageSegment(ScriptPage page, List<ScriptSourceLine> lines)
        {
            ScriptSegment segment = ParseSegment(page, lines);
            page.Buttons.AddRange(segment.Buttons);
            page.Buttons.AddRange(segment.ElseButtons);
            page.Buttons.AddRange(segment.GotoButtons);
            page.SegmentList.Add(segment);
        }

        private ScriptSourceLine SourceAt(IList<string> lines, int index)
        {
            return _expandedSourceLines != null && index < _expandedSourceLines.Count
                ? _expandedSourceLines[index]
                : new ScriptSourceLine(lines[index], index + 1, ScriptParseScope.FileName);
        }

        private ScriptSegment ParseSegment(ScriptPage page, IEnumerable<ScriptSourceLine> scriptLines)
        {
            List<ScriptSourceLine>
                checks = new List<ScriptSourceLine>(),
                acts = new List<ScriptSourceLine>(),
                elseActs = new List<ScriptSourceLine>();

            List<string>
                say = new List<string>(),
                buttons = new List<string>(),
                elseSay = new List<string>(),
                elseButtons = new List<string>(),
                gotoButtons = new List<string>();

            List<ScriptSourceLine> lines = scriptLines.ToList();

            // Tag to destination. The segment asks which role the tag carries, not which tag it was,
            // so a new block type that reuses a role needs no code here.
            var roleLines = new Dictionary<ScriptBlockRole, List<ScriptSourceLine>>
            {
                { ScriptBlockRole.Check, checks },
                { ScriptBlockRole.Act, acts },
                { ScriptBlockRole.ElseAct, elseActs },
            };

            var roleText = new Dictionary<ScriptBlockRole, List<string>>
            {
                { ScriptBlockRole.Say, say },
                { ScriptBlockRole.ElseSay, elseSay },
            };

            var roleButtons = new Dictionary<ScriptBlockRole, List<string>>
            {
                { ScriptBlockRole.Say, buttons },
                { ScriptBlockRole.ElseSay, elseButtons },
            };

            // A segment starts in the say block, exactly as the old single list did.
            List<ScriptSourceLine> currentLines = null;
            List<string> currentSay = say, currentButtons = buttons;
            ScriptSegment segment = new ScriptSegment(page, say, buttons, elseSay, elseButtons, gotoButtons);
            segment.Source = lines.FirstOrDefault().Source ?? page.Source;

            for (int i = 0; i < lines.Count; i++)
            {
                string text = lines[i].Text;

                if (lines[i].Error != null)
                {
                    segment.MarkInvalid(lines[i].Error, false);
                    continue;
                }

                if (ScriptLexer.IsComment(text)) continue;

                if (text.TrimStart().StartsWith("#"))
                {
                    ScriptBlockKind kind;
                    ScriptBlockEntry block = ScriptBlockCatalog.TryFind(text, out kind);

                    if (block == null)
                    {
                        segment.MarkInvalid(ReportUnknownBlock(page, lines[i]), false);
                        continue;
                    }

                    List<ScriptSourceLine> blockLines;
                    List<string> blockText;

                    if (roleLines.TryGetValue(block.Role, out blockLines))
                    {
                        currentLines = blockLines;
                        currentSay = null;
                    }
                    else
                    {
                        currentLines = null;
                        currentSay = roleText.TryGetValue(block.Role, out blockText) ? blockText : null;
                    }

                    List<string> blockButtons;
                    currentButtons = roleButtons.TryGetValue(block.Role, out blockButtons) ? blockButtons : null;

                    continue;
                }

                if (ScriptPageKeyCatalog.TryParseHeader(text, out _)) break;

                if (currentButtons != null)
                {
                    string buttonText = segment.ParseArguments(new[] { text })[0];
                    Match match = ScriptLexer.Button.Match(buttonText);

                    while (match.Success)
                    {
                        string argu = match.Groups[1].Captures[0].Value;
                        argu = argu.Split('/')[0];

                        currentButtons.Add(string.Format("[{0}]", argu));
                        match = match.NextMatch();
                    }
                }

                if (currentLines != null) currentLines.Add(new ScriptSourceLine(text.TrimEnd(), lines[i].Number, lines[i].FileName, lines[i].Source));
                else if (currentSay != null)
                {
                    currentSay.Add(text.TrimEnd());
                    (ReferenceEquals(currentSay, say) ? segment.SaySources : segment.ElseSaySources).Add(lines[i].Source);
                }
            }

            string previousFile = ScriptParseScope.FileName;
            string root = ScriptParseScope.Root ?? Settings.NPCPath;

            try
            {
                foreach (ScriptSourceLine line in checks)
                {
                    ScriptParseScope.Set(line.FileName ?? previousFile, root);
                    segment.ParseCheck(line.Text, line.Number, line.Source);
                }

                foreach (ScriptSourceLine line in acts)
                {
                    ScriptParseScope.Set(line.FileName ?? previousFile, root);
                    segment.ParseAct(segment.ActList, line.Text, line.Number, line.Source);
                }

                foreach (ScriptSourceLine line in elseActs)
                {
                    ScriptParseScope.Set(line.FileName ?? previousFile, root);
                    segment.ParseAct(segment.ElseActList, line.Text, line.Number, line.Source);
                }
            }
            finally
            {
                ScriptParseScope.Set(previousFile, root);
            }

            CollectActionButtons(segment.ActList, buttons, gotoButtons);
            CollectActionButtons(segment.ElseActList, elseButtons, gotoButtons);

            return segment;
        }

        private static void CollectActionButtons(IEnumerable<ScriptActions> acts, List<string> buttons, List<string> gotoButtons)
        {
            foreach (ScriptActions action in acts)
            {
                ScriptKeywordEntry entry = ScriptActionCatalog.Find(action.Type);

                if (entry == null || entry.Button == ScriptButtonKind.None) continue;

                int target = entry.Button == ScriptButtonKind.DelayedPage ? 1 : 0;

                if (action.Params.Count <= target || string.IsNullOrEmpty(action.Params[target])) continue;

                string button = "[" + action.Params[target] + "]";

                if (entry.Button == ScriptButtonKind.Roll) buttons.Add(button);
                else gotoButtons.Add(button);
            }
        }

        /// <summary>
        /// An unsupported block makes its segment unsafe; its body must not inherit the previous role.
        /// </summary>
        private static ScriptDiagnostic ReportUnknownBlock(ScriptPage page, ScriptSourceLine line)
        {
            string text = (line.Text ?? string.Empty).Trim();
            int end = text.IndexOfAny(new[] { ' ', '\t' });
            string tag = end >= 0 ? text.Substring(0, end) : text;

            return ScriptDiagnostics.Report(ScriptDiagnosticKind.UnknownBlock, new ScriptParseContext
            {
                Line = line.Text,
                LineNumber = line.Number,
                FileName = line.FileName ?? ScriptParseScope.FileName,
                PageKey = page == null ? string.Empty : page.Key,
                Source = line.Source,
            }, tag, "Unknown block tag \"" + tag + "\"");
        }
    }
}
