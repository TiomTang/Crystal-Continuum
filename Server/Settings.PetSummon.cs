using System.IO;
using Shared;

namespace Server
{
    public enum PetSummonDecision
    {
        Reject,
        Recall,
        Summon
    }

    /// <summary>
    /// Summon configuration of one Taoist servant skill: the monster it creates, what the cast
    /// costs and how its pet levels grow. The defaults are the constants the summon code used
    /// before they became configurable, so a missing entry keeps the original behaviour.
    /// </summary>
    public class PetSummonSettings
    {
        public const int MaxGrowthEntries = 255;
        public const int MaxGrowthValue = 10000;

        public string MonsterName = string.Empty;

        public int AmuletCost;
        public int SummonLimit;
        public int SummonDelay;

        /// <summary>
        /// Pet level limit of a cast at magic level L is MaxLevelBase + MaxLevelPerMagicLevel * L.
        /// The editor explains the formula, the byte ceiling is not a setting.
        /// </summary>
        public int MaxLevelBase;
        public int MaxLevelPerMagicLevel;

        /// <summary>
        /// Increment applied on each pet level. A one entry sequence is a constant increment, a
        /// longer one gives every level its own value and repeats the last entry above it.
        /// </summary>
        public int[] HealthPerLevel = { 0 };
        public int[] DamagePerLevel = { 0 };
        public int[] DefencePerLevel = { 0 };
        public int[] MagicDefencePerLevel = { 0 };

        public PetSummonSettings Copy()
        {
            return new PetSummonSettings
            {
                MonsterName = MonsterName,
                AmuletCost = AmuletCost,
                SummonLimit = SummonLimit,
                SummonDelay = SummonDelay,
                MaxLevelBase = MaxLevelBase,
                MaxLevelPerMagicLevel = MaxLevelPerMagicLevel,
                HealthPerLevel = (int[])HealthPerLevel.Clone(),
                DamagePerLevel = (int[])DamagePerLevel.Clone(),
                DefencePerLevel = (int[])DefencePerLevel.Clone(),
                MagicDefencePerLevel = (int[])MagicDefencePerLevel.Clone()
            };
        }

        public byte GetMaxPetLevel(int magicLevel)
        {
            long value = MaxLevelBase + (long)MaxLevelPerMagicLevel * magicLevel;

            if (value < 0) return 0;
            if (value > byte.MaxValue) return byte.MaxValue;

            return (byte)value;
        }

        public int HealthGrowth(int petLevel) => Growth(HealthPerLevel, petLevel);
        public int DamageGrowth(int petLevel) => Growth(DamagePerLevel, petLevel);
        public int DefenceGrowth(int petLevel) => Growth(DefencePerLevel, petLevel);
        public int MagicDefenceGrowth(int petLevel) => Growth(MagicDefencePerLevel, petLevel);

        public static int Growth(int[] sequence, int petLevel)
        {
            if (sequence == null || sequence.Length == 0 || petLevel <= 0) return 0;

            long total = 0;

            for (int i = 0; i < petLevel; i++)
                total += sequence[Math.Min(i, sequence.Length - 1)];

            if (total > int.MaxValue) return int.MaxValue;

            return (int)total;
        }

        public static string FormatGrowth(int[] sequence)
        {
            if (sequence == null || sequence.Length == 0) return "0";

            return string.Join(",", sequence);
        }

        /// <summary>
        /// What a summon cast does: add a servant, recall an existing one, or nothing. Reaching the
        /// skill limit recalls, so a limit of one keeps the original always-recall behaviour; above
        /// it the cast adds servants until the shared ceiling is reached.
        /// </summary>
        public static PetSummonDecision DecideSummon(int existing, PetSummonSettings summon, int totalPets, int maxActivePets)
        {
            if (summon == null) return PetSummonDecision.Reject;

            if (existing >= summon.SummonLimit)
                return existing > 0 ? PetSummonDecision.Recall : PetSummonDecision.Reject;

            if (totalPets >= maxActivePets) return PetSummonDecision.Reject;

            return PetSummonDecision.Summon;
        }

        /// <summary>
        /// Parses a comma separated increment sequence. Anything that is not a list of values in
        /// 0..MaxGrowthValue is rejected, so the editor can refuse to save instead of silently
        /// falling back to the default.
        /// </summary>
        public static bool TryParseGrowth(string value, out int[] sequence)
        {
            sequence = null;

            if (string.IsNullOrWhiteSpace(value)) return false;

            var result = new List<int>();

            foreach (string part in value.Split(','))
            {
                if (!int.TryParse(part.Trim(), out int number)) return false;
                if (number < 0 || number > MaxGrowthValue) return false;

                result.Add(number);
            }

            if (result.Count == 0 || result.Count > MaxGrowthEntries) return false;

            sequence = result.ToArray();
            return true;
        }
    }

