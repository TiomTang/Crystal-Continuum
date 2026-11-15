using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Scripting
{
    /// <summary>
    /// Read-only compilation of a script buffer and the files it pulls in. It returns the pages and
    /// diagnostics a load would produce, but allocates no NPC script, claims no script index and
    /// publishes no world state, so a tool can check unsaved text against the running engine's rules.
    /// </summary>
    public static class ScriptValidator
    {
        /// <summary>
        /// Parse an unsaved buffer and its dependencies. Paths are explicit, so tools never change the
        /// process directory. Sections and automatic marker activation belong to a live world and are
        /// not applied here; the returned source lets tools check their data against an available DB.
        /// </summary>
        public static ScriptValidationResult Validate(string text, string fileName, string envirPath,
            NPCScriptType type = NPCScriptType.Normal, string scriptDirectory = "NPCs")
        {
            fileName = Path.GetFullPath(fileName);

            // The pages belong to this call: the compiler only needs a list to keep them in, and no
            // script host is created to own them.
            var pages = new List<ScriptPage>();

            var source = ScriptSourceLine.Bind((text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'), fileName);
            using var scope = new ScriptValidationScope(envirPath, fileName, source.Select(line => line.Text).ToArray(), scriptDirectory);
            try
            {
                scope.ConsumeLines(source.Count);
                foreach (ScriptDirectiveEntry directive in ScriptDirectiveCatalog.Ordered)
                    directive.ApplySource(source);

                var compiler = new ScriptCompiler(Path.ChangeExtension(fileName, null), type, pages);

                ScriptCompilation compilation = compiler.Compile(source, ScriptCompileMode.Tool);

                // Check unlinked pages too, without changing DeepScan or its runtime counters.
                var parsed = new HashSet<string>(pages.Select(page =>
                    ScriptPage.NormalizeSectionName(page.Key, out _)), StringComparer.OrdinalIgnoreCase);
                foreach (ScriptSourceLine line in source)
                {
                    if (!ScriptPageKeyCatalog.TryParseHeader(line.Text, out string key) ||
                        ScriptSectionCatalog.Find(key) != null ||
                        !parsed.Add(ScriptPage.NormalizeSectionName(key, out _))) continue;
                    List<ScriptPage> additional = compiler.ParsePages(compilation.Lines, key);
                    foreach (ScriptPage page in additional) parsed.Add(ScriptPage.NormalizeSectionName(page.Key, out _));
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
                Pages = pages.AsReadOnly(),
                Diagnostics = scope.Diagnostics.AsReadOnly(),
                CalledScripts = scope.CalledScripts.AsReadOnly(),
                Type = type,
            };
        }
    }
}
