using Server.Scripting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Server.MirEnvir;
using Server.MirObjects;

namespace Server.MirForms.Scripts
{
    /// <summary>
    /// NPC script editor: file tree, structure outline, a keyword palette filtered by the block the
    /// caret sits in, syntax colouring and validation that runs both the rule checks and the engine.
    /// </summary>
    public sealed partial class ScriptEditorForm : Form
    {
        private static readonly Regex TagPattern = new Regex(@"^#[A-Za-z]+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex QuestCommentPattern = new Regex(@"^//.*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex CommentPattern = new Regex(@"^[ \t]*(;|//).*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ValuePattern = new Regex(@"<\$[^>\r\n]+>", RegexOptions.Compiled);
        // The client splits this on '/' and renders the first part in the second part's colour name.
        private static readonly Regex ColourTextPattern = new Regex(@"\{[^{}\r\n]*?/[^{}\r\n]*?\}", RegexOptions.Compiled);
        private static readonly Regex LineKeywordPattern = new Regex(@"^[ \t]*([A-Za-z][A-Za-z0-9_()]*)", RegexOptions.Compiled);

        private static readonly Color PageColor = Color.FromArgb(0, 51, 153);
        private static readonly Color TagColor = Color.FromArgb(128, 0, 128);
        private static readonly Color CommentColor = Color.FromArgb(128, 128, 128);
        private static readonly Color ButtonColor = Color.FromArgb(0, 102, 204);
        private static readonly Color ValueColor = Color.FromArgb(153, 51, 0);
        private static readonly Color ColourTextColor = Color.FromArgb(0, 102, 102);
        private static readonly Color KeywordColor = Color.FromArgb(0, 102, 51);

        private const int WmSetRedraw = 0x000B;
        private const int EmGetScrollPos = 0x04DD;
        private const int EmSetScrollPos = 0x04DE;

        [StructLayout(LayoutKind.Sequential)]
        private struct ScrollPoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref ScrollPoint lParam);

        private readonly TreeView _files = new TreeView();
        private readonly RichTextBox _editor = new RichTextBox();
        private readonly TabControl _side = new TabControl();
        private readonly ListBox _palette = new ListBox();
        private readonly ListBox _structurePalette = new ListBox();
        private readonly TextBox _paletteFilter = new TextBox();
        private readonly TreeView _structure = new TreeView();
        private readonly ListView _problems = new ListView();
        private readonly TextBox _problemDetail = new TextBox();
        private readonly ToolStripComboBox _scriptType = new ToolStripComboBox();
        private readonly ToolStripStatusLabel _statusText = new ToolStripStatusLabel();
        private readonly ToolStripStatusLabel _statusIssues = new ToolStripStatusLabel();
        private readonly System.Windows.Forms.Timer _idleTimer = new System.Windows.Forms.Timer { Interval = 700 };
        private readonly List<ScriptKeywordEntry> _paletteSource = new List<ScriptKeywordEntry>();
        private readonly List<StructureCandidate> _structureCandidates = new List<StructureCandidate>();
        private readonly List<StructureCandidate> _valueCandidates = new List<StructureCandidate>();
        private List<string> _itemNames;
        private List<StructureCandidate> _questCandidates;
        private List<StructureCandidate> _itemCandidates;
        private List<StructureCandidate> _recipeCandidates;
        private List<StructureCandidate> _typeCandidates;
        private string _paletteHint = string.Empty;

        private string _path;
        private string _savedText = string.Empty;
        private int _textRevision;
        private int _highlightRevision = -1;
        private string _structureSignature;
        private string _problemsSignature;
        private bool _dirty;
        private bool _suppress;
        private bool _highlight = true;
        private bool _autoCheck = true;
        private HashSet<string> _referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ScriptEditorForm()
        {
            Text = GameLanguage.Get("Script Editor");
            StartPosition = FormStartPosition.CenterParent;
            Width = 1200;
            Height = 780;
            MinimumSize = new Size(900, 600);

            BuildUi();
            RefreshDocumentTools();
            LoadFileTree();
        }

        private void BuildUi()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            toolbar.Items.Add(Button(GameLanguage.Get("Open"), (s, e) => OpenSelectedFile()));
            toolbar.Items.Add(Button(GameLanguage.Get("Save"), (s, e) => SaveFile()));
            toolbar.Items.Add(new ToolStripSeparator());
            _checkButton = Button(GameLanguage.Get("Check"), (s, e) => CheckBuffer(true));
            toolbar.Items.Add(_checkButton);
            toolbar.Items.Add(new ToolStripSeparator());

