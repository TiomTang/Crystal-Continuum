using Server.MirEnvir;
using Shared;

namespace Server
{
    /// <summary>
    /// Editor for the wizard temptation spell in Configs/PetTame.ini.
    /// <para>
    /// The shock and rage timers, the tame roll, the pet limit, the pet stat growth and the
    /// inherited attribute block were all hardcoded in HumanObject.ElectricShock and
    /// MonsterObject.RefreshAll. The Taoist servants keep their own values in the summon window,
    /// Configs/PetSummon.ini.
    /// </para>
    /// </summary>
    public partial class PetTameForm : Form
    {
        public PetTameForm()
        {
            InitializeComponent();
            UiText.Apply(this);

            ApplyLimits();
            LoadSettings();

            foreach (NumericUpDown box in NumericBoxes())
            {
                box.ValueChanged += EditorChanged;
            }

            foreach (TextBox box in new[] { txtHealth, txtDamage, txtDefence, txtMagicDefence })
            {
                box.TextChanged += EditorChanged;
            }

            UpdatePreviews();
        }

        private NumericUpDown[] NumericBoxes()
        {
            return new[]
            {
                nudShockChanceBase, nudShockBaseSeconds, nudShockSecondsPerLevel,
                nudRageChanceDenominator, nudRageBaseSeconds, nudRageRandomSeconds,
                nudMaxLevelDifference, nudTameChanceBase, nudTameChancePerMagicLevel, nudTameChanceTargetOffset,
                nudPetLimitBase, nudPetLimitPerMagicLevel
            };
        }

        private void ApplyLimits()
        {
            SetRange(nudShockChanceBase, 0, Settings.PetTameMaxChance);
            SetRange(nudShockBaseSeconds, 0, Settings.PetTameMaxSeconds);
            SetRange(nudShockSecondsPerLevel, 0, Settings.PetTameMaxSeconds);
            SetRange(nudRageChanceDenominator, 1, Settings.PetTameMaxChance);
            SetRange(nudRageBaseSeconds, 0, Settings.PetTameMaxSeconds);
            SetRange(nudRageRandomSeconds, 0, Settings.PetTameMaxSeconds);
            SetRange(nudMaxLevelDifference, 0, Settings.PetTameMaxLevelDifference);
            SetRange(nudTameChanceBase, 0, Settings.PetTameMaxChance);
            SetRange(nudTameChancePerMagicLevel, 0, Settings.PetTameMaxChance);
            SetRange(nudTameChanceTargetOffset, 0, Settings.PetTameMaxChance);
            SetRange(nudPetLimitBase, 0, Settings.PetSummonMaxSummonLimit);
            SetRange(nudPetLimitPerMagicLevel, 0, Settings.PetSummonMaxSummonLimit);

            foreach (NumericUpDown box in new[] { nudInheritMinDamage, nudInheritMaxDamage, nudInheritMinDefence, nudInheritMaxDefence, nudInheritMinMagicDefence, nudInheritMaxMagicDefence })
            {
                SetRange(box, 0, Settings.PetInheritanceMaxPercent);
            }

            SetRange(nudInheritHealth, 0, Settings.PetInheritanceMaxHealthPerPoint);
        }

        private static void SetRange(NumericUpDown box, int minimum, int maximum)
        {
            box.Minimum = minimum;
            box.Maximum = maximum;
        }

        private void LoadSettings()
        {
            PetTameSettings tame = Settings.PetTame ?? new PetTameSettings();

            nudShockChanceBase.Value = Clamp(tame.ShockChanceBase, nudShockChanceBase.Maximum);
            nudShockBaseSeconds.Value = Clamp(tame.ShockBaseSeconds, nudShockBaseSeconds.Maximum);
            nudShockSecondsPerLevel.Value = Clamp(tame.ShockSecondsPerLevel, nudShockSecondsPerLevel.Maximum);
            nudRageChanceDenominator.Value = Clamp(tame.RageChanceDenominator, nudRageChanceDenominator.Maximum);
            nudRageBaseSeconds.Value = Clamp(tame.RageBaseSeconds, nudRageBaseSeconds.Maximum);
            nudRageRandomSeconds.Value = Clamp(tame.RageRandomSeconds, nudRageRandomSeconds.Maximum);

            nudMaxLevelDifference.Value = Clamp(tame.MaxLevelDifference, nudMaxLevelDifference.Maximum);
            nudTameChanceBase.Value = Clamp(tame.TameChanceBase, nudTameChanceBase.Maximum);
            nudTameChancePerMagicLevel.Value = Clamp(tame.TameChancePerMagicLevel, nudTameChancePerMagicLevel.Maximum);
            nudTameChanceTargetOffset.Value = Clamp(tame.TameChanceTargetOffset, nudTameChanceTargetOffset.Maximum);
            nudPetLimitBase.Value = Clamp(tame.PetLimitBase, nudPetLimitBase.Maximum);
            nudPetLimitPerMagicLevel.Value = Clamp(tame.PetLimitPerMagicLevel, nudPetLimitPerMagicLevel.Maximum);

            txtHealth.Text = PetSummonSettings.FormatGrowth(tame.HealthPerLevel);
            txtDamage.Text = PetSummonSettings.FormatGrowth(tame.DamagePerLevel);
            txtDefence.Text = PetSummonSettings.FormatGrowth(tame.DefencePerLevel);
            txtMagicDefence.Text = PetSummonSettings.FormatGrowth(tame.MagicDefencePerLevel);

            PetInheritanceSettings inheritance = Settings.WizardPetInheritance ?? new PetInheritanceSettings { OwnerStat = Stat.MaxMC };

            chkInheritEnabled.Checked = inheritance.Enabled;
            nudInheritMinDamage.Value = Clamp(inheritance.MinDamagePercent, nudInheritMinDamage.Maximum);
            nudInheritMaxDamage.Value = Clamp(inheritance.MaxDamagePercent, nudInheritMaxDamage.Maximum);
            nudInheritMinDefence.Value = Clamp(inheritance.MinDefencePercent, nudInheritMinDefence.Maximum);
            nudInheritMaxDefence.Value = Clamp(inheritance.MaxDefencePercent, nudInheritMaxDefence.Maximum);
            nudInheritMinMagicDefence.Value = Clamp(inheritance.MinMagicDefencePercent, nudInheritMinMagicDefence.Maximum);
            nudInheritMaxMagicDefence.Value = Clamp(inheritance.MaxMagicDefencePercent, nudInheritMaxMagicDefence.Maximum);
            nudInheritHealth.Value = Clamp(inheritance.HealthPerPoint, nudInheritHealth.Maximum);
        }

