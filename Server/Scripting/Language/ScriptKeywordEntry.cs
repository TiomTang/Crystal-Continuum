using System;
using System.Collections.Generic;

namespace Server.Scripting
{
    /// <summary>
    /// Which part of an NPC script a keyword belongs to. The editor filters the command palette by
    /// the block the caret sits in, so this is what decides where a keyword may be inserted.
    /// </summary>
    public enum ScriptKeywordKind
    {
        Action,
        Check,
        Variable,
    }

    /// <summary>Grouping used by the command palette and by the keyword help.</summary>
    public enum ScriptKeywordCategory
    {
        Player,
        Item,
        Currency,
        Social,
        Guild,
        Group,
        Pet,
        Hero,
        Skill,
        Quest,
        World,
        Movement,
        Conquest,
        Combat,
        Time,
        Buff,
        Mail,
        Message,
        Admin,
        Flow,
        Variable,
    }

    /// <summary>
    /// How a line puts a button next to the NPC text. The loader collects the buttons and the editor
    /// checks that the page they point at exists, so both read this instead of listing the actions.
    /// </summary>
    public enum ScriptButtonKind
    {
        None,

        /// <summary>The first argument names the page.</summary>
        Page,

        /// <summary>The second argument names the page.</summary>
        DelayedPage,

        /// <summary>The first argument names the page, and the button sits with the plain buttons.</summary>
        Roll,
    }

    /// <summary>
    /// What the first data argument of a line names, so a check can ask the running server whether it
    /// exists. The editor validates against these instead of keeping a list of action names.
    /// </summary>
    public enum ScriptArgumentKind
    {
        None,

        /// <summary>A skill name.</summary>
        Spell,

        /// <summary>A map file name.</summary>
        Map,

        /// <summary>An item name.</summary>
        Item,
    }

    /// <summary>Result of matching one script line against the keyword table.</summary>
    public enum ScriptParseOutcome
    {
        /// <summary>The keyword was recognised and its effect applied.</summary>
        Handled,

        /// <summary>The keyword was recognised but the line was too short, so nothing was added.</summary>
        Skipped,

        /// <summary>No keyword of this kind carries that name.</summary>
        Unknown,
    }

    /// <summary>
    /// One row of the NPC script keyword table.
    /// <para>
    /// The table is the single source of truth: dispatch, validation and the editor's command
    /// palette all read it, so adding a keyword means adding one row plus its handler and nothing
    /// else has to be kept in sync by hand.
    /// </para>
    /// </summary>
    public sealed class ScriptKeywordEntry
    {
        public ScriptKeywordEntry(string name, ScriptKeywordKind kind, ScriptKeywordCategory category, string usage,
            string description, int minParts, Func<ScriptParseContext, ScriptParseOutcome> handler,
            ScriptCheckKind? check = null, ScriptActionKind? action = null, Func<ScriptExecuteContext, ScriptOutcome> executor = null,
            Func<ScriptExecuteContext, ScriptOutcome> monsterExecutor = null, Func<ScriptExecuteContext, string> valueExecutor = null,
            ScriptButtonKind button = ScriptButtonKind.None, ScriptArgumentKind argument = ScriptArgumentKind.None,
            Func<ScriptParseContext, bool> validateArguments = null)
        {
            Button = button;
            Argument = argument;
            Check = check;
            ValidateArguments = validateArguments;
            Action = action;
            Executor = executor;
            MonsterExecutor = monsterExecutor;
            ValueExecutor = valueExecutor;
            Name = name;
            Kind = kind;
            Category = category;
            Usage = usage;
            Description = description;
            MinParts = minParts;
            Handler = handler;
        }

        /// <summary>Keyword as written in scripts, without the leading #.</summary>
        public string Name { get; }

        public ScriptKeywordKind Kind { get; }

        public ScriptKeywordCategory Category { get; }

        /// <summary>Argument template shown by the editor, for example "&lt;operator&gt; &lt;level&gt;".</summary>
        public string Usage { get; }

        /// <summary>
        /// English description, which doubles as the localization key so the editor can show a
        /// translation through GameLanguage.Get.
        /// </summary>
        public string Description { get; }

        /// <summary>Fewest parts a line needs, counting the keyword itself.</summary>
        public int MinParts { get; }

        public Func<ScriptParseContext, ScriptParseOutcome> Handler { get; }

        /// <summary>The check this keyword produces, which is also the key of its executor.</summary>
        public ScriptCheckKind? Check { get; }

        /// <summary>
        /// Validates the literal arguments of a condition before its handler runs. Every check entry
        /// binds one, including the entries that accept any argument.
        /// </summary>
        public Func<ScriptParseContext, bool> ValidateArguments { get; }

        /// <summary>The action this keyword produces.</summary>
        public ScriptActionKind? Action { get; }

        /// <summary>
        /// Runs the keyword. Generated from the old switch in ScriptSegment, so one entry now carries the
        /// name, the argument template, the description, the parser and the behaviour.
        /// </summary>
        public Func<ScriptExecuteContext, ScriptOutcome> Executor { get; }

        /// <summary>
        /// Only set for the handful of actions that behave differently when a monster runs the script.
        /// </summary>
        public Func<ScriptExecuteContext, ScriptOutcome> MonsterExecutor { get; }

        /// <summary>Set on the actions that open a button, so the loader and the editor agree.</summary>
        public ScriptButtonKind Button { get; }

        /// <summary>What the first data argument names, when the editor can check that it exists.</summary>
        public ScriptArgumentKind Argument { get; }

        /// <summary>Resolves a &lt;$VALUE&gt; placeholder to the text that replaces it.</summary>
        public Func<ScriptExecuteContext, string> ValueExecutor { get; }
    }
}
