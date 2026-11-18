using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Server.Database
{
    // Read-only subset of LibraryViewer's MLibrary: item icons are addressed directly by ItemInfo.Image.
    internal static class ItemPreviewImage
    {
        private static readonly Dictionary<ushort, Bitmap> Cache = new();

        internal static Bitmap Get(ushort image)
        {
            if (Cache.TryGetValue(image, out var cached)) return cached;
            var bitmap = Load(image);
            if (bitmap != null) Cache[image] = bitmap;
            return bitmap;
        }

        internal static void ClearCache()
        {
            foreach (var bitmap in Cache.Values) bitmap.Dispose();
            Cache.Clear();
        }

        private static string LibPath()
        {
            foreach (var entry in Settings.VersionPath.Split(','))
            {
                var configured = entry.Trim();
                if (!File.Exists(configured)) continue;
                var data = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configured)), "Data");
                var lib = Path.Combine(data, "Items.Lib");
                if (File.Exists(lib)) return lib;
            }
            return null;
        }

        private static Bitmap Load(ushort image)
        {
            var path = LibPath();
            if (path == null) return null;
            try
            {
                return LoadFrame(path, image);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OverflowException)
            {
                return null;
            }
        }

        private static Bitmap LoadFrame(string path, int index)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);
            int version = reader.ReadInt32();
            if (version < 2 || version > 3) return null;
            int count = reader.ReadInt32();
            if (version >= 3) reader.ReadInt32(); // frame set offset, unused for icons
            if (count <= 0 || index >= count) return null;

            int offset = 0;
            for (int i = 0; i <= index; i++) offset = reader.ReadInt32();

            if (offset <= 0 || offset >= stream.Length) return null;
            stream.Position = offset;
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
