using System.Collections;
using System.Drawing;
using Server.MirDatabase;

namespace Server.Database;

internal sealed class MonsterRespawnAdvice
{
    private const int MaxSpread = 20;
    private readonly BitArray walkable;
    public int Width { get; }
    public int Height { get; }
    public Point? Location { get; private set; }
    public ushort Spread { get; private set; }
    public ushort Count { get; private set; } = 1;
    public ushort Delay { get; private set; } = 5;
    public ushort RandomDelay { get; private set; }

    private MonsterRespawnAdvice(int width = 0, int height = 0)
    {
        Width = width;
        Height = height;
        walkable = new BitArray(width * height);
    }

    // Map.GetWalkableCells builds the spawn pool before creating safe zones and NPCs.
    public bool IsSpawnable(int x, int y) => (uint)x < Width && (uint)y < Height && walkable[x * Height + y];

    public static MonsterRespawnAdvice Create(MapInfo map, IReadOnlyList<MapInfo> maps, int monsterIndex)
    {
        if (string.IsNullOrWhiteSpace(map?.FileName)) return new();
        MonsterRespawnAdvice result;
        try
        {
            string root = Path.GetFullPath(Settings.MapPath);
            string path = Path.GetFullPath(Path.Combine(root, map.FileName + ".map"));
            string relative = Path.GetRelativePath(root, path);
            if (Path.IsPathRooted(relative) || relative.StartsWith(".." + Path.DirectorySeparatorChar)) return new();
            using var stream = File.OpenRead(path);
            result = ReadTerrain(stream);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return new();
        }

        RespawnInfo Reference(MapInfo m) => m?.Respawns?.FirstOrDefault(r => r != null && r.MonsterIndex == monsterIndex && r.Count > 0 && r.Delay > 0 && r.RespawnTicks == 0);
        var template = Reference(map) ?? maps?.Select(Reference).FirstOrDefault(r => r != null);
        // Prefer the nearest usable cell to the centre. Never transplant coordinates across maps.
        int cx = result.Width / 2, cy = result.Height / 2;
        int preferredSpread = template?.Spread ?? Math.Min(result.Width, result.Height) / 20;
        long best = long.MaxValue;
        for (int x = 0; x < result.Width; x++)
            for (int y = 0; y < result.Height; y++)
            {
                long distance = (long)(x - cx) * (x - cx) + (long)(y - cy) * (y - cy);
                if (distance >= best || !result.IsSpawnable(x, y)) continue;
                var point = new Point(x, y);
                int radius = Math.Min(Math.Min(x, result.Width - 1 - x), Math.Min(y, result.Height - 1 - y));
                foreach (var zone in map.SafeZones)
                {
                    long clearance = Math.Max(Math.Abs((long)x - zone.Location.X), Math.Abs((long)y - zone.Location.Y)) - zone.Size - 1;
                    if (clearance < radius) radius = (int)clearance;
                    if (radius < 0) break;
                }
                if (radius < 0) continue;
                best = distance;
                result.Location = point;
                result.Spread = (ushort)Math.Min(radius, Math.Min(MaxSpread, preferredSpread));
            }
        if (result.Location.HasValue && template != null)
        {
            result.Count = template.Count;
            result.Delay = template.Delay;
            result.RandomDelay = Math.Min(template.RandomDelay, (ushort)(template.Delay - 1));
        }
        return result;
    }

    // Same format detection, column order and wall bits as MirEnvir.Map. Read only terrain:
    // Map.Load also constructs live doors, NPCs, spells and timers and must not be called here.
    private static MonsterRespawnAdvice ReadTerrain(FileStream stream)
    {
        byte[] h = new byte[40];
        // A valid 1x1 custom map occupies only 34 bytes.
        stream.ReadExactly(h.AsSpan(0, (int)Math.Min(h.Length, stream.Length)));
        if (stream.Length < 8) throw new InvalidDataException();
        int Word(int i) => BitConverter.ToInt16(h, i);
        int w, height, stride, offset, xor = 0, format;
        if (h[2] == 'C' && h[3] == '#')
        {
            if (Word(0) != 1) throw new InvalidDataException();
            (format, w, height, offset, stride) = (100, Word(4), Word(6), 8, 26);
        }
        else if (h[0] == 0)
        {
            w = Word(22); height = Word(24);
            (format, offset, stride) = (5, 28 + 3 * ((w + 1) / 2) * (height / 2), 14);
        }
        else if (h[0] == 15 && h[5] == 'S' && h[14] == '3')
            (format, w, height, offset, stride) = (6, Word(16), Word(18), 40, 20);
        else if (h[0] == 21 && h[4] == '2' && h[6] == 'A' && h[19] == '1')
        {
            xor = Word(33);
            (format, w, height, offset, stride) = (4, Word(31) ^ xor, Word(35) ^ xor, 64, 12);
        }
        else if (h[0] == 16 && h[2] == 'a' && h[7] == '1' && h[14] == '1')
        {
            xor = Word(23);
            (format, w, height, offset, stride) = (1, Word(21) ^ xor, Word(25) ^ xor, 54, 15);
        }
        else if (h[4] == 15 || h[4] == 3 && h[18] == 13 && h[19] == 10)
        {
            w = Word(0); height = Word(2);
            bool extended = stream.Length > 52 + (long)w * height * 14;
            (format, offset, stride) = (extended ? 3 : 2, 52, extended ? 36 : 14);
        }
        else if (h[0] == 13 && h[1] == 'L' && h[7] == ' ' && h[11] == 'm')
            (format, w, height, offset, stride) = (7, Word(21), Word(25), 54, 15);
        else
            (format, w, height, offset, stride) = (0, Word(0), Word(2), 52, 12);

        long cells = (long)w * height;
        if (w <= 0 || height <= 0 || cells > 4_194_304 || offset + cells * stride > stream.Length)
            throw new InvalidDataException();
        stream.Position = offset;
        var result = new MonsterRespawnAdvice(w, height);
        byte[] column = new byte[height * stride];
        for (int x = 0; x < w; x++)
        {
            stream.ReadExactly(column);
            for (int y = 0, i = 0; y < height; y++, i += stride)
                result.walkable[x * height + y] = format switch
                {
                    1 => (column[i + 3] & 32) == (0xAA & 32) && ((column[i + 7] ^ (xor >> 8)) & 128) == 0,
                    4 => ((column[i + 1] | column[i + 3]) & 128) == 0,
                    5 or 6 => (column[i] & 3) == 3,
                    7 => ((column[i + 1] | column[i + 7]) & 128) == 0,
                    100 => (column[i + 5] & 32) == 0 && (column[i + 13] & 128) == 0,
                    _ => ((column[i + 1] | column[i + 3] | column[i + 5]) & 128) == 0
                };
        }
        return result;
    }
}
