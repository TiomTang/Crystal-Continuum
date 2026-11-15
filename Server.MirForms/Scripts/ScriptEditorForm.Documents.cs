using Server.MirEnvir;
using Server.MirObjects;
using Server.Scripting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Server.MirForms.Scripts
{
    public sealed partial class ScriptEditorForm
    {
        private ScriptTextDocument _document;
        private ScriptDocumentKind? _openedKind;
        private NPCScriptType? _openedExecutionType;
        private readonly ToolStripDropDownButton _templates = new ToolStripDropDownButton();
        private ToolStripButton _checkButton;
        private bool _referenceDataAvailable;

        /// <summary>Configuration pages share creation, error handling and the editor's lifetime.</summary>
        public static void OpenScript(IWin32Window owner, string path, ScriptDocumentKind kind)
        {
            try
            {
                path = Path.GetFullPath(path);
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    string text = kind == ScriptDocumentKind.Quest ? QuestTemplate()
                        : ScriptTemplateCatalog.PageText(ScriptSpecialPageCatalog.Key(ScriptSpecialPageKind.Main));
                    // CreateNew prevents a concurrent creator's existing script from being overwritten.
                    using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                    writer.Write(text);
                }
                using var editor = new ScriptEditorForm();
                editor.OpenFile(path, kind);
                editor.ShowDialog(owner);
            }
            catch (Exception exception)
            {
                MessageBox.Show(owner, exception.Message, GameLanguage.Get("Script Editor"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadFileTree()
        {
            _files.Nodes.Clear();
            _referenced = BuildReferencedSet(out _referenceDataAvailable);
            string root = Path.GetFullPath(Settings.EnvirPath);
            if (!Directory.Exists(root))
            {
                _statusText.Text = GameLanguage.Get("Environment folder was not found.");
                return;
            }
            var node = new TreeNode(new DirectoryInfo(root).Name) { Tag = root };
            _files.Nodes.Add(node);
            FillDirectory(node, root);
            node.Expand();
        }

        private static HashSet<string> BuildReferencedSet(out bool available)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            available = false;
            foreach (var environment in new[] { Envir.Main, Envir.Edit })
            {
                if (environment == null) continue;
                available |= environment.NPCInfoList.Count > 0 || environment.QuestInfoList.Count > 0;
                foreach (var npc in environment.NPCInfoList)
                    AddReference(result, Settings.NPCPath, npc?.FileName);
                foreach (var quest in environment.QuestInfoList)
                    AddReference(result, Settings.QuestPath, quest?.FileName);
            }
            return result;
        }

        private static void AddReference(HashSet<string> references, string directory, string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            try { references.Add(Path.GetFullPath(Path.Combine(directory, name + ".txt"))); }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
        }

        private void FillDirectory(TreeNode node, string directory)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory, "*.txt").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    var child = new TreeNode(Path.GetFileName(file)) { Tag = file, Name = Path.GetFullPath(file) };
                    MarkNode(child);
                    node.Nodes.Add(child);
                }
                foreach (string path in Directory.GetDirectories(directory).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    // Linked directories can point outside Envir or back to an ancestor.
                    if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) continue;
                    var child = new TreeNode(Path.GetFileName(path)) { Tag = path };
                    node.Nodes.Add(child);
                    FillDirectory(child, path);
                }
            }
            catch (IOException exception) { node.ToolTipText = exception.Message; }
            catch (UnauthorizedAccessException exception) { node.ToolTipText = exception.Message; }
        }

        private void MarkNode(TreeNode node)
        {
            string path = (string)node.Tag;
            bool databaseFile = ScriptDocumentContext.IsWithin(path, Settings.NPCPath) ||
                ScriptDocumentContext.IsWithin(path, Settings.QuestPath);
            bool unused = _referenceDataAvailable && databaseFile && !_referenced.Contains(Path.GetFullPath(path));
            node.Text = Path.GetFileName(path) + (unused ? "  (" + GameLanguage.Get("Unreferenced") + ")" : string.Empty);
            node.ForeColor = unused ? Color.Gray : SystemColors.WindowText;
        }

        private void RefreshReferenceMarks()
        {
            var referenced = BuildReferencedSet(out bool available);
            if (referenced.SetEquals(_referenced) && available == _referenceDataAvailable) return;
            _referenced = referenced;
            _referenceDataAvailable = available;
            RelabelNodes(_files.Nodes);
        }

        private void RelabelNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name.Length > 0) MarkNode(node);
                RelabelNodes(node.Nodes);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            RefreshReferenceMarks();
            _itemNames = null;
            _questCandidates = _itemCandidates = _recipeCandidates = _monsterCandidates = null;
            RefreshPalette();
        }

        private void OpenSelectedFile()
        {
            if (!(_files.SelectedNode?.Tag is string path) || !File.Exists(path) || !ConfirmDiscard()) return;
            try { OpenFile(path); }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, GameLanguage.Get("Script Editor"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenFile(string path, ScriptDocumentKind? kind = null, NPCScriptType? executionType = null)
        {
            path = Path.GetFullPath(path);
            ScriptTextDocument document = ScriptTextFile.Read(path);
            _idleTimer.Stop();
            _suppress = true;
            try
            {
                _document = document;
                _path = path;
                _openedKind = kind;
                _openedExecutionType = executionType;
                // A manual override belongs to this buffer, never to the next tree selection.
                _scriptType.SelectedIndex = 0;
                _editor.Text = document.Text;
                _editor.SelectionStart = 0;
                _textRevision++;
                _highlightRevision = -1;
                _dirty = false;
                _savedText = _editor.Text;
                _structureSignature = null;
                _problemsSignature = null;
            }
            finally { _suppress = false; }
            SelectFileNode(_files.Nodes, path);
            RefreshDocumentTools();
            RefreshStructure();
            ApplyHighlight(true);
            RefreshPalette();
            CheckBuffer(false);
            UpdateTitle();
            UpdateStatus();
        }

        private bool SelectFileNode(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode node in nodes)
            {
                if (string.Equals(node.Name, path, StringComparison.OrdinalIgnoreCase))
                {
                    _files.SelectedNode = node;
                    node.EnsureVisible();
                    return true;
                }
                if (SelectFileNode(node.Nodes, path)) return true;
            }
            return false;
        }

        private ScriptDocumentKind CurrentDocumentKind()
        {
            if (_scriptType.SelectedItem is DocumentKindChoice choice) return choice.Kind;
            if (_scriptType.SelectedItem is NPCScriptType) return ScriptDocumentKind.Executable;
            return _openedKind ?? ScriptDocumentContext.Detect(_path);
        }

        private NPCScriptType CurrentScriptType()
        {
            if (_scriptType.SelectedItem is NPCScriptType type) return type;
            return _openedExecutionType ?? ScriptDocumentContext.DetectExecutionType(_path);
        }

        private ScriptProject ParseProject()
        {
            return ScriptProject.Parse(_editor.Text, _path, CurrentScriptType(), CurrentDocumentKind());
        }

        private void ScriptModeChanged(object sender, EventArgs e)
        {
            if (_suppress) return;
            _structureSignature = null;
            _highlightRevision = -1;
            RefreshDocumentTools();
            RefreshPalette();
            ApplyHighlight(true);
            CheckBuffer(false);
            UpdateTitle();
            UpdateStatus();
        }

        private void RefreshDocumentTools()
        {
            ScriptDocumentKind kind = CurrentDocumentKind();
            _checkButton.Enabled = kind != ScriptDocumentKind.Text;
            _templates.Text = GameLanguage.Get("Templates");
            _templates.DropDownItems.Clear();
            if (kind == ScriptDocumentKind.Quest)
                _templates.DropDownItems.Add(new ToolStripMenuItem(GameLanguage.Get("Quest definition template"), null,
                    (s, e) => AppendText(QuestTemplate())));
            else if (kind == ScriptDocumentKind.Executable)
                foreach (var template in ScriptTemplateCatalog.Entries)
                    _templates.DropDownItems.Add(new ToolStripMenuItem(GameLanguage.Get(template.Label), null,
                        (s, e) => AppendText(template.Text)));
            _templates.Enabled = kind != ScriptDocumentKind.Text;
            RefreshStructureMenu();
        }

        private void RefreshStructureMenu()
        {
            var menu = _structure.ContextMenuStrip;
            if (menu == null) return;
            menu.Items.Clear();
            ScriptProject project = ParseProject();
            var rows = CurrentDocumentKind() == ScriptDocumentKind.Quest ? ScriptStructureCatalog.QuestSections(project)
                : CurrentDocumentKind() == ScriptDocumentKind.Text ? new List<StructureCandidate>()
                : new[] { ScriptStructureCatalog.NewPage() }.Concat(ScriptStructureCatalog.Sections()).ToList();
            foreach (var row in rows)
                menu.Items.Add(new ToolStripMenuItem(row.Label, null, (s, e) => InsertStructure(row)));
        }

        private sealed class DocumentKindChoice
        {
            public readonly ScriptDocumentKind Kind;
            private readonly string _label;
            public DocumentKindChoice(ScriptDocumentKind kind, string label) { Kind = kind; _label = label; }
            public override string ToString() { return _label; }
        }
    }
}
