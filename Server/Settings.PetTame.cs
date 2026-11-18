using System.IO;
using Shared;

namespace Server
{
    /// <summary>
    /// Configuration of the wizard temptation spell (ElectricShock): the shock and rage windows, the
    /// tame roll and the stat growth of a monster tamed this way. The defaults are the values the
    /// spell used to hardcode, so a missing entry keeps the original behaviour.
    /// </summary>
    public class PetTameSettings
    {
        /// <summary>
        /// Denominator of the shock roll, lowered by the spell level: Next(ShockChanceBase - level).
        /// It decides whether the cast does anything at all.
        /// </summary>
        public int ShockChanceBase = 4;

        public int ShockBaseSeconds = 10;
        public int ShockSecondsPerLevel = 5;

        /// <summary>Rage is applied Next(RageChanceDenominator) &gt; 0 of the time, so 5 means 80%.</summary>
        public int RageChanceDenominator = 5;

        public int RageBaseSeconds = 10;
        public int RageRandomSeconds = 20;

        /// <summary>Monsters above the caster level by more than this cannot be tamed.</summary>
        public int MaxLevelDifference = 2;

        /// <summary>
        /// Tame roll: Next(caster level + TameChanceBase + level * TameChancePerMagicLevel) has to
        /// exceed target level + TameChanceTargetOffset.
        /// </summary>
        public int TameChanceBase = 20;
        public int TameChancePerMagicLevel = 5;
        public int TameChanceTargetOffset = 10;

        /// <summary>How many tamed monsters one wizard may hold: base + step x spell level.</summary>
        public int PetLimitBase = 2;
        public int PetLimitPerMagicLevel = 1;

        /// <summary>
        /// Increment applied on each pet level. A one entry sequence is a constant increment, a
        /// longer one gives every level its own value and repeats the last entry above it.
        /// </summary>
        public int[] HealthPerLevel = { 20 };
        public int[] DamagePerLevel = { 1 };
        public int[] DefencePerLevel = { 2 };
        public int[] MagicDefencePerLevel = { 2 };

        public PetTameSettings Copy()
        {
            return new PetTameSettings
            {
                ShockChanceBase = ShockChanceBase,
                ShockBaseSeconds = ShockBaseSeconds,
                ShockSecondsPerLevel = ShockSecondsPerLevel,
                RageChanceDenominator = RageChanceDenominator,
                RageBaseSeconds = RageBaseSeconds,
                RageRandomSeconds = RageRandomSeconds,
                MaxLevelDifference = MaxLevelDifference,
                TameChanceBase = TameChanceBase,
                TameChancePerMagicLevel = TameChancePerMagicLevel,
                TameChanceTargetOffset = TameChanceTargetOffset,
                PetLimitBase = PetLimitBase,
                PetLimitPerMagicLevel = PetLimitPerMagicLevel,
                HealthPerLevel = (int[])HealthPerLevel.Clone(),
                DamagePerLevel = (int[])DamagePerLevel.Clone(),
                DefencePerLevel = (int[])DefencePerLevel.Clone(),
                MagicDefencePerLevel = (int[])MagicDefencePerLevel.Clone()
            };
        }

        /// <summary>Roll denominator of a cast: one or zero is a guaranteed shock.</summary>
        public int ShockChance(int magicLevel)
        {
            return Math.Max(1, ShockChanceBase - magicLevel);
        }

        public int ShockDuration(int magicLevel)
        {
            long seconds = ShockBaseSeconds + (long)ShockSecondsPerLevel * magicLevel;

            return (int)Math.Min(int.MaxValue, Math.Max(0, seconds) * 1000);
        }

        /// <summary>Roll denominator of the rage roll, never zero so Next is valid.</summary>
        public int RageChance()
        {
            return Math.Max(1, RageChanceDenominator);
        }

        /// <summary>
        /// Pet count of one wizard. The shared monster pet ceiling still applies, so a large value
        /// here cannot go past the rest of the pet system.
        /// </summary>
        public int PetLimit(int magicLevel)
        {
            long limit = PetLimitBase + (long)PetLimitPerMagicLevel * magicLevel;

            if (limit > Globals.MaxPets) return Globals.MaxPets;
            if (limit < 0) return 0;

            return (int)limit;
        }

        public int HealthGrowth(int petLevel) => PetSummonSettings.Growth(HealthPerLevel, petLevel);
        public int DamageGrowth(int petLevel) => PetSummonSettings.Growth(DamagePerLevel, petLevel);
        public int DefenceGrowth(int petLevel) => PetSummonSettings.Growth(DefencePerLevel, petLevel);
        public int MagicDefenceGrowth(int petLevel) => PetSummonSettings.Growth(MagicDefencePerLevel, petLevel);
    }

    public static partial class Settings
    {
        public const int PetTameMaxChance = 1000;
        public const int PetTameMaxSeconds = 3600;
        public const int PetTameMaxLevelDifference = byte.MaxValue;

        public static PetTameSettings PetTame = new PetTameSettings();

        public static string PetTamePath => Path.Combine(ConfigPath, "PetTame.ini");

        public static void LoadPetTame()
        {
            // The wizard inheritance moved out of PetInheritance.ini. The first load adopts the old
            // values, so a customised setup is not silently reset to the defaults.
            if (!File.Exists(PetTamePath)) AdoptLegacyWizardInheritance();

            var reader = new InIReader(PetTamePath);

            PetTame = ReadPetTame(reader);
            WizardPetInheritance = ReadPetInheritance(reader, "Inheritance", Stat.MaxMC, WizardPetInheritance);
        }

        public static void SavePetTame()
        {
            var reader = new InIReader(PetTamePath);

            WritePetTame(reader, PetTame);
            WritePetInheritance(reader, "Inheritance", WizardPetInheritance);
        }