    /// <summary>
    /// Rules shared by the three Taoist summons: how many monster pets a player may hold at once
    /// and how pet experience turns into pet levels.
    /// </summary>
    public class PetSummonSharedSettings
    {
        /// <summary>
        /// Ceiling for every monster pet of one player. The per skill limits apply below it.
        /// </summary>
        public int MaxActivePets;

        public int ExperienceMultiplier;
        public int ExperienceBase;
        public int ExperiencePerLevel;

        public int MoveSpeedPerLevel;
        public int AttackSpeedPerLevel;

        public int MinimumMoveSpeed;
        public int MinimumAttackSpeed;

        public PetSummonSharedSettings Copy()
        {
            return new PetSummonSharedSettings
            {
                MaxActivePets = MaxActivePets,
                ExperienceMultiplier = ExperienceMultiplier,
                ExperienceBase = ExperienceBase,
                ExperiencePerLevel = ExperiencePerLevel,
                MoveSpeedPerLevel = MoveSpeedPerLevel,
                AttackSpeedPerLevel = AttackSpeedPerLevel,
                MinimumMoveSpeed = MinimumMoveSpeed,
                MinimumAttackSpeed = MinimumAttackSpeed
            };
        }

        /// <summary>Experience a pet needs to advance from the given level.</summary>
        public uint GetExperienceRequired(int petLevel)
        {
            long value = ExperienceBase + (long)ExperiencePerLevel * petLevel;

            if (value < 1) return 1;
            if (value > uint.MaxValue) return uint.MaxValue;

            return (uint)value;
        }
    }

    public static partial class Settings
    {
        public const int PetSummonMaxAmuletCost = byte.MaxValue;
        public const int PetSummonMaxSummonLimit = 1000;
        public const int PetSummonMaxSummonDelay = 60000;
        public const int PetSummonMaxExperience = 100000000;
        public const int PetSummonMaxSpeed = ushort.MaxValue;

        public static PetSummonSettings PetSummonSkeleton = new PetSummonSettings
        {
            MonsterName = "BoneFamiliar",
            AmuletCost = 1,
            SummonLimit = 1,
            SummonDelay = 500,
            MaxLevelBase = 4,
            MaxLevelPerMagicLevel = 1,
            HealthPerLevel = new[] { 20 },
            DamagePerLevel = new[] { 1 },
            DefencePerLevel = new[] { 2 },
            MagicDefencePerLevel = new[] { 2 }
        };

        public static PetSummonSettings PetSummonShinsu = new PetSummonSettings
        {
            MonsterName = "Shinsu",
            AmuletCost = 5,
            SummonLimit = 1,
            SummonDelay = 500,
            MaxLevelBase = 1,
            MaxLevelPerMagicLevel = 2,
            HealthPerLevel = new[] { 20 },
            DamagePerLevel = new[] { 1 },
            DefencePerLevel = new[] { 2 },
            MagicDefencePerLevel = new[] { 2 }
        };

        public static PetSummonSettings PetSummonHolyDeva = new PetSummonSettings
        {
            MonsterName = "HolyDeva",
            AmuletCost = 2,
            SummonLimit = 1,
            SummonDelay = 1500,
            MaxLevelBase = 1,
            MaxLevelPerMagicLevel = 2,
            HealthPerLevel = new[] { 20 },
            DamagePerLevel = new[] { 1 },
            DefencePerLevel = new[] { 2 },
            MagicDefencePerLevel = new[] { 2 }
        };

        public static PetSummonSharedSettings PetSummonShared = new PetSummonSharedSettings
        {
            MaxActivePets = 2,
            ExperienceMultiplier = 3,
            ExperienceBase = 20000,
            ExperiencePerLevel = 20000,
            MoveSpeedPerLevel = 130,
            AttackSpeedPerLevel = 70,
            MinimumMoveSpeed = 400,
            MinimumAttackSpeed = 400
        };

        public static string PetSummonPath => Path.Combine(ConfigPath, "PetSummon.ini");

        public static void LoadPetSummon()
        {
            // The monster names and the Taoist servant inheritance moved out of Setup.ini and
            // PetInheritance.ini. The first load adopts the old values, so a customised setup is
            // not silently reset to the defaults.
            if (!File.Exists(PetSummonPath)) AdoptLegacyPetSummon();

            var reader = new InIReader(PetSummonPath);

            PetSummonSkeleton = ReadPetSummon(reader, "Skeleton", PetSummonSkeleton);
            PetSummonShinsu = ReadPetSummon(reader, "Shinsu", PetSummonShinsu);
            PetSummonHolyDeva = ReadPetSummon(reader, "HolyDeva", PetSummonHolyDeva);
            PetSummonShared = ReadPetSummonShared(reader, "Shared", PetSummonShared);
            TaoistPetInheritance = ReadPetInheritance(reader, "Inheritance", Stat.MaxSC, TaoistPetInheritance);

            ApplyTaoistSummonMonsterNames();
        }

