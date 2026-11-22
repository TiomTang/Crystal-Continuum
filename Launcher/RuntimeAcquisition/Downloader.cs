using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;

namespace Mir2.Launcher.Runtime;

internal sealed class DownloadResult
{
    public bool Ok;
    public string Path = string.Empty;
    public long Bytes;
    public string Detail = string.Empty;
}

// Streams the installer to "<target>.partial" and only promotes it to the final name
// after the bytes were accepted, so an interrupted or rejected download can never look
// like a usable installer on a retry. A pinned --sha256 is checked while streaming, which
// catches a truncated or substituted body before the signature check even runs.
internal static class Downloader
{
    const int BufferSize = 128 * 1024;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Every download failure must become a gate exit code, not a crash.")]
    public static DownloadResult Fetch(string url, string targetPath, string expectedSha256, Action<int?, long> report)
    {
        var result = new DownloadResult();
        string partial = targetPath + ".partial";
        try
        {
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            DeleteQuietly(partial);

            report?.Invoke(null, 0);
            if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) CopyLocal(url, partial);
            else DownloadHttps(url, partial, report);

            long bytes = new FileInfo(partial).Length;
            if (bytes == 0) throw new InvalidDataException("the downloaded file is empty");

            if (!string.IsNullOrEmpty(expectedSha256))
            {
                string actual = Hashing.Sha256(partial);
                if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 mismatch: expected " + expectedSha256 + ", got " + actual);
                GateLog.Write("download: SHA-256 matches the pinned digest");
            }

            DeleteQuietly(targetPath);
            File.Move(partial, targetPath);
            result.Ok = true;
            result.Path = targetPath;
            result.Bytes = bytes;
            return result;
        }
        catch (Exception ex)
        {
            DeleteQuietly(partial);
            result.Detail = ex.GetType().Name + ": " + ex.Message;
            return result;
        }
    }

    static void CopyLocal(string url, string partial)
    {
        string source = new Uri(url).LocalPath;
        GateLog.Write("download: copying local file " + source);
        File.Copy(source, partial, true);
    }

    static void DownloadHttps(string url, string partial, Action<int?, long> report)
    {
        // .NET Framework 4.7.2 still negotiates TLS 1.0 by default on some builds; every
        // Microsoft download host requires at least TLS 1.2.
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        GateLog.Write("download: GET " + url);
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mir2RuntimeGate/" + RuntimeDefaults.Version);
        using HttpResponseMessage response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            throw new InvalidDataException("HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase);
        long total = response.Content.Headers.ContentLength ?? -1;
        GateLog.Write("download: HTTP " + (int)response.StatusCode + " length=" + total + " final=" + response.RequestMessage.RequestUri);

        using Stream input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan);
        byte[] buffer = new byte[BufferSize];
        long read = 0;
        int lastPercent = -1;
        int count;
        while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, count);
            read += count;
            if (total > 0)
            {
                int percent = (int)(read * 100 / total);
                if (percent != lastPercent && percent % 2 == 0)
                {
                    lastPercent = percent;
                    report?.Invoke(percent, read);
                }
            }
            else if (read % (4 * 1024 * 1024) < count)
            {
                report?.Invoke(null, read);
            }
        }
        output.Flush();
        GateLog.Write("download: wrote " + read + " bytes");
    }

    static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

internal static class Hashing
{
    public static string Sha256(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.SequentialScan);
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(stream);
        return BitConverter.ToString(digest).Replace("-", string.Empty);
    }
}
