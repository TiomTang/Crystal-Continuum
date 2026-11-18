using System.IO;
using Shared;

namespace Server
{
    /// <summary>
    /// Inheritance configuration for one pet group. The owner attribute is fixed by the group, the
    /// remaining values are the parameters an administrator can edit: the Taoist servants in the
    /// summon window, the wizard pets in the inheritance window.
    /// </summary>
    public class PetInheritanceSettings
    {
        public Stat OwnerStat = Stat.MaxSC;

        public bool Enabled;

        public int MinDamagePercent;
        public int MaxDamagePercent;

        public int MinDefencePercent;
        public int MaxDefencePercent;

        public int MinMagicDefencePercent;
        public int MaxMagicDefencePercent;

        public int HealthPerPoint;

        public PetInheritanceSettings Copy()
        {
            return new PetInheritanceSettings
            {
                OwnerStat = OwnerStat,
                Enabled = Enabled,
                MinDamagePercent = MinDamagePercent,
                MaxDamagePercent = MaxDamagePercent,
                MinDefencePercent = MinDefencePercent,
                MaxDefencePercent = MaxDefencePercent,
                MinMagicDefencePercent = MinMagicDefencePercent,
                MaxMagicDefencePercent = MaxMagicDefencePercent,
                HealthPerPoint = HealthPerPoint
            };
        }
    }

    public static partial class Settings
    {
        public const int PetInheritanceMaxPercent = 1000;
        public const int PetInheritanceMaxHealthPerPoint = 1000;

        public static PetInheritanceSettings TaoistPetInheritance = new PetInheritanceSettings
        {
            OwnerStat = Stat.MaxSC,
            Enabled = false,
            MinDamagePercent = 20,
            MaxDamagePercent = 40,
            MinDefencePercent = 15,
            MaxDefencePercent = 30,
            MinMagicDefencePercent = 15,
            MaxMagicDefencePercent = 30,
            HealthPerPoint = 0
        };

        public static PetInheritanceSettings WizardPetInheritance = new PetInheritanceSettings
        {
            OwnerStat = Stat.MaxMC,
            Enabled = false,
            MinDamagePercent = 0,
            MaxDamagePercent = 0,
            MinDefencePercent = 0,
            MaxDefencePercent = 0,
            MinMagicDefencePercent = 0,
            MaxMagicDefencePercent = 0,
            HealthPerPoint = 0
        };

        public static string PetInheritancePath => Path.Combine(ConfigPath, "PetInheritance.ini");

        public static void LoadPetInheritance()
        {
            var reader = new InIReader(PetInheritancePath);

            // The Taoist servants are configured in PetSummon.ini; only the wizard pets are left.
            WizardPetInheritance = ReadPetInheritance(reader, "Wizard", Stat.MaxMC, WizardPetInheritance);
        }

        public static void SavePetInheritance()
        {
            var reader = new InIReader(PetInheritancePath);

            WritePetInheritance(reader, "Wizard", WizardPetInheritance);
        }

        private static PetInheritanceSettings ReadPetInheritance(InIReader reader, string section, Stat ownerStat, PetInheritanceSettings defaults, bool writeWhenNull = true)
        {
            return new PetInheritanceSettings
            {
                OwnerStat = ownerStat,
                Enabled = reader.ReadBoolean(section, "Enabled", defaults.Enabled, writeWhenNull),
                MinDamagePercent = ClampPetInheritanceValue(reader.ReadInt32(section, "MinDamagePercent", defaults.MinDamagePercent, writeWhenNull), PetInheritanceMaxPercent),
                MaxDamagePercent = ClampPetInheritanceValue(reader.ReadInt32(section, "MaxDamagePercent", defaults.MaxDamagePercent, writeWhenNull), PetInheritanceMaxPercent),
                MinDefencePercent = ClampPetInheritanceValue(reader.ReadInt32(section, "MinDefencePercent", defaults.MinDefencePercent, writeWhenNull), PetInheritanceMaxPercent),
                MaxDefencePercent = ClampPetInheritanceValue(reader.ReadInt32(section, "MaxDefencePercent", defaults.MaxDefencePercent, writeWhenNull), PetInheritanceMaxPercent),
                MinMagicDefencePercent = ClampPetInheritanceValue(reader.ReadInt32(section, "MinMagicDefencePercent", defaults.MinMagicDefencePercent, writeWhenNull), PetInheritanceMaxPercent),
                MaxMagicDefencePercent = ClampPetInheritanceValue(reader.ReadInt32(section, "MaxMagicDefencePercent", defaults.MaxMagicDefencePercent, writeWhenNull), PetInheritanceMaxPercent),
                HealthPerPoint = ClampPetInheritanceValue(reader.ReadInt32(section, "HealthPerPoint", defaults.HealthPerPoint, writeWhenNull), PetInheritanceMaxHealthPerPoint)
            };
        }

        private static void WritePetInheritance(InIReader reader, string section, PetInheritanceSettings value)
        {
            reader.Write(section, "Enabled", value.Enabled);
            reader.Write(section, "MinDamagePercent", ClampPetInheritanceValue(value.MinDamagePercent, PetInheritanceMaxPercent));
            reader.Write(section, "MaxDamagePercent", ClampPetInheritanceValue(value.MaxDamagePercent, PetInheritanceMaxPercent));
            reader.Write(section, "MinDefencePercent", ClampPetInheritanceValue(value.MinDefencePercent, PetInheritanceMaxPercent));
            reader.Write(section, "MaxDefencePercent", ClampPetInheritanceValue(value.MaxDefencePercent, PetInheritanceMaxPercent));
            reader.Write(section, "MinMagicDefencePercent", ClampPetInheritanceValue(value.MinMagicDefencePercent, PetInheritanceMaxPercent));
            reader.Write(section, "MaxMagicDefencePercent", ClampPetInheritanceValue(value.MaxMagicDefencePercent, PetInheritanceMaxPercent));
            reader.Write(section, "HealthPerPoint", ClampPetInheritanceValue(value.HealthPerPoint, PetInheritanceMaxHealthPerPoint));
        }

        private static int ClampPetInheritanceValue(int value, int maximum)
        {
            if (value < 0) return 0;
            if (value > maximum) return maximum;

            return value;
        }
    }
}
