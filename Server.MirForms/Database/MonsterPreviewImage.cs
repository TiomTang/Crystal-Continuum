using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Server.Database
{
    // Read-only subset of LibraryViewer's MLibrary: the editor only needs one standing frame.
    internal static class MonsterPreviewImage
    {
        private const int CacheLimit = 8;
        private static readonly Dictionary<string, Bitmap> Cache = new();
        private static readonly Queue<string> CacheOrder = new();
        private static string ClientDataPath()
        {
            foreach (var entry in Settings.VersionPath.Split(','))
            {
                var configured = entry.Trim();
                if (!File.Exists(configured)) continue;
                var data = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configured)), "Data");
                if (Directory.Exists(Path.Combine(data, "Monster"))) return data;
            }
            return null;
        }

        internal static Bitmap Get(Monster image)
        {
            var path = LibPath(image);
            if (path == null || !File.Exists(path)) return null;
            if (Cache.TryGetValue(path, out var cached)) return cached;
            Bitmap bitmap;
            try
            {
                bitmap = LoadFrame(path);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OverflowException)
            {
                return null;
            }
            if (bitmap == null) return null;
            Cache[path] = bitmap;
            CacheOrder.Enqueue(path);
            while (CacheOrder.Count > CacheLimit) Cache.Remove(CacheOrder.Dequeue());
            return bitmap;
        }

        internal static void ClearCache()
        {
            foreach (var bitmap in Cache.Values) bitmap.Dispose();
            Cache.Clear();
            CacheOrder.Clear();
        }

        private static string LibPath(Monster image)
        {
            var root = ClientDataPath();
            if (root == null) return null;
            // Same library split as the client's MonsterObject.
            int value = (ushort)image;
            if (value is >= 940 and < 950) return Path.Combine(root, "Siege", (value - 940).ToString("00") + ".Lib");
            if (value is >= 950 and < 1000) return Path.Combine(root, "Gate", (value - 950).ToString("00") + ".Lib");
            if (value >= 10000) return Path.Combine(root, "Pet", (value - 10000).ToString("00") + ".Lib");
            return Path.Combine(root, "Monster", value.ToString("000") + ".Lib");
        }

        private static Bitmap LoadFrame(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);
            int version = reader.ReadInt32();
            if (version < 2 || version > 3) throw new InvalidDataException($"{Path.GetFileName(path)}: unsupported client lib version {version}.");
            int count = reader.ReadInt32();
            int frameSeek = version >= 3 ? reader.ReadInt32() : 0;
            if (count <= 0) return null;
            var offsets = new int[count];
            for (int i = 0; i < count; i++) offsets[i] = reader.ReadInt32();

            // FrameSet.DefaultMonster standing: start 0, 4 frames, stride 4.
            int start = 0, stride = 4;
            if (frameSeek > 0 && frameSeek < stream.Length)
            {
                stream.Position = frameSeek;
                int frames = reader.ReadInt32();
                for (int i = 0; i < frames; i++)
                {
                    var action = (MirAction)reader.ReadByte();
                    int frameStart = reader.ReadInt32(), frameCount = reader.ReadInt32(), frameSkip = reader.ReadInt32();
                    for (int field = 0; field < 5; field++) reader.ReadInt32(); // interval and effect fields
                    reader.ReadBoolean();
                    reader.ReadBoolean();
                    if (action != MirAction.Standing) continue;
                    start = frameStart;
                    stride = frameCount + frameSkip;
                }
            }

            int index = start + stride * (int)MirDirection.Down;
            if ((uint)index >= (uint)count) index = Math.Clamp(start, 0, count - 1);
            if ((uint)offsets[index] >= (uint)stream.Length) return null;
            stream.Position = offsets[index];
            short width = reader.ReadInt16(), height = reader.ReadInt16();
            stream.Position += 8; // X, Y, ShadowX, ShadowY
            reader.ReadByte();    // Shadow
            int length = reader.ReadInt32();
            if (width < 2 || height < 2 || length <= 0 || length > stream.Length) return null;
            var pixels = Decompress(reader.ReadBytes(length));
            int size = width * height * 4;
            if (pixels.Length < size) return null;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try { Marshal.Copy(pixels, 0, data.Scan0, size); }
            finally { bitmap.UnlockBits(data); }
            return bitmap;
        }

        private static byte[] Decompress(byte[] packed)
        {
            using var gzip = new GZipStream(new MemoryStream(packed), CompressionMode.Decompress);
            using var buffer = new MemoryStream();
            gzip.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
