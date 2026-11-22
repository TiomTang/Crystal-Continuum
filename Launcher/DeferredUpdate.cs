using System.Diagnostics;
namespace Launcher;
public enum ApplyPhase { Waiting, Applying, Verifying, Restarting }
public static class DeferredUpdate
{
    // Only known resource trees and the operator INI can be replaced while
    // this process holds launcher dependencies open. All other paths stay deferred.
    // Launcher translations travel as satellite assemblies (zh-Hans/Launcher.resources.dll),
    // which are loaded assemblies and therefore correctly require the restart.
    public static bool RequiresRestart(IEnumerable<FileInformation> files) => files.Any(file =>
    {
        string name = file.FileName.Replace((char)92, '/');
        return !name.Equals("Launcher.ini", StringComparison.OrdinalIgnoreCase) &&
            !new[] { "Data/", "Map/", "Sound/", "Localization/" }.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    });
    public static bool LegacyPending(string root) => File.Exists(PatchEngine.Target(root, ".launcher-update/pending.json"));
    public static bool Pending(string root) => UpdateTransaction.State(root) == "Prepared";
    // A published build is one framework-dependent file: the launcher assemblies,
    // the WebView2 assemblies and the translations are bundled inside Launcher.exe,
    // so the external helper is that file. The .NET Desktop Runtime it needs is the
    // separately packaged prerequisite of the whole installation. Ordinary
    // Debug/Release builds stay multi-file and keep Launcher.dll next to the apphost.
    // A bundled assembly reports no location; that is exactly the probe. A stale
    // Launcher.dll left behind by an older multi-file install must not change the
    // answer.
#pragma warning disable IL3000 // Assembly.Location is empty only for bundled assemblies.
    static bool IsSingleFile => string.IsNullOrEmpty(typeof(DeferredUpdate).Assembly.Location);
#pragma warning restore IL3000
    static void CopyHelperPayload(string target)
    {
        string source = AppContext.BaseDirectory;
        if (IsSingleFile)
        {
            string executable = Environment.ProcessPath;
            if (string.IsNullOrEmpty(executable) || !File.Exists(executable)) executable = Path.Combine(source, "Launcher.exe");
            File.Copy(executable, Path.Combine(target, "Launcher.exe"));
            return;
        }
        // Framework-dependent helper: the apphost's companion files, the WebView2
        // assemblies, the native loader tree and the compiled translations.
        foreach (string file in Directory.GetFiles(source))
        {
            string name = Path.GetFileName(file);
            if (new[] { "Launcher.exe", "Launcher.dll", "Launcher.deps.json", "Launcher.runtimeconfig.json" }.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                name.StartsWith("Microsoft.Web.WebView2.", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) File.Copy(file, Path.Combine(target, name));
        }
        string runtimes = Path.Combine(source, "runtimes");
        if (Directory.Exists(runtimes)) CopyTree(runtimes, Path.Combine(target, "runtimes"));
        foreach (string directory in Directory.GetDirectories(source))
            if (Directory.GetFiles(directory, "*.resources.dll").Length != 0) CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
    }
    public static void Schedule(string root)
    {
        string helper = Path.Combine(Path.GetTempPath(), "CrystalLauncher", "UpdateHelpers", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(helper);
        CopyHelperPayload(helper);
        HelperCleanup.Mark(helper);
        File.WriteAllText(Path.Combine(root, UpdateTransaction.DirectoryName, "RECOVERY.txt"), "Run external recovery helper: " + Path.Combine(helper, "Launcher.exe") + " --recover-update <installation-directory>. Keep backups until recovery succeeds.");
        var info = new ProcessStartInfo(Path.Combine(helper, "Launcher.exe")) { WorkingDirectory = helper, UseShellExecute = false };
        info.ArgumentList.Add("--apply-transaction"); info.ArgumentList.Add(Path.GetFullPath(root)); info.ArgumentList.Add(Environment.ProcessId.ToString());
        if (Process.Start(info) == null) throw new IOException("Could not start external recovery helper");
        Program.Trace("self-update-helper-scheduled");
    }
    static void CopyTree(string source, string target)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse helper dependency");
        Directory.CreateDirectory(target);
        foreach (string f in Directory.GetFiles(source))
        { if ((File.GetAttributes(f) & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse helper dependency"); File.Copy(f, Path.Combine(target, Path.GetFileName(f))); }
        foreach (string d in Directory.GetDirectories(source)) CopyTree(d, Path.Combine(target, Path.GetFileName(d)));
    }
    public static void Apply(string root, int parentId, bool restart = true, Action<ApplyPhase, int, int, string> report = null)
    {
        if (parentId != 0)
        {
            report?.Invoke(ApplyPhase.Waiting, 0, 0, null);
            try { using var parent = Process.GetProcessById(parentId); if (!parent.WaitForExit(30000)) throw new IOException("Launcher did not exit"); }
            catch (ArgumentException) { }
        }
        using (var lease = new InstallationLease(root))
        {
            InstallationLease.EnsureLegacyClientStopped(root);
            if (LegacyPending(root)) throw new IOException("Legacy .launcher-update/pending.json requires manual recovery; files preserved");
            if (parentId == 0) UpdateTransaction.Recover(root);
            else UpdateTransaction.ApplyConfirmed(root,
                token => { report?.Invoke(ApplyPhase.Verifying, 0, 0, null); return Probe(root, token); },
                (done, total, name) => report?.Invoke(ApplyPhase.Applying, done, total, name));
        }
        HelperCleanup.Schedule();
        if (restart) { report?.Invoke(ApplyPhase.Restarting, 0, 0, null); Restart(root); }
    }
    public static void Restart(string root)
    {
        if (Process.Start(new ProcessStartInfo(Path.Combine(Path.GetFullPath(root), "Launcher.exe")) { WorkingDirectory = Path.GetFullPath(root), UseShellExecute = false }) == null) throw new IOException("Committed update but restart failed");
    }
    internal static bool Probe(string root, string token)
    {
        var info = new ProcessStartInfo(Path.Combine(root, "Launcher.exe")) { WorkingDirectory = root, UseShellExecute = false };
        info.ArgumentList.Add("--confirm-update"); info.ArgumentList.Add(token);
        using var process = Process.Start(info);
        if (process == null) return false;
        if (!process.WaitForExit(30000)) { process.Kill(); process.WaitForExit(); return false; }
        string ack = PatchEngine.Target(root, UpdateTransaction.DirectoryName + "/health-" + token);
        bool ok = process.ExitCode == 0 && File.Exists(ack) && File.ReadAllText(ack) == token;
        if (File.Exists(ack)) File.Delete(ack);
        return ok;
    }
}
