using System;
using System.Text.RegularExpressions;

namespace Server.Scripting
{
    /// <summary>
    /// Argument checks for the #IF table. Each check entry binds one of these delegates, so the
    /// validation shape sits next to the keyword instead of in a switch over the condition kind.
    /// </summary>
    public static class ScriptCheckValidators
    {
        /// <summary>Check that constrains no argument.</summary>
        public static bool Unconstrained(ScriptParseContext ctx)
        {
            return true;
        }

        /// <summary>&lt;operator&gt; &lt;unsigned short&gt;</summary>
        public static bool OperatorUnsignedShort(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Operator(parts[1]) && UnsignedShort(parts[2]);
        }

        /// <summary>&lt;operator&gt; &lt;unsigned integer&gt;</summary>
        public static bool OperatorUnsignedInteger(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Operator(parts[1]) && UnsignedInteger(parts[2]);
        }

        /// <summary>&lt;operator&gt; &lt;integer&gt;</summary>
        public static bool OperatorInteger(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Operator(parts[1]) && Integer(parts[2]);
        }

        /// <summary>&lt;integer&gt; &lt;integer&gt;</summary>
        public static bool TwoIntegers(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Integer(parts[1]) && Integer(parts[2]);
        }

        /// <summary>&lt;integer&gt;</summary>
        public static bool SingleInteger(ScriptParseContext ctx)
        {
            return Integer(ctx.Parts[1]);
        }

        /// <summary>&lt;integer&gt; &lt;integer&gt; &lt;integer&gt;</summary>
        public static bool ThreeIntegers(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Integer(parts[1]) && Integer(parts[2]) && Integer(parts[3]);
        }

        /// <summary>&lt;operator&gt; &lt;count&gt; &lt;map&gt; [instance]</summary>
        public static bool OperatorIntegerWithOptionalInstance(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Operator(parts[1]) && Integer(parts[2]) && (parts.Length < 5 || Integer(parts[4]));
        }

        /// <summary>&lt;monster&gt; &lt;operator&gt; &lt;count&gt; &lt;map&gt; [instance]</summary>
        public static bool ExactMonster(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return Operator(parts[2]) && Integer(parts[3]) && (parts.Length < 6 || Integer(parts[5]));
        }

        /// <summary>&lt;item&gt; [count] [durability]</summary>
        public static bool Item(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return parts[1].Length > 0 && (parts.Length < 3 || UnsignedShort(parts[2]))
                && (parts.Length < 4 || UnsignedShort(parts[3]));
        }

        /// <summary>&lt;Male|Female&gt;</summary>
        public static bool Gender(ScriptParseContext ctx)
        {
            return EnumValue<MirGender>(ctx.Parts[1], false);
        }

        /// <summary>&lt;class&gt;</summary>
        public static bool Class(ScriptParseContext ctx)
        {
            return EnumValue<MirClass>(ctx.Parts[1]);
        }

        /// <summary>&lt;buff&gt;</summary>
        public static bool Buff(ScriptParseContext ctx)
        {
            return EnumValue<BuffType>(ctx.Parts[1]);
        }

        /// <summary>&lt;day&gt; written as an English enum name.</summary>
        public static bool DayName(ScriptParseContext ctx)
        {
            return NamedEnumValue<DayOfWeek>(ctx.Parts[1]);
        }

        /// <summary>&lt;light&gt; written as an English enum name.</summary>
        public static bool MapLightName(ScriptParseContext ctx)
        {
            return NamedEnumValue<LightSetting>(ctx.Parts[1]);
        }

        /// <summary>[&lt;flag index&gt;] &lt;0|1&gt;</summary>
        public static bool Flag(ScriptParseContext ctx)
        {
            Match flag = ScriptCheckHandlers.FlagRegex.Match(ctx.Parts[1]);

            if (!flag.Success || flag.Index != 0 || flag.Length != ctx.Parts[1].Length) return false;

            string index = flag.Groups[1].Value;

            return (RuntimeValue(index) || uint.TryParse(index, out uint flagIndex) && flagIndex < Globals.FlagIndexCount)
                && UnsignedInteger(ctx.Parts[2]);
        }

        /// <summary>&lt;permission&gt;</summary>
        public static bool GuildPermission(ScriptParseContext ctx)
        {
            string value = ctx.Parts[1];

            if (RuntimeValue(value)) return true;

            if (!Enum.TryParse(value, true, out GuildRankOptions permission)) return false;

            int allowed = 0;

            foreach (GuildRankOptions option in Enum.GetValues(typeof(GuildRankOptions))) allowed |= (int)option;

            return ((int)permission & ~allowed) == 0;
        }

        /// <summary>&lt;hour&gt; between 0 and 23.</summary>
        public static bool Hour(ScriptParseContext ctx)
        {
            string value = ctx.Parts[1];

            return RuntimeValue(value) || uint.TryParse(value, out uint hour) && hour < 24;
        }

        /// <summary>&lt;minute&gt; between 0 and 59.</summary>
        public static bool Minute(ScriptParseContext ctx)
        {
            string value = ctx.Parts[1];

            return RuntimeValue(value) || uint.TryParse(value, out uint minute) && minute < 60;
        }

        /// <summary>&lt;timer&gt; &lt;seconds&gt; &lt;operator&gt;</summary>
        public static bool Timer(ScriptParseContext ctx)
        {
            string[] parts = ctx.Parts;

            return (RuntimeValue(parts[2]) || long.TryParse(parts[2], out _)) && Operator(parts[3]);
        }

        /// <summary>&lt;chance&gt; greater than zero.</summary>
        public static bool RandomChance(ScriptParseContext ctx)
        {
            string value = ctx.Parts[1];

            return RuntimeValue(value) || int.TryParse(value, out int chance) && chance > 0;
        }

        /// <summary>&lt;left&gt; &lt;operator&gt; &lt;right&gt;</summary>
        public static bool CalcOperator(ScriptParseContext ctx)
        {
            return Operator(ctx.Parts[2]);
        }

        private static bool RuntimeValue(string value)
        {
            // %ARG(n) is substituted while the page is parsed; one left over disables the segment at
            // load, so it is never a run-time value.
            return ScriptLexer.Placeholder.IsMatch(value)
                || value.Length == 3 && value[0] == '%' && ScriptLexer.IsVariableKey(value);
        }

        private static bool Integer(string value)
        {
            return RuntimeValue(value) || int.TryParse(value, out _);
        }

        private static bool UnsignedInteger(string value)
        {
            return RuntimeValue(value) || uint.TryParse(value, out _);
        }

        private static bool UnsignedShort(string value)
        {
            return RuntimeValue(value) || ushort.TryParse(value, out _);
        }

        private static bool EnumValue<T>(string value, bool ignoreCase = true) where T : struct, Enum
        {
            return RuntimeValue(value) || Enum.TryParse(value, ignoreCase, out T parsed) && Enum.IsDefined(typeof(T), parsed);
        }

        private static bool NamedEnumValue<T>(string value) where T : struct, Enum
        {
            return RuntimeValue(value) || Enum.TryParse(value, true, out T parsed)
                && Enum.IsDefined(typeof(T), parsed) && string.Equals(value, parsed.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool Operator(string value)
        {
            if (RuntimeValue(value)) return true;

            try
            {
                ScriptSegment.Compare(value, 0, 0);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
