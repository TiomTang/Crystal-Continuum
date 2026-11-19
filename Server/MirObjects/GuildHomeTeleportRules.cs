namespace Server.MirObjects
{
    public enum GuildHomeTeleportResult
    {
        Success,
        NotInGuild,
        NotOwner,
        WarInProgress,
        PalaceUnavailable
    }

    // Guild home teleport goes to the palace of the guild's own conquest.
    // Every failure must be reported to the caller so the item is never consumed.
    public static class GuildHomeTeleportRules
    {
        public static GuildHomeTeleportResult Evaluate(GuildObject guild)
        {
            if (guild == null) return GuildHomeTeleportResult.NotInGuild;

            ConquestObject conquest = guild.Conquest;

            if (conquest == null || conquest.Guild != guild) return GuildHomeTeleportResult.NotOwner;
            if (conquest.WarIsOn) return GuildHomeTeleportResult.WarInProgress;
            if (conquest.PalaceMap == null) return GuildHomeTeleportResult.PalaceUnavailable;

            return GuildHomeTeleportResult.Success;
        }
    }
}
