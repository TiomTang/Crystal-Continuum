using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.MirObjects
{
    /// <summary>Editor-only facts produced by the real loader, without registering a live NPC.</summary>
    public sealed class ScriptValidationResult
    {
        public IReadOnlyList<ScriptSourceLine> SourceLines { get; internal set; }
        public IReadOnlyList<NPCPage> Pages { get; internal set; }
        public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; internal set; }
        public IReadOnlyList<ScriptValidationResult> CalledScripts { get; internal set; }
        public NPCScriptType Type { get; internal set; }
    }

    public partial class NPCScript
    {
        // No Load, script index allocation, world registration or AutoPlayer cleanup.
        private NPCScript(string fileName, NPCScriptType type)
        {
            FileName = fileName;
            Type = type;
            LoadRoot = null;
        }

        /// <summary>
        /// Parse an unsaved buffer and its dependencies. Paths are explicit, so tools never change the
        /// process directory. Sections and automatic marker activation belong to a live world and are
        /// not applied here; the returned source lets tools check their data against an available DB.
        /// </summary>
        public static ScriptValidationResult Validate(string text, string fileName, string envirPath,
            NPCScriptType type = NPCScriptType.Normal, string scriptDirectory = "NPCs")
        {
            fileName = Path.GetFullPath(fileName);
            var script = new NPCScript(Path.ChangeExtension(fileName, null), type);
            var source = ScriptSourceLine.Bind((text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'), fileName);
            using var scope = new ScriptValidationScope(envirPath, fileName, source.Select(line => line.Text).ToArray(), scriptDirectory);
            try
            {
                scope.ConsumeLines(source.Count);
                foreach (ScriptDirectiveEntry directive in ScriptDirectiveCatalog.Ordered)
                    directive.ApplySource(source);

                ScriptCompilation compilation = script.Compile(source, ScriptCompileMode.Tool);

                // Check unlinked pages too, without changing DeepScan or its runtime counters.
                var parsed = new HashSet<string>(script.NPCPages.Select(page =>
                    NPCPage.NormalizeSectionName(page.Key, out _)), StringComparer.OrdinalIgnoreCase);
                foreach (ScriptSourceLine line in source)
                {
                    if (!ScriptPageKeyCatalog.TryParseHeader(line.Text, out string key) ||
                        ScriptSectionCatalog.Find(key) != null ||
                        !parsed.Add(NPCPage.NormalizeSectionName(key, out _))) continue;
                    List<NPCPage> additional = script.ParsePages(compilation.Lines, key);
                    script.NPCPages.AddRange(additional);
                    foreach (NPCPage page in additional) parsed.Add(NPCPage.NormalizeSectionName(page.Key, out _));
                }
            }
            catch (Exception exception)
            {
                ScriptDiagnostics.Report(ScriptDiagnosticKind.LoadFailed,
                    new ScriptParseContext { FileName = fileName }, string.Empty, exception.Message, exception);
            }
            return new ScriptValidationResult
            {
                SourceLines = source.AsReadOnly(),
                Pages = script.NPCPages.AsReadOnly(),
                Diagnostics = scope.Diagnostics.AsReadOnly(),
                CalledScripts = scope.CalledScripts.AsReadOnly(),
                Type = type,
            };
        }
    }
}
