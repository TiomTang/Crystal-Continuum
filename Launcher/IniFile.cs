using System.Globalization;
using System.Text;

namespace Launcher;

/// <summary>
/// Read/write INI document used by the launcher for the operator file and the
/// client configuration. Unlike the shared <c>InIReader</c> it never invents
/// defaults while reading, it keeps the original encoding and line endings, and
/// it reports IO failures instead of swallowing them, so callers can tell a
/// missing file apart from one that cannot be read or written.
/// </summary>
internal sealed class IniFile
{
    private readonly List<string> lines = new();
    private string newline = "\r\n";
    private bool trailingNewline;
    private Encoding encoding = new UTF8Encoding(false);

    private IniFile(string path) => Path = path;

    public string Path { get; }
    public bool Exists { get; private set; }

    public static IniFile Read(string path)
    {
        var file = new IniFile(System.IO.Path.GetFullPath(path));
        if (!File.Exists(file.Path)) return file;

        byte[] bytes = File.ReadAllBytes(file.Path);
        file.Exists = true;
        file.encoding = DetectEncoding(bytes, out int offset);
        string text = file.encoding.GetString(bytes, offset, bytes.Length - offset);
        file.newline = text.Contains("\r\n") ? "\r\n" : "\n";
        file.trailingNewline = text.EndsWith("\n", StringComparison.Ordinal);

        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        file.lines.AddRange(normalized.Split('\n'));
        if (file.lines.Count > 0 && file.lines[^1].Length == 0) file.lines.RemoveAt(file.lines.Count - 1);
        return file;
    }

    private static Encoding DetectEncoding(byte[] bytes, out int offset)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) { offset = 3; return new UTF8Encoding(true); }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) { offset = 2; return new UnicodeEncoding(false, true); }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) { offset = 2; return new UnicodeEncoding(true, true); }
        offset = 0; return new UTF8Encoding(false);
    }

    public bool TryGet(string section, string key, out string value)
    {
        foreach ((int start, int end) in Sections(section))
            for (int i = start; i < end; i++)
            {
                if (!Matches(lines[i], key, out string entry)) continue;
                value = entry; return true;
            }
        value = null; return false;
    }

    public string GetString(string section, string key, string fallback = "")
    {
        return TryGet(section, key, out string value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }

    public bool GetBool(string section, string key, bool fallback)
    {
        return TryGet(section, key, out string value) && bool.TryParse(value, out bool result) ? result : fallback;
    }

    public int GetInt(string section, string key, int fallback)
    {
        return TryGet(section, key, out string value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
    }

    public void Set(string section, string key, string value)
    {
        string line = key + "=" + value;
        foreach ((int start, int end) in Sections(section))
        {
            for (int i = start; i < end; i++)
            {
                if (!Matches(lines[i], key, out _)) continue;
                lines[i] = line; return;
            }
            // Insert after the last non-empty entry so comments at the section
            // tail stay attached to it.
            int insert = end;
            while (insert > start && lines[insert - 1].Trim().Length == 0) insert--;
            lines.Insert(insert, line); return;
        }

        int header = lines.FindIndex(x => IsSection(x, section));
        if (header >= 0) { lines.Insert(header + 1, line); return; }
        if (lines.Count > 0 && lines[^1].Trim().Length != 0) lines.Add("");
        lines.Add("[" + section + "]");
        lines.Add(line);
    }

    public void Set(string section, string key, int value) => Set(section, key, value.ToString(CultureInfo.InvariantCulture));
    public void Set(string section, string key, bool value) => Set(section, key, value.ToString());

    public bool Remove(string section, string key)
    {
        foreach ((int start, int end) in Sections(section))
            for (int i = start; i < end; i++)
            {
                if (!Matches(lines[i], key, out _)) continue;
                lines.RemoveAt(i); return true;
            }
        return false;
    }

    /// <summary>Writes through a temporary file so a failed write cannot truncate the original.</summary>
    public void Save()
    {
        string target = Path;
        string directory = System.IO.Path.GetDirectoryName(target);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("Configuration directory is missing: " + directory);
        string temp = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length != 0) stream.Write(preamble);
                byte[] body = encoding.GetBytes(string.Join(newline, lines) + (trailingNewline && lines.Count != 0 ? newline : ""));
                stream.Write(body);
                stream.Flush(true);
            }
            if (!Exists) { File.Move(temp, Path); }
            else
            {
                // File.Replace is the atomic path but some file systems reject it
                // (for example SMB shares); an overwriting move is the fallback.
                try { File.Replace(temp, Path, null); }
                catch (IOException) { File.Move(temp, Path, true); }
            }
            Exists = true;
        }
        catch
        {
            try { File.Delete(temp); } catch { }
            throw;
        }
    }

    private IEnumerable<(int Start, int End)> Sections(string section)
    {
        int start = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (!IsSectionHeader(lines[i])) continue;
            if (start >= 0) { yield return (start, i); start = -1; }
            if (IsSection(lines[i], section)) start = i + 1;
        }
        if (start >= 0) yield return (start, lines.Count);
    }

    private static bool IsSectionHeader(string line) => line.StartsWith("[", StringComparison.Ordinal) && line.TrimEnd().EndsWith("]", StringComparison.Ordinal);
    private static bool IsSection(string line, string section) => string.Equals(line.Trim(), "[" + section + "]", StringComparison.OrdinalIgnoreCase);

    private static bool Matches(string line, string key, out string value)
    {
        value = null;
        int equals = line.IndexOf('=');
        if (equals <= 0 || !string.Equals(line[..equals].Trim(), key, StringComparison.OrdinalIgnoreCase)) return false;
        value = line[(equals + 1)..].Trim();
        return true;
    }
}
