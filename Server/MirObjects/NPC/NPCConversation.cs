using System;
using System.Collections.Generic;
using System.Linq;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects
{
    /// <summary>
    /// The NPC panel side of a script call: button authorization, input requests, panel state and
    /// the dialog packet. A detached call never goes through here, so an event or quest callback
    /// cannot touch the window a player has open.
    /// </summary>
    public static class NPCConversation
    {
        /// <summary>True when the page is reachable from a button of the page the player is on.</summary>
        public static bool CanOpenPage(PlayerObject player, uint objectID, string key)
        {
            if (player.NPCObjectID != objectID) return false;
            if (player.NPCPage == null) return false;

            bool found = false;

            foreach (NPCSegment segment in player.NPCPage.SegmentList)
            {
                if (!player.NPCSuccess.TryGetValue(segment, out bool result)) continue;

                if ((result ? segment.Buttons : segment.ElseButtons).Any(s => string.Equals(s, key, StringComparison.OrdinalIgnoreCase)))
                {
                    found = true;
                }
            }

            if (found) return true;

            var npc = NPCObject.Get(objectID);
            string npcName = npc?.Info?.GameName ?? npc?.Name ?? "Unknown";
            MessageQueue.Instance.Enqueue(GameLanguage.Get("Player: {0} was prevented access to NPC key: '{1}' on NPC: {2}", player.Name, key, npcName));

            return false;
        }

        /// <summary>True when the page needs typed input and none is present; the request is sent.</summary>
        public static bool RequestsInput(PlayerObject player, string key)
        {
            if (ScriptPageKeyCatalog.Find(key) != ScriptPageKeyCatalog.Input) return false;
            if (player.NPCData.TryGetValue("NPCInputStr", out object _npcInputStr)) return false;

            player.Enqueue(new S.NPCRequestInput { NPCID = player.NPCObjectID, PageName = key });

            return true;
        }

        /// <summary>Starts a page: the previous speech and button results no longer answer for it.</summary>
        public static void BeginPage(PlayerObject player, NPCPage page)
        {
            player.NPCSpeech = new List<string>();
            player.NPCSuccess.Clear();
            player.NPCDialogClose = false;
            player.NPCPage = page;
        }

        /// <summary>Records which NPC and which script the running segments belong to.</summary>
        public static void Track(PlayerObject player, uint objectID, int scriptId)
        {
            player.NPCObjectID = objectID;
            player.NPCScriptID = scriptId;
        }

        public static void RecordSegment(PlayerObject player, NPCPage page, NPCSegment segment, bool success)
        {
            player.NPCSuccess.Add(segment, success);
            player.NPCPage = page;
        }

        /// <summary>Stops the page and hides the window by sending an empty page.</summary>
        public static void Abort(PlayerObject player)
        {
            player.NPCSpeech.Clear();
            player.NPCSuccess.Clear();
            player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });
        }

        /// <summary>Drops what the page said without telling the client; used when execution threw.</summary>
        public static void ClearOnError(PlayerObject player)
        {
            player.NPCSpeech?.Clear();
            player.NPCSuccess?.Clear();
        }

        /// <summary>Sends the page to the client and runs its special page, if it has one.</summary>
        public static void Finish(NPCScript script, PlayerObject player, NPCPage page)
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

            ScriptSpecialPageEntry special = ScriptSpecialPageCatalog.Find(page.Key);

            if (special?.Executor == null) return;

            special.Executor(script, player, page);
        }

        /// <summary>
        /// Closes the window a player has open after a reload. The reloaded script may have removed
        /// or redefined the page and its buttons, so leaving the window open would let a click that
        /// was drawn from the old page run whatever the new page says.
        /// </summary>
        public static void CloseForReload(PlayerObject player)
        {
            if (player == null || player.NPCPage == null) return;

            player.NPCPage = null;
            player.NPCSuccess.Clear();
            player.NPCDialogClose = false;
            player.NPCSpeech = new List<string>();
            player.Enqueue(new S.NPCResponse { Page = player.NPCSpeech });
        }

        /// <summary>Clears the input answer a page may have consumed.</summary>
        public static void EndCall(PlayerObject player)
        {
            player.NPCData.Remove("NPCInputStr");
        }
    }
}