            toolbar.Items.Add(_templates);
            toolbar.Items.Add(new ToolStripSeparator());
            _scriptType.DropDownStyle = ComboBoxStyle.DropDownList;
            _scriptType.Items.Add(GameLanguage.Get("Automatic script type"));
            foreach (NPCScriptType type in Enum.GetValues(typeof(NPCScriptType))) _scriptType.Items.Add(type);
            _scriptType.SelectedIndex = 0;
            _scriptType.Items.Add(new DocumentKindChoice(ScriptDocumentKind.Quest, GameLanguage.Get("Quest definition")));
            _scriptType.Items.Add(new DocumentKindChoice(ScriptDocumentKind.Text, GameLanguage.Get("Plain text")));
            _scriptType.SelectedIndexChanged += ScriptModeChanged;
            toolbar.Items.Add(_scriptType);

            var highlight = new ToolStripButton(GameLanguage.Get("Highlight")) { Checked = true, CheckOnClick = true };
            highlight.CheckedChanged += (s, e) => { _highlight = highlight.Checked; ApplyHighlight(true); };
            toolbar.Items.Add(highlight);

            var autoCheck = new ToolStripButton(GameLanguage.Get("Auto Check")) { Checked = true, CheckOnClick = true };
            autoCheck.CheckedChanged += (s, e) => { _autoCheck = autoCheck.Checked; if (!_autoCheck) _idleTimer.Stop(); };
            toolbar.Items.Add(autoCheck);

            var status = new StatusStrip();
            _statusText.Spring = true;
            _statusText.TextAlign = ContentAlignment.MiddleLeft;
            status.Items.Add(_statusText);
            status.Items.Add(_statusIssues);

            var root = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
            };

