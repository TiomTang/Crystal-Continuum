using System.Diagnostics;

namespace Launcher;

/// <summary>
/// Exclusive installation lease held by the launcher for its whole lifetime.
/// The client holds the same file as a shared read lease, so the game and an
/// update can never overlap. The file name is a cross-process convention and
/// stays identical to the client's lease.
/// </summary>
internal sealed class InstallationLease : IDisposable
{
    private readonly FileStream stream;

    public InstallationLease(string directory)
    {
        string root = Path.GetFullPath(directory);
        foreach (string path in new[] { root, Path.Combine(root, ".mir2-install.lock") })
        {
            try { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse installation lease path"); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        stream = new FileStream(Path.Combine(root, ".mir2-install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    /// <summary>
    /// A client that predates the shared lease does not hold the lock file, so
    /// the launcher must still refuse to update while it is running.
    /// </summary>
    public static void EnsureLegacyClientStopped(string directory)
    {
        string target = Path.Combine(Path.GetFullPath(directory), "Client.exe");
        foreach (var process in Process.GetProcessesByName("Client"))
        {
            using (process)
            {
                string path;
                try { path = process.MainModule?.FileName; }
                catch (Exception ex) { throw new IOException("Cannot verify running client; close it before updating.", ex); }
                if (string.Equals(path, target, StringComparison.OrdinalIgnoreCase)) throw new IOException("Close the running game before updating.");
            }
        }
    }

    public void Dispose() => stream.Dispose();
}
