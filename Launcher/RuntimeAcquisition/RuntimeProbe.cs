using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Mir2.Launcher.Runtime;

// The launcher targets net8.0-windows, so it needs the 64-bit Microsoft.WindowsDesktop.App
// shared framework. A plain Microsoft.NETCore.App 8.0 runtime would start the launcher and
// then fail on System.Windows.Forms, so it must not be reported as present. Two sources are
// consulted because either one alone can be stale: the installer's own registration and the
// shared framework directory that the host actually resolves against.
internal static class RuntimeProbe
{
    const string SharedFrameworkKey = @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App";
    const string FrameworkName = "Microsoft.WindowsDesktop.App";
    const string RequiredPayload = "System.Windows.Forms.dll";

    public static IReadOnlyList<string> InstalledVersions(string dotnetRoot, bool forceMissing)
    {
        var versions = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (forceMissing)
        {
            GateLog.Write("probe: treated as missing by test hook");
            return new List<string>();
        }
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            CollectFromDirectory(Path.Combine(Path.GetFullPath(dotnetRoot), "shared", FrameworkName), versions, false);
            return new List<string>(versions);
        }
        CollectFromRegistry(versions);
        CollectFromDirectory(Path.Combine(ProgramFiles(), "dotnet", "shared", FrameworkName), versions, true);
        return new List<string>(versions);
    }

    static string ProgramFiles()
    {
        string path = Environment.GetEnvironmentVariable("ProgramFiles");
        return string.IsNullOrEmpty(path) ? @"C:\Program Files" : path;
    }

    static void CollectFromRegistry(ISet<string> versions)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey key = baseKey.OpenSubKey(SharedFrameworkKey);
            if (key == null) return;
            // Each installed 8.0.x build registers a value whose name is the version.
            foreach (string name in key.GetValueNames())
                if (IsSupported(name)) versions.Add(name);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is IOException)
        {
            GateLog.Write("probe: registry read failed: " + ex.GetType().Name);
        }
    }

    static void CollectFromDirectory(string directory, ISet<string> versions, bool requirePayload)
    {
        try
        {
            if (!Directory.Exists(directory)) return;
            foreach (string path in Directory.GetDirectories(directory))
            {
                string name = Path.GetFileName(path);
                if (!IsSupported(name)) continue;
                // A directory left behind by an uninstalled or half copied build has no
                // framework assemblies; the machine-wide probe must not trust it.
                if (requirePayload && !File.Exists(Path.Combine(path, RequiredPayload))) continue;
                versions.Add(name);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
        {
            GateLog.Write("probe: directory read failed: " + ex.GetType().Name);
        }
    }

    static bool IsSupported(string name)
    {
        if (!Version.TryParse(name, out Version version)) return false;
        if (version.Major != 8) return false;
        return version >= new Version(8, 0, 0);
    }
}
