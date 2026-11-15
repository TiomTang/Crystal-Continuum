using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Scripting
{
    /// <summary>
    /// One encoding rule for every script file: a BOM wins, otherwise the bytes must be valid UTF-8, and a
    /// file that is not is read as GBK, the code page the older scripts were written in. Reading bytes
    /// without a rule turns a GBK script into plausible-looking mojibake and nobody ever finds out.
    /// </summary>
    public static class ScriptTextFile
    {
        /// <summary>Decoding is strict so invalid UTF-8 is detected; writing uses the same encoding, leniently.</summary>
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>UTF-8 without a BOM: what Save writes, and what a script file without one is.</summary>
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        private static readonly byte[] Utf8Preamble = { 0xEF, 0xBB, 0xBF };
        private static readonly byte[] Utf16LePreamble = { 0xFF, 0xFE };
        private static readonly byte[] Utf16BePreamble = { 0xFE, 0xFF };
        private static readonly byte[] NoPreamble = Array.Empty<byte>();

        /// <summary>
        /// Reads the file and keeps what writing it back needs. A UTF-16 mark wins over any text decoder,
        /// so it is never read as GBK; a UTF-8 mark stays even when the body behind it turns out to be GBK.
        /// </summary>
        public static ScriptTextDocument Read(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            int offset = 0;
            byte[] preamble = NoPreamble;

            if (bytes.Length >= 3 && bytes[0] == Utf8Preamble[0] && bytes[1] == Utf8Preamble[1] && bytes[2] == Utf8Preamble[2])
            {
                offset = 3;
                preamble = Utf8Preamble;
            }
            // Both UTF-16 marks are settled before any text decoder sees the bytes, so neither is ever
            // mistaken for a GBK pair.
            else if (bytes.Length >= 2 && bytes[0] == Utf16LePreamble[0] && bytes[1] == Utf16LePreamble[1])
                return Decode(bytes, 2, Encoding.Unicode, Encoding.Unicode, Utf16LePreamble, false);
            else if (bytes.Length >= 2 && bytes[0] == Utf16BePreamble[0] && bytes[1] == Utf16BePreamble[1])
                return Decode(bytes, 2, Encoding.BigEndianUnicode, Encoding.BigEndianUnicode, Utf16BePreamble, false);

            try
            {
                return Decode(bytes, offset, StrictUtf8, Utf8, preamble, false);
            }
            catch (DecoderFallbackException)
            {
                // Legacy bytes behind a UTF-8 mark keep that mark: the file is written back as it was read.
                Encoding gbk = TextEncodings.Gbk;

                return Decode(bytes, offset, gbk, gbk, preamble, true);
            }
        }

        private static ScriptTextDocument Decode(byte[] bytes, int offset, Encoding reader, Encoding writer, byte[] preamble, bool legacy)
        {
            string text = reader.GetString(bytes, offset, bytes.Length - offset);
            return new ScriptTextDocument(text, writer, preamble, ScriptTextDocument.DetectNewLine(text), legacy);
        }

        /// <summary>Decodes the file; legacy says it was not UTF-8 and GBK was used instead.</summary>
        public static string ReadAllText(string path, out bool legacy)
        {
            ScriptTextDocument document = Read(path);
            legacy = document.IsLegacy;
            return document.Text;
        }

        /// <summary>Same split as File.ReadAllLines: \r\n, \r and \n, with no trailing empty entry.</summary>
        public static string[] ReadAllLines(string path, out bool legacy)
        {
            ScriptTextDocument document = Read(path);
            legacy = document.IsLegacy;
            return SplitLines(document.Text);
        }

        /// <summary>The engine's line split: \r\n, \r and \n, with no trailing empty entry.</summary>
        public static string[] SplitLines(string text)
        {
            text = text ?? string.Empty;

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
