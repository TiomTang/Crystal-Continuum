using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Mir2.Launcher.Runtime;

internal sealed class SignatureResult
{
    public bool Ok;
    public string Signer = string.Empty;
    public string Detail = string.Empty;

    public override string ToString()
    {
        return (Ok ? "trusted" : "rejected") + " signer=\"" + Signer + "\"" + (string.IsNullOrEmpty(Detail) ? string.Empty : " detail=" + Detail);
    }
}

// Verifies the downloaded installer before it is executed:
//   * WinVerifyTrust proves the file content still matches a signature whose chain is
//     trusted by this machine's store (a tampered download fails here even when the URL
//     and TLS were fine, because only a valid signature over the exact bytes passes).
//   * The signing certificate subject proves the signer is Microsoft, so a valid
//     signature from an unrelated publisher is still rejected.
// Revocation is not queried: an offline first run must not block on a CRL fetch, and the
// signing trust anchor plus the pinned-Authenticode check is the integrity boundary here.
internal static class AuthenticodeVerifier
{
    const int WTD_UI_NONE = 2;
    const int WTD_REVOKE_NONE = 0;
    const int WTD_CHOICE_FILE = 1;
    const int WTD_STATEACTION_VERIFY = 1;
    const int WTD_STATEACTION_CLOSE = 2;
    const int WTD_DISABLE_MD2_MD4 = 0x00002000;
    const int WTD_CACHE_ONLY_URL_RETRIEVAL = 0x00001000;

    // WINTRUST_ACTION_GENERIC_VERIFY_V2
    static readonly Guid GenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    // WINTRUST_FILE_INFO. Kept blittable (pointer instead of a string) because
    // WinVerifyTrust needs a pointer to this structure inside a pinned block.
    [StructLayout(LayoutKind.Sequential)]
    struct WintrustFileInfo
    {
        public int cbStruct;
        public IntPtr FilePath;
        public IntPtr hFile;
        public IntPtr KnownSubject;
    }

    // Only the fields up to UIContext are used; cbStruct tells WinVerifyTrust how far to
    // read, so this layout also works on builds that appended more fields. Every field is
    // an integer or a pointer, which keeps the class blittable and therefore pinnable.
    [StructLayout(LayoutKind.Sequential)]
    sealed class WintrustData
    {
        public int cbStruct;
        public IntPtr PolicyCallbackData;
        public IntPtr SIPClientData;
        public int UIChoice;
        public int RevocationChecks;
        public int UnionChoice;
        public IntPtr FileInfo;
        public int StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public int ProvFlags;
        public int UIContext;
    }

    [DllImport("wintrust.dll", PreserveSig = true, SetLastError = false)]
    static extern uint WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid actionId, [In, Out] WintrustData data);

    public static SignatureResult Verify(string path, string requiredSubjectFragment)
    {
        var result = new SignatureResult();
        if (!File.Exists(path))
        {
            result.Detail = "file not found: " + path;
            return result;
        }

        // First pass with cached chain data (fast, works offline). Only a failure is
        // retried with network chain retrieval, so a machine without internet still
        // validates a normally signed installer from the local certificate store.
        uint status = VerifyTrust(path, cacheOnly: true);
        if (status != 0)
        {
            GateLog.Write("signature: cache-only pass returned 0x" + status.ToString("X8") + ", retrying with chain download");
            status = VerifyTrust(path, cacheOnly: false);
        }
        if (status != 0)
        {
            result.Detail = "WinVerifyTrust returned 0x" + status.ToString("X8");
            return result;
        }

        try
        {
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
            result.Signer = certificate.Subject;
        }
        catch (Exception ex)
        {
            result.Detail = "no signing certificate could be read: " + ex.Message;
            return result;
        }

        if (!string.IsNullOrEmpty(requiredSubjectFragment) &&
            result.Signer.IndexOf(requiredSubjectFragment, StringComparison.OrdinalIgnoreCase) < 0)
        {
            result.Detail = "signer is not " + requiredSubjectFragment;
            return result;
        }

        result.Ok = true;
        return result;
    }

    static uint VerifyTrust(string path, bool cacheOnly)
    {
        IntPtr pathPointer = Marshal.StringToHGlobalUni(path);
        IntPtr fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WintrustFileInfo)));
        try
        {
            var fileInfo = new WintrustFileInfo
            {
                cbStruct = Marshal.SizeOf(typeof(WintrustFileInfo)),
                FilePath = pathPointer
            };
            Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);
            var data = new WintrustData
            {
                cbStruct = Marshal.SizeOf(typeof(WintrustData)),
                UIChoice = WTD_UI_NONE,
                RevocationChecks = WTD_REVOKE_NONE,
                UnionChoice = WTD_CHOICE_FILE,
                FileInfo = fileInfoPointer,
                StateAction = WTD_STATEACTION_VERIFY,
                ProvFlags = WTD_DISABLE_MD2_MD4 | (cacheOnly ? WTD_CACHE_ONLY_URL_RETRIEVAL : 0)
            };
            uint status = WinVerifyTrust(IntPtr.Zero, GenericVerifyV2, data);
            // State data allocated by VERIFY must be released, whatever the verdict.
            data.StateAction = WTD_STATEACTION_CLOSE;
            WinVerifyTrust(IntPtr.Zero, GenericVerifyV2, data);
            return status;
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfoPointer);
            Marshal.FreeHGlobal(pathPointer);
        }
    }
}
