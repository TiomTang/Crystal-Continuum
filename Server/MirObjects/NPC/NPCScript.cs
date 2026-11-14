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

        public static NPCScript GetOrAdd(uint loadedObjectID, string fileName, NPCScriptType type)
        {
            var script = Envir.Scripts.SingleOrDefault(x => x.Value.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) && x.Value.LoadedObjectID == loadedObjectID).Value;

            if (script != null)
            {
                return script;
            }

            return new NPCScript(loadedObjectID, fileName, type);
        }

        public readonly int ScriptID;
        public readonly uint LoadedObjectID;
        public readonly NPCScriptType Type;
        public readonly string FileName;

        public List<ItemType> Types = new List<ItemType>();
        public List<ItemType> UsedTypes = new List<ItemType>();
        public List<UserItem> Goods = new List<UserItem>();
        public List<RecipeInfo> CraftGoods = new List<RecipeInfo>();

        public List<NPCPage> NPCSections = new List<NPCPage>();
        public List<NPCPage> NPCPages = new List<NPCPage>();

        private NPCScript(uint loadedObjectID, string fileName, NPCScriptType type)
        {
            ScriptID = ++Envir.ScriptIndex;

            LoadedObjectID = loadedObjectID;
            FileName = fileName;
            Type = type;

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
                    new ScriptParseContext { FileName = FileName + ".txt" }, string.Empty, ex.Message);

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
                    new ScriptParseContext { FileName = FileName + ".txt" }, "Goods", ex.Message);

                MessageQueue.Enqueue("Goods Not Loaded: " + FileName);
            }
        }

        public float PriceRate(PlayerObject player, bool baseRate = false)
        {
            var callingNPC = Envir.NPCs.SingleOrDefault(x => x.ObjectID == player.NPCObjectID);

            if (callingNPC == null)
            {
                return 1F;
            }

            if (callingNPC.Conq == null || baseRate)
            {
                return callingNPC.Info.Rate / 100F;
            }

            if (player.MyGuild != null && player.MyGuild.Guildindex == callingNPC.Conq.GuildInfo.Owner)
            {
                return callingNPC.Info.Rate / 100F;
            }
            else
            {
                return (((callingNPC.Info.Rate / 100F) * callingNPC.Conq.GuildInfo.NPCRate) + callingNPC.Info.Rate) / 100F;
            }
        }


        public void Call(MonsterObject monster, string key)
        {
            key = key.ToUpper();

            monster.NPCBreak = false;

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (monster.NPCBreak)
                    {
                        monster.NPCBreak = false;
                        break;
                    }

                    ProcessSegment(monster, page, segment);
                }
            }
        }
        public void Call(string key)
        {
            key = key.ToUpper();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (page.BreakFromSegments)
                    {
                        page.BreakFromSegments = false;
                        break;
                    }

                    ProcessSegment(page, segment);
                }
            }
        }
        public void Call(PlayerObject player, uint objectID, string key)
        {
            key = key.ToUpper();

            player.NPCBreak = false;

            if (!player.NPCDelayed)
            {
                if (!ScriptSpecialPageCatalog.IsPage(key, ScriptSpecialPageKind.Main))
                {
                    if (player.NPCObjectID != objectID) return;

                    bool found = false;

                    foreach (NPCSegment segment in player.NPCPage.SegmentList)
                    {
                        if (!player.NPCSuccess.TryGetValue(segment, out bool result)) break; //no result for segment ?

                        if ((result ? segment.Buttons : segment.ElseButtons).Any(s => s.ToUpper() == key))
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        var npc = NPCObject.Get(objectID);
                        string npcName = npc?.Info?.GameName ?? npc?.Name ?? "Unknown";
                        MessageQueue.Enqueue(GameLanguage.Get("Player: {0} was prevented access to NPC key: '{1}' on NPC: {2}", player.Name, key, npcName));
                        return;
                    }
                }
            }
            else
            {
                player.NPCDelayed = false;
            }

            if (ScriptPageKeyCatalog.Find(key) == ScriptPageKeyCatalog.Input && !player.NPCData.TryGetValue("NPCInputStr", out object _npcInputStr))
            {
                //send off packet to request input
                player.Enqueue(new S.NPCRequestInput { NPCID = player.NPCObjectID, PageName = key });
                return;
            }

            try
            {
                for (int i = 0; i < NPCPages.Count; i++)
                {
                    NPCPage page = NPCPages[i];
                    if (!String.Equals(page.Key, key, StringComparison.CurrentCultureIgnoreCase)) continue;

                    player.NPCSpeech = new List<string>();
                    player.NPCSuccess.Clear();
                    player.NPCDialogClose = false;

                    foreach (NPCSegment segment in page.SegmentList)
                    {
                        if (player.NPCBreak)
                        {
                            player.NPCBreak = false;
                            break;
                        }

                        if (player.NPCDialogClose) break;

                        ProcessSegment(player, page, segment, objectID);
                    }

                    Response(player, page);
                }
            }
            finally
            {
                // A page that fails while it runs must not leave the session in the delayed state,
                // which would skip the button check on the next request.
                player.NPCDelayed = false;
                player.NPCBreak = false;
                player.NPCData.Remove("NPCInputStr");
            }
        }

        private void Response(PlayerObject player, NPCPage page)
        {
            // A CLOSE action ends the page without dialogue: the client hides the
            // window when it receives an empty page.
            if (player.NPCDialogClose)
            {
                player.NPCDialogClose = false;
                player.NPCSpeech.Clear();

                player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });

                return;
            }

            player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });

            ProcessSpecial(player, page);
        }

        private void ProcessSegment(PlayerObject player, NPCPage page, NPCSegment segment, uint objectID)
        {
            player.NPCObjectID = objectID;
            player.NPCScriptID = ScriptID;

            bool success;

            try
            {
                success = segment.Check(player);
            }
            catch (Exception ex)
            {
                // A broken action costs one segment, not the dialogue: the page still answers with
                // its failure text and the reason reaches the diagnostics.
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException,
                    page == null ? string.Empty : page.Key, "NPC", ex.Message);

                success = false;
            }

            player.NPCSuccess.Add(segment, success);
            player.NPCPage = page;
        }

        private void ProcessSegment(MonsterObject monster, NPCPage page, NPCSegment segment)
        {
            segment.Check(monster);
        }
        private void ProcessSegment(NPCPage page, NPCSegment segment)
        {
            segment.Check();
        }

        private void ProcessSpecial(PlayerObject player, NPCPage page)
        {
            ScriptSpecialPageEntry special = ScriptSpecialPageCatalog.Find(page.Key);

            if (special?.Executor == null) return;

            special.Executor(this, player, page);
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
