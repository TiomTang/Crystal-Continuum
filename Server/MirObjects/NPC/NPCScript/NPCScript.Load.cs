using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        /// <summary>Directory this script is read from; a called script is looked up in the same one.</summary>
        public string Root => LoadRoot ?? Settings.NPCPath;

        public void LoadInfo()
        {
            ClearInfo();

            LoadGeneration++;

            if (!Directory.Exists(Root)) return;

            string fileName = Path.Combine(Root, FileName + ".txt");

            if (!File.Exists(fileName))
            {
                ScriptDiagnostics.Report(ScriptDiagnosticKind.MissingScript,
                    new ScriptParseContext { FileName = FileName + ".txt" }, "NPC", "Script file was not found; no pages were loaded.");
                MessageQueue.Enqueue(GameLanguage.Get("Script Not Found: {0}", FileName));
                return;
            }

            ScriptParseScope.Set(FileName + ".txt", Root);

            try
            {
                bool legacy;

                var sourceLines = ScriptSourceLine.Bind(ScriptTextFile.ReadAllLines(fileName, out legacy), FileName + ".txt");

                if (legacy)
                    ScriptDiagnostics.Report(ScriptDiagnosticKind.LegacyEncoding,
                        new ScriptParseContext { FileName = FileName + ".txt" }, string.Empty,
                        "file is not UTF-8, read as GBK");

                foreach (ScriptDirectiveEntry directive in ScriptDirectiveCatalog.Ordered)
                    directive.ApplySource(sourceLines);

                var compiler = new ScriptCompiler(FileName, Type, NPCPages);

                ScriptCompilation compilation = compiler.Compile(sourceLines, ScriptCompileMode.Live);

                // The three steps below may touch the world, so they run only here and never in a tool.
                BindCalls();
                ParseSections(compilation);

                if (ScriptDiagnostics.DeepScan && (Type == NPCScriptType.Normal || Type == NPCScriptType.Called))
                    compiler.ReportUnreachablePages(compilation.Lines);

                ActivateAutoMarkers(compilation);
            }
            catch (Exception ex)
            {
                // One unreadable or unexpected script must not stop the NPC load. The script stays empty
                // and the reason is reported with the file name, so the author can find it.
                ClearInfo();

                ScriptDiagnostics.Report(ScriptDiagnosticKind.LoadFailed,
                    new ScriptParseContext { FileName = FileName + ".txt" }, string.Empty, ex.Message, ex);

                MessageQueue.Enqueue("Script Not Loaded: " + FileName);
            }
            finally
            {
                ScriptParseScope.Set(null);
            }
        }

        public void ClearInfo()
        {
            Goods = new List<UserItem>();
            Types = new List<ItemType>();
            UsedTypes = new List<ItemType>();
            NPCPages = new List<ScriptPage>();
            CraftGoods = new List<RecipeInfo>();

            if (Type == NPCScriptType.AutoPlayer)
            {
                Envir.CustomCommands.Clear();
            }
        }

        public void LoadGoods()
        {
            var loadedNPC = NPCObject.Get(LoadedObjectID);

            if (loadedNPC == null) return;

            string path = Path.Combine(Settings.GoodsPath, loadedNPC.Info.Index.ToString() + ".msd");

            var loaded = new List<UserItem>();

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        int version = reader.ReadInt32();
                        int count = version;
                        int customversion = Envir.LoadCustomVersion;
                        if (version == 9999)//the only real way to tell if the file was made before or after version code got added: assuming nobody had a config option to save more then 10000 sold items
                        {
                            version = reader.ReadInt32();
                            customversion = reader.ReadInt32();
                            count = reader.ReadInt32();
                        }
                        else
                            version = Envir.LoadVersion;

                        if (count < 0 || count > 100000)
                            throw new ArgumentException("Invalid goods count: " + count);

                        for (int k = 0; k < count; k++)
                        {
                            UserItem item = new UserItem(reader, version, customversion);
                            if (Envir.BindItem(item))
                                loaded.Add(item);
                        }
                    }
                }
            }

            // Swap only after the file has been read completely, so a truncated .msd cannot leave the
            // NPC with half its stock or clear what the previous load had.
            loadedNPC.UsedGoods.Clear();
            loadedNPC.UsedGoods.AddRange(loaded);
        }

        /// <summary>
        /// Registers every script a page calls and replaces the written path with its runtime id. The
        /// parser stores the path only, so compiling a buffer in the editor registers nothing.
        /// </summary>
        private void BindCalls()
        {
            foreach (ScriptPage page in NPCPages)
            {
                if (page == null) continue;

                foreach (ScriptSegment segment in page.SegmentList)
                {
                    if (segment == null) continue;

                    foreach (ScriptActions action in segment.ActList)
                    {
                        if (action.Type != ScriptActionKind.Call || action.Params.Count == 0) continue;

                        string path = action.Params[0];

                        if (int.TryParse(path, out _)) continue;

                        NPCScript called = GetOrAdd(0, path, NPCScriptType.Called, Root);

                        page.ScriptCalls.Add(called.ScriptID);

                        action.Params[0] = called.ScriptID.ToString();
                    }
                }
            }
        }

        /// <summary>Data sections are world data, so only a live load reads them.</summary>
        private void ParseSections(ScriptCompilation compilation)
        {
            IList<string> lines = compilation.Lines;

            foreach (ScriptSectionEntry section in ScriptSectionCatalog.Ordered)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (!section.MatchesHeader(lines[i])) continue;

                    section.Parse(this, compilation, i);
                }
            }
        }

        /// <summary>Publishes what the live plan recorded while compiling.</summary>
        private static void ActivateAutoMarkers(ScriptCompilation compilation)
        {
            foreach (ScriptAutoMarkerPlan plan in compilation.AutoPlans)
                plan.Entry.Publish(plan.Payload);

            compilation.AutoPlans.Clear();
        }
    }
}
