using System;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using System.Text.RegularExpressions;
using S = ServerPackets;

namespace Server.MirObjects
{
    public partial class NPCScript
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        protected static MessageQueue MessageQueue
        {
            get { return MessageQueue.Instance; }
        }

        public static NPCScript Get(int index)
        {
            return Envir.Scripts[index];
        }

        public static NPCScript GetOrAdd(uint loadedObjectID, string fileName, NPCScriptType type, string loadRoot = null)
        {
            // The same name can exist under NPCs and under Events, so the root is part of the identity.
            string root = loadRoot ?? Settings.NPCPath;

            var script = Envir.Scripts.SingleOrDefault(x => x.Value.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)
                                                             && x.Value.LoadedObjectID == loadedObjectID
                                                             && string.Equals(x.Value.LoadRoot, root, StringComparison.OrdinalIgnoreCase)).Value;

            if (script != null)
            {
                return script;
            }

            return new NPCScript(loadedObjectID, fileName, type, root);
        }

        public readonly int ScriptID;
        public readonly uint LoadedObjectID;
        public readonly NPCScriptType Type;
        public readonly string FileName;

        /// <summary>Directory this file is read from. Events live outside NPCPath, so it is not global.</summary>
        public readonly string LoadRoot;

        /// <summary>
        /// Increases on every load attempt. A queued step remembers the generation it was created in,
        /// so a reload cannot make it continue on content that was never running when it was queued.
        /// </summary>
        public int LoadGeneration { get; private set; }

        public bool IsStaleGeneration(int generation)
        {
            return generation != 0 && generation != LoadGeneration;
        }

        public List<ItemType> Types = new List<ItemType>();
        public List<ItemType> UsedTypes = new List<ItemType>();
        public List<UserItem> Goods = new List<UserItem>();
        public List<RecipeInfo> CraftGoods = new List<RecipeInfo>();

        public List<ScriptPage> NPCSections = new List<ScriptPage>();
        public List<ScriptPage> NPCPages = new List<ScriptPage>();

        private NPCScript(uint loadedObjectID, string fileName, NPCScriptType type, string loadRoot)
        {
            ScriptID = ++Envir.ScriptIndex;

            LoadedObjectID = loadedObjectID;
            FileName = fileName;
            Type = type;
            LoadRoot = loadRoot;

            Load();

            Envir.Scripts.Add(ScriptID, this);
        }

        public void Load()
        {
            try
            {
                LoadInfo();
            }
            catch (Exception ex)
            {
                // One broken script must not take the rest of the NPCs down. The script stays empty and
                // the reason is reported, so the server starts and the author can see what happened.
                ClearInfo();

                ScriptDiagnostics.Report(ScriptDiagnosticKind.LoadFailed,
                    new ScriptParseContext { FileName = FileName + ".txt" }, string.Empty, ex.Message, ex);

                MessageQueue.Enqueue("Script Not Loaded: " + FileName);

                return;
            }

            try
            {
                LoadGoods();
            }
            catch (Exception ex)
            {
                // The pages parsed fine; a broken .msd only costs the shop stock. The previous stock
                // stays untouched because LoadGoods swaps its list only after a complete read.
                ScriptDiagnostics.Report(ScriptDiagnosticKind.LoadFailed,
                    new ScriptParseContext { FileName = FileName + ".txt" }, "Goods", ex.Message, ex);

                MessageQueue.Enqueue("Goods Not Loaded: " + FileName);
            }
        }

    }

    public enum NPCScriptType
    {
        Normal,
        Called,
        AutoPlayer,
        AutoMonster,
        Robot
    }
}
