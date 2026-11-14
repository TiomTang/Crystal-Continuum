using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirNetwork;
using Server.Library.MirDatabase;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.MirObjects
{
    /// <summary>
    /// What each #ACT action does. Keyed by ScriptActionKind, registered on the entry in
    /// ScriptActionCatalog; Pass moves on to the next action, Fail keeps going, Abort stops the list.
    /// The bodies are split by what they touch: this file keeps the shared helpers, the parts hold the
    /// player, social and world actions.
    /// </summary>
    public partial class NPCSegment
    {
        private static void ChangeObjectHP(MapObject obj, int amount)
        {
            var player = obj as PlayerObject;

            if (player != null) player.ChangeHP(amount);
            else ((MonsterObject)obj).ChangeHP(amount);
        }
    }
}
