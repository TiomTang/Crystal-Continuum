using Server.MirObjects;
using System;
using System.IO;

namespace Server.MirForms.Scripts
{
    public enum ScriptDocumentKind
    {
        Executable,
        Quest,
        Text,
    }

    /// <summary>File identity is independent of the language's NPC execution mode.</summary>
    public static class ScriptDocumentContext
    {
        public static bool IsWithin(string path, string directory)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(directory)) return false;
            string relative = Path.GetRelativePath(Path.GetFullPath(directory), Path.GetFullPath(path));
            return !Path.IsPathRooted(relative) && relative != ".." &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
        }

        public static ScriptDocumentKind Detect(string path)
        {
            if (string.IsNullOrEmpty(path)) return ScriptDocumentKind.Executable;
            string root = ScriptProject.IncludeRoot(path);
            if (IsWithin(path, Path.Combine(root, "Quests"))) return ScriptDocumentKind.Quest;
            if (IsWithin(path, Path.Combine(root, "NPCs")) ||
                IsWithin(path, Path.Combine(root, "SystemScripts")) ||
                IsWithin(path, Path.Combine(root, "Events"))) return ScriptDocumentKind.Executable;
            return ScriptDocumentKind.Text;
        }

        public static NPCScriptType DetectExecutionType(string path)
        {
            if (string.IsNullOrEmpty(path)) return NPCScriptType.Normal;
            string root = Path.Combine(ScriptProject.IncludeRoot(path), "NPCs");
            string file = Path.GetFullPath(path);
            if (Matches(file, root, Settings.DefaultNPCFilename)) return NPCScriptType.AutoPlayer;
            if (Matches(file, root, Settings.MonsterNPCFilename)) return NPCScriptType.AutoMonster;
            if (Matches(file, root, Settings.RobotNPCFilename)) return NPCScriptType.Robot;
            return NPCScriptType.Normal;
        }

        private static bool Matches(string file, string root, string name)
        {
            return !string.IsNullOrEmpty(name) && string.Equals(file,
                Path.GetFullPath(Path.Combine(root, name + ".txt")), StringComparison.OrdinalIgnoreCase);
        }
    }
}
