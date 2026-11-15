using Server.Scripting;
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
        public void Call(MonsterObject monster, string key)
        {
            using var interaction = ScriptInteraction.Enter(null, new ScriptInteraction(false), FileName + ":" + key);
            using var evidence = ScriptDiagnostics.BeginExecution(ScriptInteraction.Current.CreateEvidence(script: FileName, page: key));
            var call = new ScriptCallContext();

            for (int i = 0; i < NPCPages.Count; i++)
            {
                ScriptPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (ScriptSegment segment in page.SegmentList)
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
                ScriptPage page = NPCPages[i];
                if (!String.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (ScriptSegment segment in page.SegmentList)
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
            bool isMainPage = ScriptSpecialPageCatalog.IsPage(key, ScriptSpecialPageKind.Main);

            try
            {
                // Main starts a new conversation and does not use the previous page's buttons.
                // Only button calls must reject a page kept from before a reload.
                if (!internalCall && !isMainPage && player.ScriptPage != null && !NPCPages.Contains(player.ScriptPage))
                {
                    NPCConversation.CloseForReload(player);
                    return;
                }

                if (!internalCall && !isMainPage &&
                    !NPCConversation.CanOpenPage(player, objectID, key))
                {
                    return;
                }

                if (NPCConversation.RequestsInput(player, key)) return;

                bool foundPage = false;
                for (int i = 0; i < NPCPages.Count; i++)
                {
                    ScriptPage page = NPCPages[i];
                    if (!String.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                    foundPage = true;

                    NPCConversation.BeginPage(player, page);

                    foreach (ScriptSegment segment in page.SegmentList)
                    {
                        if (call.BreakRequested || call.ExecutionAborted || interaction.Failed) break;

                        if (player.NPCDialogClose) break;

                        ProcessSegment(player, page, segment, objectID, call);
                    }

                    if (call.ExecutionAborted || interaction.Failed)
                    {
                        NPCConversation.Abort(player);
                        break;
                    }
                    NPCConversation.Finish(this, player, page);
                }
                if (!foundPage && (Type == NPCScriptType.Normal || Type == NPCScriptType.Called))
                    ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.MissingPage, key, "NPC",
                        "Requested page is unavailable; the call chain was stopped.", FileName + ".txt");
            }
            catch (Exception ex)
            {
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException, key, "NPC",
                    "NPC page execution failed.", FileName + ".txt", ex.GetType().FullName, ex);
                NPCConversation.ClearOnError(player);
            }
            finally
            {
                interaction.LastCompletedAction = call.LastCompletedAction;
                interaction.HasSideEffects |= call.HasSideEffects;
                player.NPCData.Remove("NPCInputStr");
            }
        }

        private void ProcessSegment(PlayerObject player, ScriptPage page, ScriptSegment segment, uint objectID, ScriptCallContext call)
        {
            NPCConversation.Track(player, objectID, ScriptID);

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

            NPCConversation.RecordSegment(player, page, segment, success);
        }


        /// <summary>True when the file defines this page, whether or not it is reachable by a button.</summary>
        public bool HasPage(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            foreach (ScriptPage page in NPCPages)
            {
                if (string.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Runs one page for a player with no NPC conversation: no button authorization, no panel
        /// state and no dialog packet. An event or a quest callback uses this, and the player's open
        /// NPC window stays exactly as it was. Says are collected and discarded, because nothing here
        /// may deliver a dialog.
        /// </summary>
        public bool RunDetached(PlayerObject player, string key)
        {
            if (player == null || string.IsNullOrEmpty(key)) return false;

            key = key.ToUpperInvariant();

            using var scope = ScriptInteraction.Enter(player, new ScriptInteraction(false), FileName + ":" + key);
            using var boundary = ScriptDiagnostics.BeginExecution(ScriptInteraction.Current.CreateEvidence(
                ScriptDiagnostics.CaptureExecution(), FileName + ".txt", key));
            ScriptInteraction interaction = ScriptInteraction.Current;

            var call = new ScriptCallContext();
            List<string> speech = player.NPCSpeech;
            bool found = false;

            // SAY appends to the player's dialog buffer. Give the page an empty one and hand the
            // original back afterwards, so a detached run cannot leak text into the next dialog.
            player.NPCSpeech = new List<string>();

            try
            {
                foreach (ScriptPage page in NPCPages)
                {
                    if (!string.Equals(page.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                    found = true;

                    foreach (ScriptSegment segment in page.SegmentList)
                    {
                        if (call.BreakRequested || call.ExecutionAborted || interaction.Failed) break;

                        try
                        {
                            segment.Check(player, call);
                        }
                        catch (Exception ex)
                        {
                            // Same rule as a page call: an execution error stops the chain and never
                            // authorizes the ELSE buttons.
                            ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.RuntimeException, key, "NPC",
                                "Detached page execution failed.", FileName + ".txt", ex.GetType().FullName, ex);
                            call.ExecutionAborted = true;
                            break;
                        }
                    }
                }
            }
            finally
            {
                player.NPCSpeech = speech;
                NPCConversation.EndCall(player);
                interaction.LastCompletedAction = call.LastCompletedAction;
                interaction.HasSideEffects |= call.HasSideEffects;
            }

            if (!found)
            {
                ScriptDiagnostics.ReportRuntime(ScriptDiagnosticKind.MissingPage, key, "NPC",
                    "Requested page is unavailable; the detached call was stopped.", FileName + ".txt");
            }

            return found && !call.ExecutionAborted && !interaction.Failed;
        }
    }
}
