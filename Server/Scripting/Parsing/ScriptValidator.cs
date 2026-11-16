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
            return ValidateCore(text, fileName, envirPath, type, scriptDirectory, false);
        }

        /// <summary>
        /// Check an editable document, including standalone INCLUDE fragments. Runtime roots and CALL
        /// dependencies still use Validate: brace envelopes are not executable script statements.
        /// </summary>
        public static ScriptValidationResult ValidateDocument(string text, string fileName, string envirPath,
            NPCScriptType type = NPCScriptType.Normal, string scriptDirectory = "NPCs")
        {
            return ValidateCore(text, fileName, envirPath, type, scriptDirectory, true);
        }

        private static ScriptValidationResult ValidateCore(string text, string fileName, string envirPath,
            NPCScriptType type, string scriptDirectory, bool document)
        {
            fileName = Path.GetFullPath(fileName);

            // The pages belong to this call: the compiler only needs a list to keep them in, and no
            // script host is created to own them.
            var pages = new List<ScriptPage>();

            var source = ScriptSourceLine.Bind((text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'), fileName);
            // Cache the original envelope before blanking its delimiters: a self-INCLUDE must still
            // read the unsaved buffer with its braces and original source evidence intact.
            using var scope = new ScriptValidationScope(envirPath, fileName, source.Select(line => line.Text).ToArray(), scriptDirectory);
            try
            {
                scope.ConsumeLines(source.Count);
                if (document) PrepareIncludeFragments(source);
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

        private static void PrepareIncludeFragments(List<ScriptSourceLine> source)
        {
            for (int header = 0; header < source.Count;)
            {
                while (header < source.Count && (string.IsNullOrWhiteSpace(source[header].Text) ||
                    ScriptLexer.IsComment(source[header].Text))) header++;
                if (header == source.Count) return;

                if (!ScriptPageKeyCatalog.TryParseHeader(source[header].Text, out string key) ||
                    ScriptSectionCatalog.Find(key) != null) return;

                // Recognize consecutive envelopes from the document start, not braces in later
                // pages of an ordinary script. Only whitespace/comments may precede the opener.
                int next = header + 1;
                while (next < source.Count && (string.IsNullOrWhiteSpace(source[next].Text) ||
                    ScriptLexer.IsComment(source[next].Text))) next++;
                if (next == source.Count || source[next].Text.Trim() != "{") return;

                if (!ScriptIncludeParser.TryFindBody(source, header, out int opener, out int closer))
                {
                    ScriptSourceLine line = source[next];
                    ScriptDiagnostics.Report(ScriptDiagnosticKind.MalformedDirective, new ScriptParseContext
                    {
                        FileName = line.FileName, LineNumber = line.Number, Line = line.Text,
                        Source = line.Source, PageKey = key,
                    }, ScriptDirectiveCatalog.Tag(ScriptDirectiveCatalog.Include), "INCLUDE fragment has no closing brace.");
                    return;
                }

                // Only the outer pair is consumed by INCLUDE. Preserve original evidence and line
                // numbers, and leave any inner or stray brace to the normal statement parser.
                source[opener] = Blank(source[opener]);
                source[closer] = Blank(source[closer]);
                header = closer + 1;
            }
        }

        private static ScriptSourceLine Blank(ScriptSourceLine line)
        {
            return new ScriptSourceLine(string.Empty, line.Number, line.FileName, line.Source, line.Error);
        }
    }
}