        /// <summary>Reads the wizard section of PetInheritance.ini once, without writing that file back.</summary>
        private static void AdoptLegacyWizardInheritance()
        {
            if (!File.Exists(PetInheritancePath)) return;

            WizardPetInheritance = ReadPetInheritance(new InIReader(PetInheritancePath), "Wizard", Stat.MaxMC, WizardPetInheritance, false);
        }

        private static PetTameSettings ReadPetTame(InIReader reader)
        {
            var defaults = new PetTameSettings();

            return new PetTameSettings
            {
                ShockChanceBase = ClampPetSummonValue(reader.ReadInt32("Shock", "ShockChanceBase", defaults.ShockChanceBase), PetTameMaxChance),
                ShockBaseSeconds = ClampPetSummonValue(reader.ReadInt32("Shock", "ShockBaseSeconds", defaults.ShockBaseSeconds), PetTameMaxSeconds),
                ShockSecondsPerLevel = ClampPetSummonValue(reader.ReadInt32("Shock", "ShockSecondsPerLevel", defaults.ShockSecondsPerLevel), PetTameMaxSeconds),
                RageChanceDenominator = ClampPetSummonValue(reader.ReadInt32("Shock", "RageChanceDenominator", defaults.RageChanceDenominator), PetTameMaxChance),
                RageBaseSeconds = ClampPetSummonValue(reader.ReadInt32("Shock", "RageBaseSeconds", defaults.RageBaseSeconds), PetTameMaxSeconds),
                RageRandomSeconds = ClampPetSummonValue(reader.ReadInt32("Shock", "RageRandomSeconds", defaults.RageRandomSeconds), PetTameMaxSeconds),
                MaxLevelDifference = ClampPetSummonValue(reader.ReadInt32("Tame", "MaxLevelDifference", defaults.MaxLevelDifference), PetTameMaxLevelDifference),
                TameChanceBase = ClampPetSummonValue(reader.ReadInt32("Tame", "TameChanceBase", defaults.TameChanceBase), PetTameMaxChance),
                TameChancePerMagicLevel = ClampPetSummonValue(reader.ReadInt32("Tame", "TameChancePerMagicLevel", defaults.TameChancePerMagicLevel), PetTameMaxChance),
                TameChanceTargetOffset = ClampPetSummonValue(reader.ReadInt32("Tame", "TameChanceTargetOffset", defaults.TameChanceTargetOffset), PetTameMaxChance),
                PetLimitBase = ClampPetSummonValue(reader.ReadInt32("Tame", "PetLimitBase", defaults.PetLimitBase), PetSummonMaxSummonLimit),
                PetLimitPerMagicLevel = ClampPetSummonValue(reader.ReadInt32("Tame", "PetLimitPerMagicLevel", defaults.PetLimitPerMagicLevel), PetSummonMaxSummonLimit),
                HealthPerLevel = ReadGrowth(reader, "Growth", "HealthPerLevel", defaults.HealthPerLevel),
                DamagePerLevel = ReadGrowth(reader, "Growth", "DamagePerLevel", defaults.DamagePerLevel),
                DefencePerLevel = ReadGrowth(reader, "Growth", "DefencePerLevel", defaults.DefencePerLevel),
                MagicDefencePerLevel = ReadGrowth(reader, "Growth", "MagicDefencePerLevel", defaults.MagicDefencePerLevel)
            };
        }

        private static void WritePetTame(InIReader reader, PetTameSettings value)
        {
            reader.Write("Shock", "ShockChanceBase", ClampPetSummonValue(value.ShockChanceBase, PetTameMaxChance));
            reader.Write("Shock", "ShockBaseSeconds", ClampPetSummonValue(value.ShockBaseSeconds, PetTameMaxSeconds));
            reader.Write("Shock", "ShockSecondsPerLevel", ClampPetSummonValue(value.ShockSecondsPerLevel, PetTameMaxSeconds));
            reader.Write("Shock", "RageChanceDenominator", ClampPetSummonValue(value.RageChanceDenominator, PetTameMaxChance));
            reader.Write("Shock", "RageBaseSeconds", ClampPetSummonValue(value.RageBaseSeconds, PetTameMaxSeconds));
            reader.Write("Shock", "RageRandomSeconds", ClampPetSummonValue(value.RageRandomSeconds, PetTameMaxSeconds));
            reader.Write("Tame", "MaxLevelDifference", ClampPetSummonValue(value.MaxLevelDifference, PetTameMaxLevelDifference));
            reader.Write("Tame", "TameChanceBase", ClampPetSummonValue(value.TameChanceBase, PetTameMaxChance));
            reader.Write("Tame", "TameChancePerMagicLevel", ClampPetSummonValue(value.TameChancePerMagicLevel, PetTameMaxChance));
            reader.Write("Tame", "TameChanceTargetOffset", ClampPetSummonValue(value.TameChanceTargetOffset, PetTameMaxChance));
            reader.Write("Tame", "PetLimitBase", ClampPetSummonValue(value.PetLimitBase, PetSummonMaxSummonLimit));
            reader.Write("Tame", "PetLimitPerMagicLevel", ClampPetSummonValue(value.PetLimitPerMagicLevel, PetSummonMaxSummonLimit));
            reader.Write("Growth", "HealthPerLevel", PetSummonSettings.FormatGrowth(value.HealthPerLevel));
            reader.Write("Growth", "DamagePerLevel", PetSummonSettings.FormatGrowth(value.DamagePerLevel));
            reader.Write("Growth", "DefencePerLevel", PetSummonSettings.FormatGrowth(value.DefencePerLevel));
            reader.Write("Growth", "MagicDefencePerLevel", PetSummonSettings.FormatGrowth(value.MagicDefencePerLevel));
        }
    }
}