        private void EditorChanged(object sender, EventArgs e)
        {
            UpdatePreviews();
        }

        /// <summary>
        /// Shows what the edited values mean at spell level 3 and marks growth fields that would be
        /// rejected on save.
        /// </summary>
        private void UpdatePreviews()
        {
            long limit = (long)nudPetLimitBase.Value + (long)nudPetLimitPerMagicLevel.Value * 3;
            if (limit > Globals.MaxPets) limit = Globals.MaxPets;

            long shock = (long)nudShockBaseSeconds.Value + (long)nudShockSecondsPerLevel.Value * 3;
            long roll = (long)nudTameChanceBase.Value + (long)nudTameChancePerMagicLevel.Value * 3;

            lblTamePreview.Text = GameLanguage.Get(
                "Spell level 3: up to {0} pets, shock lasts {1} seconds, the tame roll is Next(caster level + {2}) and must exceed target level + {3}.",
                limit, shock, roll, (int)nudTameChanceTargetOffset.Value);

            ValidateGrowth(txtHealth);
            ValidateGrowth(txtDamage);
            ValidateGrowth(txtDefence);
            ValidateGrowth(txtMagicDefence);
        }

        private static bool ValidateGrowth(TextBox box)
        {
            bool valid = PetSummonSettings.TryParseGrowth(box.Text, out _);

            box.BackColor = valid ? SystemColors.Window : Color.MistyRose;

            return valid;
        }

        private static decimal Clamp(int value, decimal maximum)
        {
            if (value < 0) return 0;
            if (value > maximum) return maximum;

            return value;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            bool valid = true;

            foreach (TextBox box in new[] { txtHealth, txtDamage, txtDefence, txtMagicDefence })
            {
                valid &= ValidateGrowth(box);
            }

            if (!valid)
            {
                lblStatus.Text = GameLanguage.Get("Fix the red fields before saving.");
                return;
            }

            PetSummonSettings.TryParseGrowth(txtHealth.Text, out int[] health);
            PetSummonSettings.TryParseGrowth(txtDamage.Text, out int[] damage);
            PetSummonSettings.TryParseGrowth(txtDefence.Text, out int[] defence);
            PetSummonSettings.TryParseGrowth(txtMagicDefence.Text, out int[] magicDefence);

            Settings.PetTame = new PetTameSettings
            {
                ShockChanceBase = (int)nudShockChanceBase.Value,
                ShockBaseSeconds = (int)nudShockBaseSeconds.Value,
                ShockSecondsPerLevel = (int)nudShockSecondsPerLevel.Value,
                RageChanceDenominator = (int)nudRageChanceDenominator.Value,
                RageBaseSeconds = (int)nudRageBaseSeconds.Value,
                RageRandomSeconds = (int)nudRageRandomSeconds.Value,
                MaxLevelDifference = (int)nudMaxLevelDifference.Value,
                TameChanceBase = (int)nudTameChanceBase.Value,
                TameChancePerMagicLevel = (int)nudTameChancePerMagicLevel.Value,
                TameChanceTargetOffset = (int)nudTameChanceTargetOffset.Value,
                PetLimitBase = (int)nudPetLimitBase.Value,
                PetLimitPerMagicLevel = (int)nudPetLimitPerMagicLevel.Value,
                HealthPerLevel = health,
                DamagePerLevel = damage,
                DefencePerLevel = defence,
                MagicDefencePerLevel = magicDefence
            };

            Settings.WizardPetInheritance = new PetInheritanceSettings
            {
                OwnerStat = Stat.MaxMC,
                Enabled = chkInheritEnabled.Checked,
                MinDamagePercent = (int)nudInheritMinDamage.Value,
                MaxDamagePercent = (int)nudInheritMaxDamage.Value,
                MinDefencePercent = (int)nudInheritMinDefence.Value,
                MaxDefencePercent = (int)nudInheritMaxDefence.Value,
                MinMagicDefencePercent = (int)nudInheritMinMagicDefence.Value,
                MaxMagicDefencePercent = (int)nudInheritMaxMagicDefence.Value,
                HealthPerPoint = (int)nudInheritHealth.Value
            };

            Settings.SavePetTame();

            // The world loop rebuilds the pet stats, so the editor never touches live objects.
            SMain.Envir?.RequiresPetInheritanceUpdate();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