        public static void SavePetSummon()
        {
            var reader = new InIReader(PetSummonPath);

            WritePetSummon(reader, "Skeleton", PetSummonSkeleton);
            WritePetSummon(reader, "Shinsu", PetSummonShinsu);
            WritePetSummon(reader, "HolyDeva", PetSummonHolyDeva);
            WritePetSummonShared(reader, "Shared", PetSummonShared);
            WritePetInheritance(reader, "Inheritance", TaoistPetInheritance);

            ApplyTaoistSummonMonsterNames();
        }

        /// <summary>
        /// The summon configuration owns the Taoist monster names, the rest of the server keeps
        /// reading them from the Settings fields.
        /// </summary>
        private static void ApplyTaoistSummonMonsterNames()
        {
            SkeletonName = PetSummonSkeleton.MonsterName;
            ShinsuName = PetSummonShinsu.MonsterName;
            AngelName = PetSummonHolyDeva.MonsterName;
        }

        /// <summary>Reads the values from the files this configuration was split out of.</summary>
        private static void AdoptLegacyPetSummon()
        {
            AdoptLegacyMonsterNames();
            AdoptLegacyTaoistInheritance();
        }

        /// <summary>Reads the monster names from Setup.ini once, without writing that file back.</summary>
        private static void AdoptLegacyMonsterNames()
        {
            var legacy = new InIReader(Path.Combine(ConfigPath, "Setup.ini"));

            PetSummonSkeleton.MonsterName = legacy.ReadString("Game", "SkeletonName", PetSummonSkeleton.MonsterName, false);
            PetSummonShinsu.MonsterName = legacy.ReadString("Game", "ShinsuName", PetSummonShinsu.MonsterName, false);
            PetSummonHolyDeva.MonsterName = legacy.ReadString("Game", "AngelName", PetSummonHolyDeva.MonsterName, false);
        }

        /// <summary>Reads the Taoist section of PetInheritance.ini once, without writing it back.</summary>
        private static void AdoptLegacyTaoistInheritance()
        {
            if (!File.Exists(PetInheritancePath)) return;

            TaoistPetInheritance = ReadPetInheritance(new InIReader(PetInheritancePath), "Taoist", Stat.MaxSC, TaoistPetInheritance, false);
        }

        /// <summary>Summon configuration a pet belongs to, resolved by its monster name.</summary>
        public static PetSummonSettings FindTaoistSummon(string monsterName)
        {
            if (string.IsNullOrEmpty(monsterName)) return null;

            if (string.Equals(PetSummonSkeleton.MonsterName, monsterName, StringComparison.Ordinal)) return PetSummonSkeleton;
            if (string.Equals(PetSummonShinsu.MonsterName, monsterName, StringComparison.Ordinal)) return PetSummonShinsu;
            if (string.Equals(PetSummonHolyDeva.MonsterName, monsterName, StringComparison.Ordinal)) return PetSummonHolyDeva;

            return null;
        }

        private static PetSummonSettings ReadPetSummon(InIReader reader, string section, PetSummonSettings defaults)
        {
            return new PetSummonSettings
            {
                MonsterName = reader.ReadString(section, "MonsterName", defaults.MonsterName),
                AmuletCost = ClampPetSummonValue(reader.ReadInt32(section, "AmuletCost", defaults.AmuletCost), PetSummonMaxAmuletCost),
                SummonLimit = ClampPetSummonValue(reader.ReadInt32(section, "SummonLimit", defaults.SummonLimit), PetSummonMaxSummonLimit),
                SummonDelay = ClampPetSummonValue(reader.ReadInt32(section, "SummonDelay", defaults.SummonDelay), PetSummonMaxSummonDelay),
                MaxLevelBase = ClampPetSummonValue(reader.ReadInt32(section, "MaxLevelBase", defaults.MaxLevelBase), byte.MaxValue),
                MaxLevelPerMagicLevel = ClampPetSummonValue(reader.ReadInt32(section, "MaxLevelPerMagicLevel", defaults.MaxLevelPerMagicLevel), byte.MaxValue),
                HealthPerLevel = ReadGrowth(reader, section, "HealthPerLevel", defaults.HealthPerLevel),
                DamagePerLevel = ReadGrowth(reader, section, "DamagePerLevel", defaults.DamagePerLevel),
                DefencePerLevel = ReadGrowth(reader, section, "DefencePerLevel", defaults.DefencePerLevel),
                MagicDefencePerLevel = ReadGrowth(reader, section, "MagicDefencePerLevel", defaults.MagicDefencePerLevel)
            };
        }

