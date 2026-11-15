using System;
using System.Collections.Generic;

namespace Server.Scripting
{
    /// <summary>
    /// One parsed body line of a quest section. Values are already converted the way the runtime has always
    /// converted them, including the fallbacks: a count that is present but unparsable is 0, not the default 1.
    /// </summary>
    public sealed class QuestScriptRow
    {
        /// <summary>Where the line came from; <see cref="ScriptSourceInfo.Text"/> is the raw line, unchanged.</summary>
        public ScriptSourceInfo Source { get; }

        /// <summary>Item or monster name for task/reward rows, the flag token for flag rows, the raw line otherwise.</summary>
        public string Name { get; }

        /// <summary>Task count; 0 when the count was present but invalid, matching the runtime fallback.</summary>
        public int Count { get; }

        /// <summary>Value of a numeric section line, 0 when the line was not a valid number.</summary>
        public uint Number { get; }

        /// <summary>Flag number for flag rows; 0 when the token was not a number, -1 only when no token existed.</summary>
        public int Flag { get; }

        /// <summary>First quoted message on the line, or an empty string.</summary>
        public string Message { get; }

        internal QuestScriptRow(ScriptSourceInfo source, string name, int count, uint number, int flag, string message)
        {
            Source = source;
            Name = name ?? string.Empty;
            Count = count;
            Number = number;
            Flag = flag;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>One occurrence of a section header and the body lines the runtime consumes for it.</summary>
    public sealed class QuestScriptSection
    {
        public QuestScriptSectionEntry Entry { get; }

        /// <summary>Header line, 1-based.</summary>
        public int Line { get; }

        /// <summary>
        /// Last body line that belongs to this section, 1-based and inclusive, including blank lines the
        /// runtime skips; equal to <see cref="Line"/> when the section has no body.
        /// </summary>
        public int EndLine { get; }

        /// <summary>Body lines that produced a row, in source order; blank lines are not rows.</summary>
        public IReadOnlyList<QuestScriptRow> Rows { get; }

        internal QuestScriptSection(QuestScriptSectionEntry entry, int line, int endLine, IReadOnlyList<QuestScriptRow> rows)
        {
            Entry = entry;
            Line = line;
            EndLine = endLine;
            Rows = rows ?? Array.Empty<QuestScriptRow>();
        }
    }

    /// <summary>
    /// One quest file, parsed. Sections keep source order so an editor can show the file as written; the
    /// runtime groups them by catalog entry instead, which is the order it has always consumed them in.
    /// </summary>
    public sealed class QuestScriptDocument
    {
        public IReadOnlyList<QuestScriptSection> Sections { get; }

        /// <summary>Local problems with the file. Never routed through the global script diagnostic sinks.</summary>
        public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; }

        internal QuestScriptDocument(IReadOnlyList<QuestScriptSection> sections, IReadOnlyList<ScriptDiagnostic> diagnostics)
        {
            Sections = sections ?? Array.Empty<QuestScriptSection>();
            Diagnostics = diagnostics ?? Array.Empty<ScriptDiagnostic>();
        }

        internal static readonly QuestScriptDocument Empty = new QuestScriptDocument(null, null);
    }
}
