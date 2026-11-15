using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.MirObjects
{
    /// <summary>Bounded, fail-closed log text. Raw dynamic argument values are never emitted.</summary>
    internal static class ScriptDiagnosticText
    {
        internal const int ExceptionLimit = 32768;
        private const int InputLimit = 131072;
        private const int ArgumentLimit = 64;
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly Regex Secrets = Pattern(
            @"(?<key>[""']?(?:password|passwd|pwd|secret|(?:access[_-]?|refresh[_-]?)?token|api[_-]?key|authorization|cookie|account(?:[_-]?(?:name|id))?|user(?:[_-]?(?:name|id))?|player(?:[_-]?(?:name|id))?|chat|input(?:str)?|credential|密码|口令|账号|账户|聊天|输入)[""']?\s*[:=]\s*)(?:""(?:\\.|[^""\\])*(?:""|$)|'(?:\\.|[^'\\])*(?:'|$)|[^,;\r\n\}\]]+)");
        private static readonly Regex Bearer = Pattern(@"\bBearer\s+[^\s,;""']+");
        private static readonly Regex UrlCredentials = Pattern(@"(?<scheme>\b[a-z][a-z0-9+.-]*://)[^\s/@]+@");
        private static readonly Regex Email = Pattern(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b");
        private static readonly Regex Token = Pattern(@"\b[A-Z0-9_+/=-]{40,}\b");
        private static readonly Regex PageArguments = Pattern(@"(?<page>@[^()\s\[\]]+)\([^\r\n)]*\)");

        private static Regex Pattern(string pattern)
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
        }

        internal static string Safe(string value, int limit = 2048, IList<string> arguments = null, bool multiline = false)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            try
            {
                bool inputTruncated = value.Length > InputLimit;
                if (inputTruncated) value = value.Substring(0, InputLimit);
                value = NPCSegment.RedactExecutionInput(value);
                // A single-character sentinel avoids replacing text inserted by a previous redaction.
                const string hidden = "\u0001";
                value = Secrets.Replace(value, "${key}" + hidden);
                value = Bearer.Replace(value, "Bearer " + hidden);
                value = UrlCredentials.Replace(value, "${scheme}" + hidden + "@");
                value = Email.Replace(value, hidden);
                value = Token.Replace(value, hidden);
                value = PageArguments.Replace(value, "${page}(" + hidden + ")");
                // Remove dynamic values even from exception messages, which may interpolate player input.
                if (arguments != null)
                {
                    if (arguments.Count > ArgumentLimit) return "[content withheld: argument limit]";
                    int count = Math.Min(arguments.Count, ArgumentLimit);
                    for (int i = 0; i < count; i++)
                    {
                        string argument = arguments[i];
                        if (argument != null && argument.Length > InputLimit) return "[content withheld: argument limit]";
                        // Numeric/comparison arguments are useful evidence, not arbitrary text to replace
                        // inside identifiers, stack line numbers or paths (e.g. PARAM1).
                        if (!string.IsNullOrEmpty(argument) && !IsScalar(argument))
                            value = value.Replace(argument, hidden, StringComparison.Ordinal);
                    }
                }
                value = value.Replace(hidden, "[redacted]", StringComparison.Ordinal);
                var safe = new StringBuilder(Math.Min(value.Length, limit));
                bool outputTruncated = false;
                foreach (char c in value)
                {
                    if (c == '\r') continue;
                    if (c == '\n') safe.Append(multiline ? "\n" : "\\n");
                    else if (c == '\t') safe.Append(multiline ? "\t" : " ");
                    else if (char.IsControl(c) || c == '\u2028' || c == '\u2029' ||
                        (c >= '\u202a' && c <= '\u202e') || (c >= '\u2066' && c <= '\u2069'))
                        safe.Append("\\u").Append(((int)c).ToString("X4"));
                    else safe.Append(c);
                    if (safe.Length > limit) { outputTruncated = true; break; }
                }
                return Limit(safe.ToString(), limit, inputTruncated || outputTruncated);
            }
            catch (Exception)
            {
                // A timeout or unusual IList implementation must not disclose the original value.
                return "[content withheld: redaction failed]";
            }
        }

        internal static string Version(string value)
        {
            // A content SHA-256 is an identifier, not a credential-shaped token.
            if (value != null && value.Length == 64)
            {
                bool hex = true;
                foreach (char c in value) if (!Uri.IsHexDigit(c)) { hex = false; break; }
                if (hex) return value;
            }
            return Safe(value, 256);
        }

        private static string Limit(string value, int limit, bool truncated)
        {
            const string suffix = " [truncated]";
            if (!truncated && value.Length <= limit) return value;
            int length = Math.Min(value.Length, Math.Max(0, limit - suffix.Length));
            // Do not cut between the halves of a UTF-16 surrogate pair.
            if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
            return value.Substring(0, length) + suffix;
        }

        internal static string Arguments(IList<string> values)
        {
            if (values == null) return string.Empty;
            try
            {
                int count = Math.Min(values.Count, ArgumentLimit);
                var result = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    if (i > 0) result.Append(", ");
                    string value = values[i];
                    result.Append(i).Append('=').Append(IsScalar(value) ? Safe(value, 64) : "[redacted]");
                }
                if (values.Count > count) result.Append(", [additional arguments withheld]");
                return result.ToString();
            }
            catch (Exception)
            {
                return "[arguments withheld]";
            }
        }

        private static bool IsScalar(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 32) return false;
            return decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out _) ||
                value == "<" || value == ">" || value == "<=" || value == ">=" ||
                value == "==" || value == "!=" || value == "+" || value == "-" || value == "*" || value == "/";
        }

        internal static string Exception(Exception exception, IList<string> arguments)
        {
            if (exception == null) return string.Empty;
            try
            {
                return Safe(exception.ToString(), ExceptionLimit, arguments, true);
            }
            catch (Exception)
            {
                return "[exception details unavailable]";
            }
        }
    }
}
