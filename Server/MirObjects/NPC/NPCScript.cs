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
            using var interaction = ScriptInteraction.Enter(null, new ScriptInteraction(false), FileName + ":" + key);
            using var evidence = ScriptDiagnostics.BeginExecution(ScriptInteraction.Current.CreateEvidence(script: FileName, page: key));
            var call = new ScriptCallContext();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (call.BreakRequested || call.ExecutionAborted || ScriptInteraction.Current.Failed) break;

                    segment.Check(monster, call);
                }
            }
        }
        public void Call(string key)
        {
            using var interaction = ScriptInteraction.Enter(null, new ScriptInteraction(false), FileName + ":" + key);
            using var evidence = ScriptDiagnostics.BeginExecution(ScriptInteraction.Current.CreateEvidence(script: FileName, page: key));
            var call = new ScriptCallContext();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                NPCPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (NPCSegment segment in page.SegmentList)
                {
                    if (call.BreakRequested || call.ExecutionAborted || ScriptInteraction.Current.Failed) break;

                    segment.Check(call);
                }
            }
        }
        public void Call(PlayerObject player, uint objectID, string key)
        {
            Call(player, objectID, key, false);
        }

        internal void CallInternal(PlayerObject player, uint objectID, string key)
        {
            Call(player, objectID, key, true);
        }

        private void Call(PlayerObject player, uint objectID, string key, bool internalCall)
        {
            var interaction = ScriptInteraction.Current ?? new ScriptInteraction(!internalCall);
            using var scope = ScriptInteraction.Enter(player, interaction, FileName + ":" + key);
            using var boundary = ScriptDiagnostics.BeginExecution(interaction.CreateEvidence(
                ScriptDiagnostics.CaptureExecution(), FileName + ".txt", key));
            if (interaction.Failed) return;
            var call = new ScriptCallContext();
            call.LastCompletedAction = interaction.LastCompletedAction;
            call.HasSideEffects = interaction.HasSideEffects;
            key = key.ToUpperInvariant();

            try
            {
                if (!internalCall && !ScriptSpecialPageCatalog.IsPage(key, ScriptSpecialPageKind.Main))
                {
                    if (player.NPCObjectID != objectID) return;
                    if (player.NPCPage == null) return;

                    bool found = false;

                    foreach (NPCSegment segment in player.NPCPage.SegmentList)
                    {
                        if (!player.NPCSuccess.TryGetValue(segment, out bool result)) continue;

                        if ((result ? segment.Buttons : segment.ElseButtons).Any(s => string.Equals(s, key, StringComparison.OrdinalIgnoreCase)))
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
                if (ScriptPageKeyCatalog.Find(key) == ScriptPageKeyCatalog.Input && !player.NPCData.TryGetValue("NPCInputStr", out object _npcInputStr))
                {
                    //send off packet to request input
                    player.Enqueue(new S.NPCRequestInput { NPCID = player.NPCObjectID, PageName = key });
                    return;
                }

                bool foundPage = false;
                for (int i = 0; i < NPCPages.Count; i++)
                {
                    NPCPage page = NPCPages[i];
                    if (!String.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                    foundPage = true;

                    player.NPCSpeech = new List<string>();
                    player.NPCSuccess.Clear();
                    player.NPCDialogClose = false;
                    player.NPCPage = page;

                    foreach (NPCSegment segment in page.SegmentList)
                    {
                        if (call.BreakRequested || call.ExecutionAborted || interaction.Failed) break;

                        if (player.NPCDialogClose) break;

                        ProcessSegment(player, page, segment, objectID, call);
                    }

                    if (call.ExecutionAborted || interaction.Failed)
                    {
                        player.NPCSpeech.Clear();
                        player.NPCSuccess.Clear();
                        player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });
                        break;
                    }
                    Response(player, page);
                }
                if (!foundPage && (Type == NPCScriptType.Normal || Type == NPCScriptType.Called))
                    ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.MissingPage, key, "NPC",
                        "Requested page is unavailable; the call chain was stopped.", FileName + ".txt");
            }
            catch (Exception ex)
            {
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException, key, "NPC",
                    "NPC page execution failed.", FileName + ".txt", ex.GetType().FullName, ex);
                player.NPCSpeech?.Clear();
                player.NPCSuccess?.Clear();
            }
            finally
            {
                interaction.LastCompletedAction = call.LastCompletedAction;
                interaction.HasSideEffects |= call.HasSideEffects;
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

        private void ProcessSegment(PlayerObject player, NPCPage page, NPCSegment segment, uint objectID, ScriptCallContext call)
        {
            player.NPCObjectID = objectID;
            player.NPCScriptID = ScriptID;

            bool success;

            try
            {
                success = segment.Check(player, call);
                if (call.ConditionAborted || call.ExecutionAborted || ScriptInteraction.Current?.Failed == true) return;
            }
            catch (Exception ex)
            {
                // An execution error is not a failed condition and must not authorize ELSE buttons.
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException,
                    page == null ? string.Empty : page.Key, "NPC", ex.Message,
                    FileName + ".txt", ex.GetType().FullName, ex);
                call.ExecutionAborted = true;

                return;
            }

            player.NPCSuccess.Add(segment, success);
            player.NPCPage = page;
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
