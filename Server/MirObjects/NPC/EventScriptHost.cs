using System;
using System.IO;
using Server.MirEnvir;

namespace Server.MirObjects
{
    /// <summary>
    /// Explicit entry for the pages under <c>Envir/Events</c>. Nothing here decides when an event
    /// happens: a caller names the file and the page, so no reward fires because a file exists on
    /// disk. A script that is not named stays unloaded.
    /// </summary>
    public static class EventScriptHost
    {
        /// <summary>Loads an event script from <see cref="Settings.EventsPath"/> or returns the loaded one.</summary>
        public static NPCScript Get(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            return NPCScript.GetOrAdd(0, fileName.Trim(), NPCScriptType.Normal, Settings.EventsPath);
        }

        /// <summary>True when the file was found and parsed into at least one page.</summary>
        public static bool IsLoaded(string fileName)
        {
            NPCScript script = Get(fileName);

            return script != null && script.NPCPages.Count > 0;
        }

        /// <summary>
        /// Runs one event page for a player without an NPC conversation. Returns false and sets
        /// <paramref name="error"/> when the file is missing, the page is absent or the page stopped
        /// on an execution error. The caller owns any reward ledger: this method does not dedupe.
        /// </summary>
        public static bool TryRun(string fileName, string page, PlayerObject player, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "event script name is empty";
                return false;
            }

            if (player == null)
            {
                error = "no player to run the event page for";
                return false;
            }

            string file = fileName.Trim();
            NPCScript script = Get(file);

            if (script == null || script.NPCPages.Count == 0)
            {
                error = "event script not loaded: " + file;
                return false;
            }

            string key = page ?? ScriptSpecialPageCatalog.Key(ScriptSpecialPageKind.Main);

            if (!script.HasPage(key))
            {
                error = "page not found: " + key + " in " + file;
                return false;
            }

            if (!script.RunDetached(player, key))
            {
                error = "event page stopped: " + key + " in " + file;
                return false;
            }

            return true;
        }

        /// <summary>Full path of an event script, for diagnostics and tools.</summary>
        public static string PathOf(string fileName)
        {
            return string.IsNullOrWhiteSpace(fileName)
                ? Settings.EventsPath
                : System.IO.Path.Combine(Settings.EventsPath, fileName.Trim() + ".txt");
        }

        /// <summary>True when the file exists on disk, whether or not it is loaded.</summary>
        public static bool Exists(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName) && File.Exists(PathOf(fileName));
        }

        internal static Envir Envir => Envir.Main;
    }
}
