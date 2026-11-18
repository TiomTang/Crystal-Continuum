using Server.MirEnvir;
using Shared;

namespace Server
{
    /// <summary>
    /// Editor for the wizard pet inheritance in Configs/PetInheritance.ini. The Taoist servants
    /// are configured in the summon window, their values live in Configs/PetSummon.ini.
    /// </summary>
    public partial class PetInheritanceForm : Form
    {
        public Envir Envir => SMain.Envir;

        public PetInheritanceForm()
        {
            InitializeComponent();
            UiText.Apply(this);

            ApplyLimits();

            LoadSettings();
        }

        private void ApplyLimits()
        {
            List<NumericUpDown> boxes = new List<NumericUpDown>
            {
                nudWizardMinDamage, nudWizardMaxDamage,
                nudWizardMinDefence, nudWizardMaxDefence,
                nudWizardMinMagicDefence, nudWizardMaxMagicDefence,
                nudWizardHealth
            };

            foreach (NumericUpDown box in boxes)
            {
                box.Minimum = 0;
                box.Maximum = Settings.PetInheritanceMaxPercent;
            }

            nudWizardHealth.Maximum = Settings.PetInheritanceMaxHealthPerPoint;
        }

        private void LoadSettings()
        {
            PetInheritanceSettings wizard = Settings.WizardPetInheritance ?? new PetInheritanceSettings { OwnerStat = Stat.MaxMC };

            chkWizardEnabled.Checked = wizard.Enabled;
            nudWizardMinDamage.Value = Clamp(wizard.MinDamagePercent, nudWizardMinDamage.Maximum);
            nudWizardMaxDamage.Value = Clamp(wizard.MaxDamagePercent, nudWizardMaxDamage.Maximum);
            nudWizardMinDefence.Value = Clamp(wizard.MinDefencePercent, nudWizardMinDefence.Maximum);
            nudWizardMaxDefence.Value = Clamp(wizard.MaxDefencePercent, nudWizardMaxDefence.Maximum);
            nudWizardMinMagicDefence.Value = Clamp(wizard.MinMagicDefencePercent, nudWizardMinMagicDefence.Maximum);
            nudWizardMaxMagicDefence.Value = Clamp(wizard.MaxMagicDefencePercent, nudWizardMaxMagicDefence.Maximum);
            nudWizardHealth.Value = Clamp(wizard.HealthPerPoint, nudWizardHealth.Maximum);
        }

        private static decimal Clamp(int value, decimal maximum)
        {
            if (value < 0) return 0;
            if (value > maximum) return maximum;

            return value;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Settings.WizardPetInheritance = new PetInheritanceSettings
            {
                OwnerStat = Stat.MaxMC,
                Enabled = chkWizardEnabled.Checked,
                MinDamagePercent = (int)nudWizardMinDamage.Value,
                MaxDamagePercent = (int)nudWizardMaxDamage.Value,
                MinDefencePercent = (int)nudWizardMinDefence.Value,
                MaxDefencePercent = (int)nudWizardMaxDefence.Value,
                MinMagicDefencePercent = (int)nudWizardMinMagicDefence.Value,
                MaxMagicDefencePercent = (int)nudWizardMaxMagicDefence.Value,
                HealthPerPoint = (int)nudWizardHealth.Value
            };

            Settings.SavePetInheritance();

            // The world loop rebuilds the servant stats, so the editor never edits live objects.
            Envir?.RequiresPetInheritanceUpdate();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