            var body = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2,
            };

            _files.Dock = DockStyle.Fill;
            _files.HideSelection = false;
            _files.DoubleClick += (s, e) => OpenSelectedFile();
            root.Panel1.Controls.Add(_files);

            _editor.Dock = DockStyle.Fill;
            _editor.Font = new Font("Consolas", 10F);
            _editor.WordWrap = false;
            _editor.AcceptsTab = true;
            _editor.HideSelection = false;
            _editor.ScrollBars = RichTextBoxScrollBars.Both;
            _editor.DetectUrls = false;
            _editor.TextChanged += (s, e) =>
            {
                if (_suppress) return;

                _dirty = _editor.Text != _savedText;
                _textRevision++;
                UpdateTitle();
                RefreshPalette();
                ScheduleIdle();
            };
            _editor.SelectionChanged += (s, e) => { if (_suppress) return; RefreshPalette(); UpdateStatus(); };
            _editor.MouseDown += EditorMouseDown;
            body.Panel1.Controls.Add(_editor);

            var palettePage = new TabPage(GameLanguage.Get("Commands"));

            var paletteLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            paletteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            paletteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            paletteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            paletteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            paletteLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _structurePalette.Dock = DockStyle.Fill;
            _structurePalette.IntegralHeight = false;
            _structurePalette.MouseDoubleClick += (s, e) => InsertFrom(_structurePalette, e.Location);
            _structurePalette.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) InsertFrom(_structurePalette, null); };

            _paletteFilter.Dock = DockStyle.Fill;
            _paletteFilter.PlaceholderText = GameLanguage.Get("Filter");
            _paletteFilter.TextChanged += (s, e) => FillPalette();

            _palette.Dock = DockStyle.Fill;
            _palette.IntegralHeight = false;
            _palette.MouseDoubleClick += (s, e) => InsertFrom(_palette, e.Location);
            _palette.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) InsertFrom(_palette, null); };

            paletteLayout.Controls.Add(Label(GameLanguage.Get("Structure")), 0, 0);
            paletteLayout.Controls.Add(_structurePalette, 0, 1);
            paletteLayout.Controls.Add(Label(GameLanguage.Get("Commands")), 0, 2);
            paletteLayout.Controls.Add(_paletteFilter, 0, 3);
            paletteLayout.Controls.Add(_palette, 0, 4);

            palettePage.Controls.Add(paletteLayout);

            var structurePage = new TabPage(GameLanguage.Get("Structure"));
            _structure.Dock = DockStyle.Fill;
            _structure.HideSelection = false;
            _structure.DoubleClick += (s, e) => JumpToStructureNode();

            var structureMenu = new ContextMenuStrip();
            structureMenu.Opening += (s, e) => RefreshStructureMenu();
            _structure.ContextMenuStrip = structureMenu;
            structurePage.Controls.Add(_structure);

            var problemPage = new TabPage(GameLanguage.Get("Problems"));
            _problems.Dock = DockStyle.Fill;
            _problems.View = View.Details;
            _problems.FullRowSelect = true;
            _problems.ShowItemToolTips = true;
            _problems.Columns.Add(GameLanguage.Get("Severity"), 65);
            _problems.Columns.Add(GameLanguage.Get("Source file"), 150);
            _problems.Columns.Add(GameLanguage.Get("Line"), 50);
            _problems.Columns.Add(GameLanguage.Get("Page"), 100);
            _problems.Columns.Add(GameLanguage.Get("Diagnostic category"), 110);
            _problems.Columns.Add(GameLanguage.Get("Execution impact"), 120);
            _problems.Columns.Add(GameLanguage.Get("Message"), 420);
            _problemDetail.Dock = DockStyle.Bottom;
            _problemDetail.Height = 150;
            _problemDetail.Multiline = true;
            _problemDetail.ReadOnly = true;
            _problemDetail.ScrollBars = ScrollBars.Both;
            _problems.SelectedIndexChanged += (s, e) =>
            {
                _problemDetail.Text = _problems.SelectedItems.Count == 0 ? string.Empty
                    : ProblemDetail((ScriptIssue)_problems.SelectedItems[0].Tag);
            };
            _problems.ItemActivate += (s, e) => JumpToProblem();
            problemPage.Controls.Add(_problems);
            problemPage.Controls.Add(_problemDetail);

            _side.Dock = DockStyle.Fill;
            _side.TabPages.Add(palettePage);
            _side.TabPages.Add(structurePage);
            _side.TabPages.Add(problemPage);
            body.Panel2.Controls.Add(_side);

            root.Panel2.Controls.Add(body);

            Controls.Add(root);
            Controls.Add(toolbar);
            Controls.Add(status);

            _idleTimer.Tick += (s, e) => { _idleTimer.Stop(); ApplyHighlight(false); if (_autoCheck) CheckBuffer(false); };

            Shown += (s, e) =>
            {
                root.Panel1MinSize = 140;
                root.SplitterDistance = 220;

                body.Panel2MinSize = 320;
                body.SplitterDistance = Math.Max(body.Width - 340, 200);
            };
        }

        private static ToolStripButton Button(string text, EventHandler handler)
        {
            var button = new ToolStripButton(text);
            button.Click += handler;
            return button;
        }

        private void InsertSnippet(string text)
        {
            _editor.SelectedText = text;
            _editor.Focus();
        }

        // ---------------------------------------------------------------- files

        private void SaveFile()
        {
            if (_path == null) return;

            try { _document.Save(_path, _editor.Text); }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, GameLanguage.Get("Script Editor"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _dirty = false;
            _savedText = _editor.Text;
            UpdateTitle();
            _statusIssues.Text = GameLanguage.Get("Saved");
            CheckBuffer(false);
        }

        private bool ConfirmDiscard()
        {
            if (_dirty && _editor.Text == _savedText) _dirty = false;

            if (!_dirty) return true;

            DialogResult result = MessageBox.Show(this,
                GameLanguage.Get("The script has unsaved changes. Discard them?"),
                GameLanguage.Get("Script Editor"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        private void UpdateTitle()
        {
            string mode = CurrentDocumentKind() == ScriptDocumentKind.Quest ? GameLanguage.Get("Quest definition")
                : CurrentDocumentKind() == ScriptDocumentKind.Text ? GameLanguage.Get("Plain text") : CurrentScriptType().ToString();
            Text = (_path == null ? GameLanguage.Get("Script Editor") : Path.GetFileName(_path)) +
                (_dirty ? " *" : string.Empty) + " [" + mode + "] - " + GameLanguage.Get("Script Editor");
        }

        // ---------------------------------------------------------------- palette

        private void ScheduleIdle()
        {
            _idleTimer.Stop();
            if (_autoCheck || _highlight) _idleTimer.Start();
        }

        private void RefreshPalette()
        {
            int line = _editor.GetLineFromCharIndex(_editor.SelectionStart) + 1;
            ScriptProject project = ParseProject();

            _paletteSource.Clear();
            _structureCandidates.Clear();
            _valueCandidates.Clear();

            if (project.Kind == ScriptDocumentKind.Quest)
            {
                RefreshQuestPalette(project, line);
                return;
            }
            if (project.Kind == ScriptDocumentKind.Text)
            {
                _paletteHint = GameLanguage.Get("Plain text has no script validation.");
                FillPalette();
                return;
            }

            ScriptPageInfo sectionPage = project.SectionPageAt(line);

            if (sectionPage != null)
            {
                // A data section has no blocks, but its file can still gain pages and sections.
                _structureCandidates.AddRange(ScriptStructureCatalog.TopLevel(project, CurrentScriptType()));
                _paletteHint = SectionHint(sectionPage.Section);
                _valueCandidates.AddRange(ValueCandidates(sectionPage, project));
                FillPalette();
                return;
            }

            _paletteHint = GameLanguage.Get("No keyword here");

            ScriptBlockKind kind = project.ContextAt(line, out _);

            _structureCandidates.AddRange(ScriptStructureCatalog.Build(project, line, CurrentScriptType()));

            switch (kind)
            {
                case ScriptBlockKind.Check:
                    _paletteSource.AddRange(ScriptKeywordRegistry.Checks);
                    AddVariables();
                    break;
                case ScriptBlockKind.Act:
                case ScriptBlockKind.ElseAct:
                    _paletteSource.AddRange(ScriptKeywordRegistry.Actions);
                    AddVariables();
                    break;
                case ScriptBlockKind.Say:
                case ScriptBlockKind.ElseSay:
                    AddVariables();
                    break;
            }

            FillPalette();
        }

        private void AddVariables()
        {
            _paletteSource.AddRange(CurrentScriptType() == NPCScriptType.AutoMonster
                ? ScriptKeywordRegistry.MonsterVariables : ScriptKeywordRegistry.Variables);
        }

        private void FillPalette()
        {
            string filter = _paletteFilter.Text.Trim();

            _structurePalette.BeginUpdate();
            _structurePalette.Items.Clear();

            foreach (StructureCandidate candidate in _structureCandidates)
            {
                if (filter.Length > 0 && candidate.IsHeader) continue;

                var entry = new PaletteEntry(candidate);

                if (filter.Length > 0 && entry.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                _structurePalette.Items.Add(entry);
            }

            if (_structurePalette.Items.Count == 0)
            {
                _structurePalette.Items.Add(new PaletteEntry(null, GameLanguage.Get("No structure here")));
            }

            _structurePalette.EndUpdate();

            _palette.BeginUpdate();
            _palette.Items.Clear();

            foreach (StructureCandidate candidate in _valueCandidates)
            {
                var entry = new PaletteEntry(candidate);

                if (filter.Length > 0 && entry.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                _palette.Items.Add(entry);
            }

            foreach (ScriptKeywordEntry keyword in _paletteSource)
            {
                var entry = new PaletteEntry(keyword);

                if (filter.Length > 0 &&
                    entry.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                    keyword.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                _palette.Items.Add(entry);
            }

            if (_palette.Items.Count == 0)
            {
                _palette.Items.Add(new PaletteEntry(null, _paletteHint));
            }

            _palette.EndUpdate();
        }

        private static Label Label(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText,
            };
        }

        private void InsertFrom(ListBox list, Point? location)
        {
            int index = location.HasValue ? list.IndexFromPoint(location.Value) : list.SelectedIndex;

            if (index < 0 || index >= list.Items.Count) return;

            var entry = list.Items[index] as PaletteEntry;

            if (entry == null) return;

            if (entry.Candidate != null) InsertStructure(entry.Candidate);
            else InsertKeyword(entry);
        }

        private void InsertStructure(StructureCandidate candidate)
        {
            if (candidate.IsJump)
            {
                JumpToLine(candidate.JumpLine);
                return;
            }

            if (candidate.Text == null) return;

            // A page or a data section belongs to the file, not to the block the caret happens to sit in.
            if (candidate.TopLevel)
            {
                AppendText(candidate.Text);
                return;
            }

            int line = _editor.GetLineFromCharIndex(_editor.SelectionStart);
            int position = _editor.GetFirstCharIndexFromLine(line) + _editor.Lines[line].Length;

            _editor.SelectionStart = position;
            _editor.SelectionLength = 0;
            _editor.SelectedText = "\r\n" + candidate.Text;
            _editor.Focus();
        }

        private void AppendText(string text)
        {
            _editor.SelectionStart = _editor.TextLength;
            _editor.SelectionLength = 0;
            _editor.SelectedText = (_editor.TextLength > 0 ? "\r\n" : string.Empty) + text;
            _editor.Focus();
        }

        private ToolStripMenuItem StructureMenuItem(string text, string insert)
        {
            return new ToolStripMenuItem(text, null, (s, e) => AppendText(insert));
        }

        private void InsertKeyword(PaletteEntry entry)
        {
            if (entry == null || entry.Keyword == null) return;

            ScriptKeywordEntry keyword = entry.Keyword;
            bool variable = keyword.Kind == ScriptKeywordKind.Variable;

            string usage = variable
                ? "<$" + keyword.Name + ">"
                : keyword.Name + (keyword.Usage.Length > 0 ? " " + keyword.Usage : string.Empty);

            int start = _editor.SelectionStart;
            _editor.SelectedText = usage;
            _editor.SelectionStart = start + (variable ? 0 : keyword.Name.Length + 1);
            _editor.SelectionLength = variable ? 0 : keyword.Usage.Length;
            _editor.Focus();
        }

        // ---------------------------------------------------------------- structure and problems

        private void RefreshStructure()
        {
            ScriptProject project = ParseProject();

            var signature = new StringBuilder(_path ?? string.Empty).Append(project.Kind);

            foreach (ScriptPageInfo page in project.Pages)
            {
                signature.Append(page.Key).Append(':').Append(page.Line);

                foreach (ScriptBlock block in page.Blocks)
                    signature.Append('@').Append(block.StartLine).Append(':').Append(block.Tag);
            }

            if (signature.ToString() == _structureSignature) return;

            _structureSignature = signature.ToString();

            _structure.BeginUpdate();
            _structure.Nodes.Clear();

            foreach (ScriptPageInfo page in project.Pages)
            {
                string suffix = page.IsSection ? "  ·  " + GameLanguage.Get("Section") : string.Empty;
                var node = new TreeNode(page.Key + "    " + page.Line + suffix) { Tag = page.Line };

                if (page.IsSection) node.ForeColor = Color.FromArgb(90, 90, 90);

                _structure.Nodes.Add(node);

                foreach (ScriptBlock block in page.Blocks)
                {
                    node.Nodes.Add(new TreeNode(block.Tag + "    " + block.StartLine) { Tag = block.StartLine });
                }
            }

            _structure.EndUpdate();
        }

        private void JumpToStructureNode()
        {
            if (_structure.SelectedNode?.Tag is int line) JumpToLine(line);
        }

        private void JumpToProblem()
        {
            if (_problems.SelectedItems.Count == 0) return;
            if (!(_problems.SelectedItems[0].Tag is ScriptIssue issue)) return;
            if (string.IsNullOrEmpty(issue.FileName) || _path == null ||
                string.Equals(Path.GetFullPath(issue.FileName), Path.GetFullPath(_path), StringComparison.OrdinalIgnoreCase))
            {
                JumpToLine(issue.Line);
                return;
            }
            try
            {
                // Preserve the root's unsaved buffer and issue selection in its own window.
                var external = new ScriptEditorForm();
                try
                {
                    // Reopen with the mode that produced this issue, not necessarily the root's mode.
                    external.OpenFile(issue.FileName, CurrentDocumentKind(), issue.ExecutionType ?? CurrentScriptType());
                    external.Show(this);
                    external.JumpToLine(issue.Line);
                }
                catch { external.Dispose(); throw; }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, GameLanguage.Get("Script Editor"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void JumpToLine(int line)
        {
            if (line < 1) line = 1;
            if (line > _editor.Lines.Length) line = _editor.Lines.Length;

            int index = _editor.GetFirstCharIndexFromLine(line - 1);

            if (index < 0) return;

            _editor.SelectionStart = index;
            _editor.SelectionLength = _editor.Lines[line - 1].Length;
            _editor.ScrollToCaret();
            _editor.Focus();
        }

        private void JumpToPage(string key)
        {
            ScriptProject project = ParseProject();
            string normalized = ScriptProject.Normalize(key);

            foreach (ScriptPageInfo page in project.Pages)
            {
                if (ScriptProject.Normalize(page.Key) != normalized) continue;

                JumpToLine(page.Line);
                return;
            }

            _statusIssues.Text = "找不到页面 " + key;
        }

        private void CheckBuffer(bool report)
        {
            ScriptProject project = ParseProject();
            List<ScriptIssue> issues;
            try
            {
                issues = project.Check(BuildRules());
            }
            catch (Exception exception)
            {
                issues = new List<ScriptIssue> { new ScriptIssue { FileName = _path, IsError = true,
                    Message = GameLanguage.Get("Engine rejected the script:") + " " + exception.Message } };
            }

            var problemSignature = new StringBuilder();

            foreach (ScriptIssue issue in issues)
                problemSignature.Append(issue.ExecutionType).Append(':').Append(ProblemDetail(issue)).Append('|');

            if (problemSignature.ToString() != _problemsSignature)
            {
                _problemsSignature = problemSignature.ToString();

                _problems.BeginUpdate();
                _problems.Items.Clear();
                _problemDetail.Clear();

                foreach (ScriptIssue issue in issues.OrderBy(x => x.FileName).ThenBy(x => x.Line))
                {
                    var item = new ListViewItem(GameLanguage.Get(issue.IsError ? "Error" : "Warning"));
                    item.SubItems.Add(Path.GetFileName(issue.FileName));
                    item.SubItems.Add(issue.Line > 0 ? issue.Line.ToString() : "-");
                    item.SubItems.Add(issue.PageKey ?? string.Empty);
                    item.SubItems.Add(issue.Diagnostic == null ? GameLanguage.Get("Authoring notice")
                        : GameLanguage.Get(issue.Diagnostic.Category.ToString()));
                    item.SubItems.Add(issue.Diagnostic == null ? string.Empty : GameLanguage.Get(issue.Diagnostic.Effect.ToString()));
                    item.SubItems.Add(issue.Message);
                    item.ForeColor = issue.IsError ? Color.Firebrick : Color.DimGray;
                    item.Tag = issue;
                    item.ToolTipText = ProblemDetail(issue);
                    _problems.Items.Add(item);
                }

                _problems.EndUpdate();
            }

            int errors = issues.Count(x => x.IsError);
            _statusIssues.Text = project.Kind == ScriptDocumentKind.Text
                ? GameLanguage.Get("Plain text has no script validation.")
                : issues.Count == 0 ? GameLanguage.Get("No problems found")
                : string.Format(GameLanguage.Get("{0} error(s), {1} warning(s)"), errors, issues.Count - errors);

            RefreshStructure();

            if (report && issues.Count == 0 && project.Kind != ScriptDocumentKind.Text)
            {
                MessageBox.Show(this, GameLanguage.Get("No problems found"), GameLanguage.Get("Script Editor"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (report) _side.SelectedIndex = 2;
        }

        /// <summary>Spell and map names can only be judged against the loaded database.</summary>
        private static ScriptRules BuildRules()
        {
            var rules = new ScriptRules
            {
                SpellExists = name => Enum.TryParse<Spell>(name, true, out _),
            };

            try
            {
                var maps = NonEmpty(Envir.Main?.MapInfoList, Envir.Edit?.MapInfoList);

                if (maps != null && maps.Count > 0)
                {
                    var names = new HashSet<string>(maps.Select(x => x.FileName), StringComparer.OrdinalIgnoreCase);

                    rules.MapExists = name => names.Contains(name);
                }

                var items = NonEmpty(Envir.Main?.ItemInfoList, Envir.Edit?.ItemInfoList);
                var itemIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (items != null && items.Count > 0)
                {
                    // GetItemInfo resolves the first item with this space-normalized name.
                    foreach (var item in items) itemIndices.TryAdd(item.Name.Replace(" ", ""), item.Index);

                    rules.ItemExists = name => itemIndices.ContainsKey(name.Replace(" ", ""));
                }

                var recipes = NonEmpty(Envir.Main?.RecipeInfoList, Envir.Edit?.RecipeInfoList);
                if (recipes != null && recipes.Count > 0 && itemIndices.Count > 0)
                {
                    var indices = new HashSet<int>(recipes.Where(x => x.Item?.Info != null && x.Ingredients?.Count > 0)
                        .Select(x => x.Item.Info.Index));
                    rules.RecipeExists = name => itemIndices.TryGetValue(name.Replace(" ", ""), out int index) && indices.Contains(index);
                }

                var monsters = NonEmpty(Envir.Main?.MonsterInfoList, Envir.Edit?.MonsterInfoList);
                if (monsters != null && monsters.Count > 0)
                {
                    var names = new HashSet<string>(monsters.Select(x => x.Name.Replace(" ", string.Empty)),
                        StringComparer.OrdinalIgnoreCase);
                    rules.MonsterExists = name => names.Contains(name.Replace(" ", string.Empty));
                }

                var quests = NonEmpty(Envir.Main?.QuestInfoList, Envir.Edit?.QuestInfoList);

                if (quests != null && quests.Count > 0)
                {
                    var numbers = new HashSet<int>(quests.Select(x => x.Index));

                    rules.QuestExists = number => numbers.Contains(number);
                }
            }
            catch (Exception)
            {
            }

            return rules;
        }

        private static string ProblemDetail(ScriptIssue issue)
        {
            string location = issue.FileName + ":" + issue.Line + " " + issue.PageKey;
            ScriptDiagnostic diagnostic = issue.Diagnostic;
            if (diagnostic == null) return location + Environment.NewLine + issue.Message;
            return location + Environment.NewLine + GameLanguage.Get(diagnostic.Category.ToString()) + " / "
                + GameLanguage.Get(diagnostic.Effect.ToString()) + " / " + diagnostic.Phase + " / " + diagnostic.Kind
                + Environment.NewLine + issue.Message + Environment.NewLine + diagnostic.Text
                + Environment.NewLine + diagnostic.SourceContext;
        }

        private void UpdateStatus()
        {
            int index = _editor.GetLineFromCharIndex(_editor.SelectionStart);
            int column = _editor.SelectionStart - _editor.GetFirstCharIndexFromLine(index);

            if (_path != null)
                _statusText.Text = _path + (_document == null ? string.Empty : "  [" + _document.Encoding.WebName + "]")
                    + "   " + (index + 1) + ":" + (column + 1);
        }

        // ---------------------------------------------------------------- highlighting

        /// <summary>
        /// Recolours the buffer. Formatting the document raises TextChanged, so the handler has to be
        /// suppressed here or opening a script would keep marking it dirty and re-running this pass.
        /// </summary>
        private void ApplyHighlight(bool force)
        {
            if (!_highlight)
            {
                if (force) ClearColors();
                return;
            }

            if (_suppress) return;
            if (!force && _highlightRevision == _textRevision) return;

            int selectionStart = _editor.SelectionStart;
            int selectionLength = _editor.SelectionLength;
            var scroll = new ScrollPoint();

            SendMessage(_editor.Handle, EmGetScrollPos, IntPtr.Zero, ref scroll);

            _suppress = true;
            _editor.SuspendLayout();
            SendMessage(_editor.Handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);

            try
            {
                string text = _editor.Text;

                _editor.SelectAll();
                _editor.SelectionColor = _editor.ForeColor;

                ScriptProject project = ParseProject();
                if (project.Kind != ScriptDocumentKind.Text)
                {
                    Colorize(text, project.Kind == ScriptDocumentKind.Quest
                        ? QuestCommentPattern : CommentPattern, CommentColor);
                    int[] starts = BuildLineStarts(text);
                    foreach (ScriptPageInfo page in project.Pages)
                    {
                        int line = page.Line - 1;
                        _editor.Select(starts[line], _editor.Lines[line].Length);
                        _editor.SelectionColor = PageColor;
                    }
                    ColorizeColourText(text);
                    if (project.Kind == ScriptDocumentKind.Executable)
                    {
                        Colorize(text, TagPattern, TagColor);
                        Colorize(text, ScriptLexer.Button, ButtonColor);
                        Colorize(text, ValuePattern, ValueColor);
                        ColorizeKeywords(text);
                    }
                }

                _highlightRevision = _textRevision;
            }
            finally
            {
                _editor.Select(selectionStart, selectionLength);
                _editor.ResumeLayout();
                SendMessage(_editor.Handle, WmSetRedraw, (IntPtr)1, IntPtr.Zero);
                SendMessage(_editor.Handle, EmSetScrollPos, IntPtr.Zero, ref scroll);
                _editor.Invalidate();
                _suppress = false;
            }
        }

        private void ClearColors()
        {
            if (_suppress) return;

            _suppress = true;

            try
            {
                _editor.SelectAll();
                _editor.SelectionColor = _editor.ForeColor;
                _highlightRevision = _textRevision;
            }
            finally
            {
                _suppress = false;
            }
        }

        private void Colorize(string text, Regex pattern, Color color)
        {
            foreach (Match match in pattern.Matches(text))
            {
                _editor.Select(match.Index, match.Length);
                _editor.SelectionColor = color;
            }
        }

        /// <summary>
        /// Text written as {content/ColourName}, which the client removes from the line and draws in that
        /// colour. It consumes the construct even when the name is misspelled, so the caret must see it
        /// as markup either way.
        /// </summary>
        private void ColorizeColourText(string text)
        {
            foreach (Match match in ColourTextPattern.Matches(text))
            {
                _editor.Select(match.Index, match.Length);
                _editor.SelectionColor = ColourTextColor;
            }
        }

        private void ColorizeKeywords(string text)
        {
            int[] lineStarts = BuildLineStarts(text);

            foreach (ScriptBlock block in ParseProject().Blocks)
            {
                if (block.Kind != ScriptBlockKind.Check && block.Kind != ScriptBlockKind.Act && block.Kind != ScriptBlockKind.ElseAct) continue;

                // EndLine is the last line of the block itself, so a one-line block still gets a colour.
                for (int line = block.StartLine; line <= block.EndLine && line - 1 < lineStarts.Length; line++)
                {
                    int start = lineStarts[line - 1];
                    int end = line < lineStarts.Length ? lineStarts[line] : text.Length;
                    Match match = LineKeywordPattern.Match(text.Substring(start, end - start));

                    if (!match.Success) continue;

                    _editor.Select(start + match.Groups[1].Index, match.Groups[1].Length);
                    _editor.SelectionColor = KeywordColor;
                }
            }
        }

        private static int[] BuildLineStarts(string text)
        {
            var starts = new List<int> { 0 };

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') starts.Add(i + 1);
            }

            return starts.ToArray();
        }

        // ---------------------------------------------------------------- navigation

        private void EditorMouseDown(object sender, MouseEventArgs e)
        {
            if (CurrentDocumentKind() != ScriptDocumentKind.Executable ||
                e.Button != MouseButtons.Left || !ModifierKeys.HasFlag(Keys.Control)) return;

            int index = _editor.GetCharIndexFromPosition(e.Location);
            int lineIndex = _editor.GetLineFromCharIndex(index);
            string line = _editor.Lines[lineIndex];
            int lineStart = _editor.GetFirstCharIndexFromLine(lineIndex);

            foreach (Match match in ScriptLexer.Button.Matches(line))
            {
                Group target = match.Groups[1];
                int from = lineStart + target.Index;
                int to = from + target.Length;

                if (index < from || index > to) continue;

                JumpToPage(target.Value);
                return;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!ConfirmDiscard()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _idleTimer.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Legal values for the section the caret sits in. Identical authoring candidates are hidden
        /// without claiming that repeated goods or weighted speech have no runtime effect.
        /// </summary>
        private List<StructureCandidate> ValueCandidates(ScriptPageInfo section, ScriptProject project)
        {
            List<StructureCandidate> source;

            switch (section.Section)
            {
                case ScriptSectionKind.Types:
                case ScriptSectionKind.UsedTypes:
                    source = TypeCandidates();
                    break;
                case ScriptSectionKind.Trade:
                    source = ItemCandidates();
                    break;
                case ScriptSectionKind.Recipe:
                    source = RecipeCandidates();
                    break;
                case ScriptSectionKind.Quests:
                    source = QuestCandidates();
                    break;
                default:
                    return new List<StructureCandidate>();
            }

            HashSet<string> existing = project.SectionValues(section);

            if (existing.Count == 0) return source;

            return source.Where(x => !existing.Contains(x.Text)).ToList();
        }

        private List<StructureCandidate> RecipeCandidates()
        {
            if (_recipeCandidates != null) return _recipeCandidates;

            _recipeCandidates = new List<StructureCandidate>();

            try
            {
                var recipes = NonEmpty(Envir.Main?.RecipeInfoList, Envir.Edit?.RecipeInfoList);

                if (recipes != null)
                {
                    foreach (var recipe in recipes.OrderBy(x => x.Item?.Info?.Name))
                    {
                        if (recipe.Item?.Info == null || recipe.Ingredients == null || recipe.Ingredients.Count == 0) continue;

                        string name = recipe.Item.Info.Name;
                        string ingredients = string.Join("，", recipe.Ingredients
                            .Where(x => x?.Info != null)
                            .Select(x => x.Info.Name + "×" + x.Count));

                        _recipeCandidates.Add(new StructureCandidate(name, ingredients, name));
                    }
                }
            }
            catch (Exception)
            {
            }

            return _recipeCandidates;
        }

        private List<StructureCandidate> TypeCandidates()
        {
            if (_typeCandidates != null) return _typeCandidates;

            _typeCandidates = new List<StructureCandidate>();

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                if (type == ItemType.Nothing) continue;

                string value = ((int)type).ToString(CultureInfo.InvariantCulture);
                _typeCandidates.Add(new StructureCandidate(value, type.ToString(), value));
            }

            return _typeCandidates;
        }

        private List<StructureCandidate> ItemCandidates()
        {
            if (_itemCandidates != null) return _itemCandidates;

            _itemCandidates = new List<StructureCandidate>();

            foreach (string name in ItemNames())
                _itemCandidates.Add(new StructureCandidate(name, string.Empty, name));

            return _itemCandidates;
        }

        /// <summary>
        /// The running world (Main) holds the recipes; the edit world (Edit) never loads them. Take
        /// whichever list actually has data.
        /// </summary>
        private static List<T> NonEmpty<T>(List<T> first, List<T> second)
        {
            if (first != null && first.Count > 0) return first;

            return second;
        }

        private List<string> ItemNames()
        {
            if (_itemNames != null) return _itemNames;

            _itemNames = new List<string>();

            try
            {
                var items = NonEmpty(Envir.Main?.ItemInfoList, Envir.Edit?.ItemInfoList);

                if (items != null) _itemNames.AddRange(items.Select(x => x.Name).Where(x => x.Length > 0));
            }
            catch (Exception)
            {
            }

            _itemNames.Sort(StringComparer.OrdinalIgnoreCase);
            return _itemNames;
        }

        private List<StructureCandidate> QuestCandidates()
        {
            if (_questCandidates != null) return _questCandidates;

            _questCandidates = new List<StructureCandidate>();

            try
            {
                var quests = NonEmpty(Envir.Main?.QuestInfoList, Envir.Edit?.QuestInfoList);

                if (quests != null)
                {
                    foreach (var quest in quests.OrderBy(x => x.Index))
                    {
                        string value = quest.Index.ToString(CultureInfo.InvariantCulture);
                        _questCandidates.Add(new StructureCandidate(value, quest.Name, value));
                    }
                }
            }
            catch (Exception)
            {
            }

            return _questCandidates;
        }

        private static string SectionHint(ScriptSectionKind section)
        {
            switch (section)
            {
                case ScriptSectionKind.Types:
                case ScriptSectionKind.UsedTypes:
                    return GameLanguage.Get("One item type number per line.");
                case ScriptSectionKind.Trade:
                    return GameLanguage.Get("Item name, optionally followed by a count.");
                case ScriptSectionKind.Recipe:
                    return GameLanguage.Get("Item name produced by the recipe.");
                case ScriptSectionKind.Quests:
                    return GameLanguage.Get("Quest number per line: positive offers it here, negative turns it in here.");
                case ScriptSectionKind.Speech:
                    return GameLanguage.Get("Speech weight followed by a space and text.");
                default:
                    return GameLanguage.Get("No keyword here");
            }
        }


        private sealed class PaletteEntry
        {
            public readonly ScriptKeywordEntry Keyword;
            public readonly StructureCandidate Candidate;
            private readonly string _hint;

            public PaletteEntry(ScriptKeywordEntry keyword, string hint = null)
            {
                Keyword = keyword;
                _hint = hint;
            }

            public PaletteEntry(StructureCandidate candidate)
            {
                Candidate = candidate;
            }

            public override string ToString()
            {
                if (Candidate != null)
                {
                    string text = Candidate.Description.Length > 0
                        ? Candidate.Label + "   " + Candidate.Description
                        : Candidate.Label;

                    if (Candidate.IsJump) text += "  ·  " + GameLanguage.Get("exists");

                    return text;
                }

                if (Keyword == null) return _hint ?? GameLanguage.Get("No keyword here");

                return Keyword.Name + "   " + GameLanguage.Get(Keyword.Description);
            }
        }
    }
}
