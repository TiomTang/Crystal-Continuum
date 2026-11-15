using System;
using System.IO;
using System.Text;

namespace Server.Scripting
{
    /// <summary>
    /// One script file's text plus the facts needed to write it back unchanged: the encoding, the byte
    /// order mark it started with and the newline it used. Decoding and re-encoding are two halves of one
    /// rule, so they live on the same object instead of every caller re-deriving them.
    /// </summary>
    public sealed class ScriptTextDocument
    {
        private readonly byte[] _preamble;

        internal ScriptTextDocument(string text, Encoding encoding, byte[] preamble, string newLine, bool isLegacy)
        {
            Text = text ?? string.Empty;
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _preamble = preamble ?? Array.Empty<byte>();
            HasBom = _preamble.Length > 0;
            NewLine = string.IsNullOrEmpty(newLine) ? Environment.NewLine : newLine;
            IsLegacy = isLegacy;
        }

        /// <summary>The decoded text, still carrying the file's own line endings.</summary>
        public string Text { get; }

        /// <summary>The encoding the file was read with, and the one Save encodes with.</summary>
        public Encoding Encoding { get; }

        /// <summary>True when the file began with a byte order mark; Save writes that same mark back.</summary>
        public bool HasBom { get; }

        /// <summary>The file's dominant line ending: "\r\n", "\n" or "\r".</summary>
        public string NewLine { get; }

        /// <summary>True when the bytes were not UTF-8 and the file was read as GBK.</summary>
        public bool IsLegacy { get; }

        /// <summary>
        /// Writes text with this document's encoding and byte order mark, after giving it this document's
        /// line endings. An editor that joins its buffer with "\n" can pass that buffer directly. A file
        /// that used one ending throughout and holds ordinary text round-trips to the same bytes; a file
        /// that mixed endings is written with its dominant one, because one NewLine describes one file.
        /// A character the encoding cannot represent throws EncoderFallbackException before the file is
        /// touched, so a GBK script never gets a silent '?' where the author typed something new.
        /// </summary>
        public void Save(string path, string text)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            // Encode with a throwing fallback, and do it before opening the file: a code page that cannot
            // represent a newly typed character must fail here, with the old file still intact.
            var writer = (System.Text.Encoding)Encoding.Clone();
            writer.EncoderFallback = EncoderFallback.ExceptionFallback;

            byte[] body = writer.GetBytes(ApplyNewLine(text));

            if (_preamble.Length == 0)
            {
                File.WriteAllBytes(path, body);
                return;
            }

            // The mark and the body go out as one buffer, so no separate preamble write can be left
            // behind. It is still an ordinary overwrite, not an atomic replace of the file.
            var bytes = new byte[_preamble.Length + body.Length];
            Buffer.BlockCopy(_preamble, 0, bytes, 0, _preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, _preamble.Length, body.Length);
            File.WriteAllBytes(path, bytes);
        }

        private string ApplyNewLine(string text)
        {
            text = text ?? string.Empty;

            // Collapse every ending to LF, then expand to the file's own. An editor buffer that already
            // uses the file's ending goes through both steps and comes out identical.
            string normalized = text.IndexOf('\r') >= 0 ? text.Replace("\r\n", "\n").Replace('\r', '\n') : text;
            return NewLine == "\n" ? normalized : normalized.Replace("\n", NewLine);
        }

        /// <summary>
        /// The line ending text uses most: CRLF wins a tie, because the Envir tree already uses it. Text
        /// with no line ending reports the platform's, which only matters once an editor adds one.
        /// </summary>
        internal static string DetectNewLine(string text)
        {
            int crlf = 0, lf = 0, cr = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') { crlf++; i++; }
                    else cr++;
                }
                else if (text[i] == '\n') lf++;
            }

            if (crlf == 0 && lf == 0 && cr == 0) return Environment.NewLine;
            if (crlf >= lf && crlf >= cr) return "\r\n";
            return lf >= cr ? "\n" : "\r";
        }
    }
}
