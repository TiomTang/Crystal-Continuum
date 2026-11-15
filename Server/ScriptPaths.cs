using System;
using System.IO;

namespace Server
{
    /// <summary>Pure path rules shared by live settings and isolated script validation.</summary>
    internal static class ScriptPaths
    {
        internal static string NameList(string directory, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            string path = Path.Combine(directory, name);
            if (name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || File.Exists(path)) return path;
            return path + ".txt";
        }
    }
}
