using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
    /// What each &lt;$VALUE&gt; replacement returns. The player table and the monster table both register
    /// these from ScriptValueCatalog; a body that answers an empty string means "no value".
    /// The player table and the monster table live in their own part.
    /// </summary>
    public partial class ScriptSegment
    {

    }
}
