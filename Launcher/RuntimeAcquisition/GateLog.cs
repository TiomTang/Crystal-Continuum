using System;
using System.IO;
using System.Text;

namespace Mir2.Launcher.Runtime;

// Setup starts this gate with a hidden console, so every decision has to survive in a
// file that a support engineer can read afterwards. The log appends: a retried run
// keeps the earlier attempt next to the new one instead of erasing the evidence.
internal static class GateLog
{
    const long MaxBytes = 1024 * 1024;

    static StreamWriter writer;

    public static string WorkDirectory { get; private set; } = DefaultWorkDirectory();

    public static string LogPath { get; private set; }

    public static bool Quiet { get; private set; }

    public static void Open(string logPath, string workDirectory, bool quiet)
    {
        Quiet = quiet;
        WorkDirectory = Path.GetFullPath(string.IsNullOrEmpty(workDirectory) ? DefaultWorkDirectory() : workDirectory);
        Directory.CreateDirectory(WorkDirectory);
        LogPath = Path.GetFullPath(string.IsNullOrEmpty(logPath) ? Path.Combine(WorkDirectory, "logs", "runtime-gate.log") : logPath);
        string logDirectory = Path.GetDirectoryName(LogPath);
        if (!string.IsNullOrEmpty(logDirectory)) Directory.CreateDirectory(logDirectory);
        Rotate(LogPath);
        writer = new StreamWriter(new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        Write("gate " + RuntimeDefaults.Version + " start on " + Environment.OSVersion.VersionString + " (" + (Environment.Is64BitProcess ? "x64" : "not x64") + ")");
    }

    public static string DefaultWorkDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mir2Setup");
    }

    public static void Write(string message)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message;
        if (writer != null)
        {
            try { writer.WriteLine(line); }
            catch (IOException) { }
        }
        if (Quiet) return;
        try { Console.WriteLine(line); }
        catch (IOException) { }
    }

    public static void Close()
    {
        if (writer == null) return;
        try { writer.Dispose(); }
        catch (IOException) { }
        writer = null;
    }

    // The gate is re-run by every setup attempt, so cap the file instead of letting it
    // grow without bound on a machine where setup is retried repeatedly.
    static void Rotate(string path)
    {
        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > MaxBytes) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
