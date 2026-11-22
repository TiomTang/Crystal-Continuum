using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Mir2.Launcher.Runtime;

internal sealed class LaunchResult
{
    public bool Started;
    public bool ElevationCancelled;
    public int ExitCode = -1;
    public string Detail = string.Empty;
}

// The Microsoft runtime installer carries a requireAdministrator manifest. Setup itself
// may run per user (the launcher self-update needs a writable install directory), so the
// gate starts the installer with the runas verb and waits for it, which is where the UAC
// consent prompt comes from. When setup already runs elevated the child inherits that
// token and no second prompt appears.
internal static class ElevationRunner
{
    const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
    const uint SEE_MASK_NOASYNC = 0x00000100;
    const int SW_SHOWNORMAL = 1;
    const int ERROR_CANCELLED = 1223;
    const uint WAIT_TIMEOUT = 0x00000102;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct ShellExecuteInfo
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpVerb;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpParameters;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIconOrMonitor;
        public IntPtr hProcess;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool ShellExecuteEx(ref ShellExecuteInfo info);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr handle);

    public static bool IsElevated()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static LaunchResult Run(string executable, string arguments, string workingDirectory, int timeoutMilliseconds, bool inheritToken)
    {
        GateLog.Write("installer: elevated=" + IsElevated() + (inheritToken ? " inherit-token=true" : string.Empty));
        return IsElevated() || inheritToken
            ? RunInherited(executable, arguments, workingDirectory, timeoutMilliseconds)
            : RunWithConsent(executable, arguments, workingDirectory, timeoutMilliseconds);
    }

    static LaunchResult RunInherited(string executable, string arguments, string workingDirectory, int timeoutMilliseconds)
    {
        var result = new LaunchResult();
        try
        {
            var startInfo = new ProcessStartInfo(executable, arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };
            using Process process = Process.Start(startInfo);
            if (process == null)
            {
                result.Detail = "Process.Start returned no process";
                return result;
            }
            result.Started = true;
            if (!process.WaitForExit(timeoutMilliseconds))
            {
                result.Detail = "installer did not finish within " + (timeoutMilliseconds / 60000) + " minutes";
                return result;
            }
            result.ExitCode = process.ExitCode;
            return result;
        }
        catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException || ex is System.IO.IOException)
        {
            result.Detail = "start failed: " + ex.Message;
            return result;
        }
    }

    static LaunchResult RunWithConsent(string executable, string arguments, string workingDirectory, int timeoutMilliseconds)
    {
        var result = new LaunchResult();
        var info = new ShellExecuteInfo
        {
            cbSize = Marshal.SizeOf(typeof(ShellExecuteInfo)),
            fMask = SEE_MASK_NOCLOSEPROCESS | SEE_MASK_NOASYNC,
            lpVerb = "runas",
            lpFile = executable,
            lpParameters = arguments,
            lpDirectory = workingDirectory,
            nShow = SW_SHOWNORMAL
        };
        if (!ShellExecuteEx(ref info))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == ERROR_CANCELLED)
            {
                result.ElevationCancelled = true;
                result.Detail = "the administrator consent prompt was refused";
            }
            else
            {
                result.Detail = "ShellExecuteEx failed: " + new Win32Exception(error).Message + " (error " + error + ")";
            }
            return result;
        }
        if (info.hProcess == IntPtr.Zero)
        {
            result.Detail = "ShellExecuteEx returned no process handle";
            return result;
        }
        result.Started = true;
        try
        {
            uint wait = WaitForSingleObject(info.hProcess, (uint)timeoutMilliseconds);
            if (wait == WAIT_TIMEOUT)
            {
                result.Detail = "installer did not finish within " + (timeoutMilliseconds / 60000) + " minutes";
                return result;
            }
            if (!GetExitCodeProcess(info.hProcess, out uint exitCode))
            {
                result.Detail = "GetExitCodeProcess failed: " + new Win32Exception(Marshal.GetLastWin32Error()).Message;
                return result;
            }
            result.ExitCode = unchecked((int)exitCode);
            return result;
        }
        finally
        {
            CloseHandle(info.hProcess);
        }
    }
}
