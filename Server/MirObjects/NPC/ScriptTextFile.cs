using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.MirObjects
{
    /// <summary>
    /// One encoding rule for every script file: a BOM wins, otherwise the bytes must be valid UTF-8, and a
    /// file that is not is read as GBK, the code page the older scripts were written in. Reading bytes
    /// without a rule turns a GBK script into plausible-looking mojibake and nobody ever finds out.
    /// </summary>
    public static class ScriptTextFile
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>Decodes the file; legacy says it was not UTF-8 and GBK was used instead.</summary>
        public static string ReadAllText(string path, out bool legacy)
        {
            byte[] bytes = File.ReadAllBytes(path);

            legacy = false;

            int offset = 0;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                offset = 3;
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            try
            {
                return StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
            }
            catch (DecoderFallbackException)
            {
                legacy = true;

                return TextEncodings.Gbk.GetString(bytes, offset, bytes.Length - offset);
            }
        }

        /// <summary>Same split as File.ReadAllLines: \r\n, \r and \n, with no trailing empty entry.</summary>
        public static string[] ReadAllLines(string path, out bool legacy)
        {
            string text = ReadAllText(path, out legacy);

            var lines = new List<string>();

            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\r' && text[i] != '\n') continue;

                lines.Add(text.Substring(start, i - start));

                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;

                start = i + 1;
            }

            if (start < text.Length) lines.Add(text.Substring(start));

            return lines.ToArray();
        }
    }
}
