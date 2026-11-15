using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using Timer = Server.MirEnvir.Timer;

namespace Server.Scripting
{
    /// <summary>
    /// What each #IF condition does. Keyed by ScriptCheckKind, registered on the entry in
    /// ScriptCheckCatalog; player-only checks return Pass when there is no player, which is what the
    /// engine used to get by having no monster case for them.
    /// The bodies are split by what they inspect, into the Player, Social and World parts.
    /// </summary>
    public partial class ScriptSegment
    {

    }
}