        private static PetSummonSharedSettings ReadPetSummonShared(InIReader reader, string section, PetSummonSharedSettings defaults)
        {
            return new PetSummonSharedSettings
            {
                MaxActivePets = ClampPetSummonValue(reader.ReadInt32(section, "MaxActivePets", defaults.MaxActivePets), PetSummonMaxSummonLimit),
                ExperienceMultiplier = ClampPetSummonValue(reader.ReadInt32(section, "ExperienceMultiplier", defaults.ExperienceMultiplier), PetSummonMaxAmuletCost),
                ExperienceBase = ClampPetSummonValue(reader.ReadInt32(section, "ExperienceBase", defaults.ExperienceBase), PetSummonMaxExperience),
                ExperiencePerLevel = ClampPetSummonValue(reader.ReadInt32(section, "ExperiencePerLevel", defaults.ExperiencePerLevel), PetSummonMaxExperience),
                MoveSpeedPerLevel = ClampPetSummonValue(reader.ReadInt32(section, "MoveSpeedPerLevel", defaults.MoveSpeedPerLevel), PetSummonMaxSpeed),
                AttackSpeedPerLevel = ClampPetSummonValue(reader.ReadInt32(section, "AttackSpeedPerLevel", defaults.AttackSpeedPerLevel), PetSummonMaxSpeed),
                MinimumMoveSpeed = ClampPetSummonValue(reader.ReadInt32(section, "MinimumMoveSpeed", defaults.MinimumMoveSpeed), PetSummonMaxSpeed),
                MinimumAttackSpeed = ClampPetSummonValue(reader.ReadInt32(section, "MinimumAttackSpeed", defaults.MinimumAttackSpeed), PetSummonMaxSpeed)
            };
        }

        private static int[] ReadGrowth(InIReader reader, string section, string key, int[] defaults)
        {
            string value = reader.ReadString(section, key, PetSummonSettings.FormatGrowth(defaults));

            return PetSummonSettings.TryParseGrowth(value, out int[] sequence) ? sequence : defaults;
        }

        private static void WritePetSummon(InIReader reader, string section, PetSummonSettings value)
        {
            reader.Write(section, "MonsterName", value.MonsterName ?? string.Empty);
            reader.Write(section, "AmuletCost", ClampPetSummonValue(value.AmuletCost, PetSummonMaxAmuletCost));
            reader.Write(section, "SummonLimit", ClampPetSummonValue(value.SummonLimit, PetSummonMaxSummonLimit));
            reader.Write(section, "SummonDelay", ClampPetSummonValue(value.SummonDelay, PetSummonMaxSummonDelay));
            reader.Write(section, "MaxLevelBase", ClampPetSummonValue(value.MaxLevelBase, byte.MaxValue));
            reader.Write(section, "MaxLevelPerMagicLevel", ClampPetSummonValue(value.MaxLevelPerMagicLevel, byte.MaxValue));
            reader.Write(section, "HealthPerLevel", PetSummonSettings.FormatGrowth(value.HealthPerLevel));
            reader.Write(section, "DamagePerLevel", PetSummonSettings.FormatGrowth(value.DamagePerLevel));
            reader.Write(section, "DefencePerLevel", PetSummonSettings.FormatGrowth(value.DefencePerLevel));
            reader.Write(section, "MagicDefencePerLevel", PetSummonSettings.FormatGrowth(value.MagicDefencePerLevel));
        }

        private static void WritePetSummonShared(InIReader reader, string section, PetSummonSharedSettings value)
        {
            reader.Write(section, "MaxActivePets", ClampPetSummonValue(value.MaxActivePets, PetSummonMaxSummonLimit));
            reader.Write(section, "ExperienceMultiplier", ClampPetSummonValue(value.ExperienceMultiplier, PetSummonMaxAmuletCost));
            reader.Write(section, "ExperienceBase", ClampPetSummonValue(value.ExperienceBase, PetSummonMaxExperience));
            reader.Write(section, "ExperiencePerLevel", ClampPetSummonValue(value.ExperiencePerLevel, PetSummonMaxExperience));
            reader.Write(section, "MoveSpeedPerLevel", ClampPetSummonValue(value.MoveSpeedPerLevel, PetSummonMaxSpeed));
            reader.Write(section, "AttackSpeedPerLevel", ClampPetSummonValue(value.AttackSpeedPerLevel, PetSummonMaxSpeed));
            reader.Write(section, "MinimumMoveSpeed", ClampPetSummonValue(value.MinimumMoveSpeed, PetSummonMaxSpeed));
            reader.Write(section, "MinimumAttackSpeed", ClampPetSummonValue(value.MinimumAttackSpeed, PetSummonMaxSpeed));
        }

        private static int ClampPetSummonValue(int value, int maximum)
        {
            if (value < 0) return 0;
            if (value > maximum) return maximum;

            return value;
        }
    }
}
