using Server.MirDatabase;
using Server.MirEnvir;
using Shared;

namespace Server
{
    /// <summary>
    /// Editor for the Taoist summon configuration in Configs/PetSummon.ini.
    /// <para>
    /// Everything the summon code used to hardcode is editable here: the monster template, the
    /// amulet cost, the per skill pet limit and the pet level growth. A monster name is resolved
    /// against the loaded records while typing, because a name that matches nothing makes the
    /// skill do nothing without any log line.
    /// </para>
    /// </summary>
    public partial class PetSummonForm : Form
    {
        private readonly List<MonsterInfo> _monsters;
        private readonly SkillEditor[] _editors;

        public PetSummonForm()
        {
            InitializeComponent();
            UiText.Apply(this);

            _editors = new[]
            {
                CreateEditor(cbSkeletonMonster, lblSkeletonMonsterStatus, nudSkeletonAmulet, nudSkeletonLimit, nudSkeletonDelay,
                    nudSkeletonLevelBase, nudSkeletonLevelStep, txtSkeletonHealth, txtSkeletonDamage, txtSkeletonDefence,
                    txtSkeletonMagicDefence, lblSkeletonPreview),
                CreateEditor(cbShinsuMonster, lblShinsuMonsterStatus, nudShinsuAmulet, nudShinsuLimit, nudShinsuDelay,
                    nudShinsuLevelBase, nudShinsuLevelStep, txtShinsuHealth, txtShinsuDamage, txtShinsuDefence,
                    txtShinsuMagicDefence, lblShinsuPreview),
                CreateEditor(cbHolyDevaMonster, lblHolyDevaMonsterStatus, nudHolyDevaAmulet, nudHolyDevaLimit, nudHolyDevaDelay,
                    nudHolyDevaLevelBase, nudHolyDevaLevelStep, txtHolyDevaHealth, txtHolyDevaDamage, txtHolyDevaDefence,
                    txtHolyDevaMagicDefence, lblHolyDevaPreview)
            };

            _monsters = (SMain.EditEnvir?.MonsterInfoList ?? Enumerable.Empty<MonsterInfo>())
                .Where(x => x != null)
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string[] names = _monsters.Select(x => x.Name).ToArray();

            ApplyLimits();

            foreach (SkillEditor editor in _editors) editor.Monster.Items.AddRange(names);

            LoadSettings();

            foreach (SkillEditor editor in _editors)
            {
                editor.Monster.TextChanged += EditorChanged;
                editor.Monster.SelectedIndexChanged += EditorChanged;
                editor.Health.TextChanged += EditorChanged;
                editor.Damage.TextChanged += EditorChanged;
                editor.Defence.TextChanged += EditorChanged;
                editor.MagicDefence.TextChanged += EditorChanged;
            }
        }

        private sealed class SkillEditor
        {
            internal ComboBox Monster;
            internal Label MonsterStatus;
            internal NumericUpDown Amulet;
            internal NumericUpDown Limit;
            internal NumericUpDown Delay;
            internal NumericUpDown LevelBase;
            internal NumericUpDown LevelStep;
            internal TextBox Health;
            internal TextBox Damage;
            internal TextBox Defence;
            internal TextBox MagicDefence;
            internal Label Preview;
        }

        private static SkillEditor CreateEditor(ComboBox monster, Label monsterStatus, NumericUpDown amulet,
            NumericUpDown limit, NumericUpDown delay, NumericUpDown levelBase, NumericUpDown levelStep,
            TextBox health, TextBox damage, TextBox defence, TextBox magicDefence, Label preview)
        {
            return new SkillEditor
            {
                Monster = monster,
                MonsterStatus = monsterStatus,
                Amulet = amulet,
                Limit = limit,
                Delay = delay,
                LevelBase = levelBase,
                LevelStep = levelStep,
                Health = health,
                Damage = damage,
                Defence = defence,
                MagicDefence = magicDefence,
                Preview = preview
            };
        }

        private void ApplyLimits()
        {
            foreach (SkillEditor editor in _editors)
            {
                editor.Amulet.Minimum = 0;
                editor.Amulet.Maximum = Settings.PetSummonMaxAmuletCost;

                editor.Limit.Minimum = 0;
                editor.Limit.Maximum = Settings.PetSummonMaxSummonLimit;

                editor.Delay.Minimum = 0;
                editor.Delay.Maximum = Settings.PetSummonMaxSummonDelay;

                editor.LevelBase.Minimum = 0;
                editor.LevelBase.Maximum = byte.MaxValue;

                editor.LevelStep.Minimum = 0;
                editor.LevelStep.Maximum = byte.MaxValue;
            }

            nudMaxActivePets.Minimum = 0;
            nudMaxActivePets.Maximum = Settings.PetSummonMaxSummonLimit;

            nudExpMultiplier.Minimum = 0;
            nudExpMultiplier.Maximum = Settings.PetSummonMaxAmuletCost;

            nudExpBase.Minimum = 0;
            nudExpBase.Maximum = Settings.PetSummonMaxExperience;

            nudExpPerLevel.Minimum = 0;
            nudExpPerLevel.Maximum = Settings.PetSummonMaxExperience;

            NumericUpDown[] speeds = { nudMoveSpeed, nudAttackSpeed, nudMinMoveSpeed, nudMinAttackSpeed };

            foreach (NumericUpDown speed in speeds)
            {
                speed.Minimum = 0;
                speed.Maximum = Settings.PetSummonMaxSpeed;
            }

            NumericUpDown[] inheritancePercents =
            {
                nudInheritMinDamage, nudInheritMaxDamage,
                nudInheritMinDefence, nudInheritMaxDefence,
                nudInheritMinMagicDefence, nudInheritMaxMagicDefence
            };

            foreach (NumericUpDown box in inheritancePercents)
            {
                box.Minimum = 0;
                box.Maximum = Settings.PetInheritanceMaxPercent;
            }

            nudInheritHealth.Minimum = 0;
            nudInheritHealth.Maximum = Settings.PetInheritanceMaxHealthPerPoint;
        }

        private void LoadSettings()
        {
            LoadSummon(_editors[0], Settings.PetSummonSkeleton);
            LoadSummon(_editors[1], Settings.PetSummonShinsu);
            LoadSummon(_editors[2], Settings.PetSummonHolyDeva);

            PetSummonSharedSettings shared = Settings.PetSummonShared ?? new PetSummonSharedSettings();

            nudMaxActivePets.Value = Clamp(shared.MaxActivePets, nudMaxActivePets.Maximum);
            nudExpMultiplier.Value = Clamp(shared.ExperienceMultiplier, nudExpMultiplier.Maximum);
            nudExpBase.Value = Clamp(shared.ExperienceBase, nudExpBase.Maximum);
            nudExpPerLevel.Value = Clamp(shared.ExperiencePerLevel, nudExpPerLevel.Maximum);
            nudMoveSpeed.Value = Clamp(shared.MoveSpeedPerLevel, nudMoveSpeed.Maximum);
            nudAttackSpeed.Value = Clamp(shared.AttackSpeedPerLevel, nudAttackSpeed.Maximum);
            nudMinMoveSpeed.Value = Clamp(shared.MinimumMoveSpeed, nudMinMoveSpeed.Maximum);
            nudMinAttackSpeed.Value = Clamp(shared.MinimumAttackSpeed, nudMinAttackSpeed.Maximum);

            PetInheritanceSettings inheritance = Settings.TaoistPetInheritance ?? new PetInheritanceSettings { OwnerStat = Stat.MaxSC };

            chkTaoistInherit.Checked = inheritance.Enabled;
            nudInheritMinDamage.Value = Clamp(inheritance.MinDamagePercent, nudInheritMinDamage.Maximum);
            nudInheritMaxDamage.Value = Clamp(inheritance.MaxDamagePercent, nudInheritMaxDamage.Maximum);
            nudInheritMinDefence.Value = Clamp(inheritance.MinDefencePercent, nudInheritMinDefence.Maximum);
            nudInheritMaxDefence.Value = Clamp(inheritance.MaxDefencePercent, nudInheritMaxDefence.Maximum);
            nudInheritMinMagicDefence.Value = Clamp(inheritance.MinMagicDefencePercent, nudInheritMinMagicDefence.Maximum);
            nudInheritMaxMagicDefence.Value = Clamp(inheritance.MaxMagicDefencePercent, nudInheritMaxMagicDefence.Maximum);
            nudInheritHealth.Value = Clamp(inheritance.HealthPerPoint, nudInheritHealth.Maximum);
        }

        private void LoadSummon(SkillEditor editor, PetSummonSettings settings)
        {
            editor.Monster.Text = settings.MonsterName;
            editor.Amulet.Value = Clamp(settings.AmuletCost, editor.Amulet.Maximum);
            editor.Limit.Value = Clamp(settings.SummonLimit, editor.Limit.Maximum);
            editor.Delay.Value = Clamp(settings.SummonDelay, editor.Delay.Maximum);
            editor.LevelBase.Value = Clamp(settings.MaxLevelBase, editor.LevelBase.Maximum);
            editor.LevelStep.Value = Clamp(settings.MaxLevelPerMagicLevel, editor.LevelStep.Maximum);

            editor.Health.Text = PetSummonSettings.FormatGrowth(settings.HealthPerLevel);
            editor.Damage.Text = PetSummonSettings.FormatGrowth(settings.DamagePerLevel);
            editor.Defence.Text = PetSummonSettings.FormatGrowth(settings.DefencePerLevel);
            editor.MagicDefence.Text = PetSummonSettings.FormatGrowth(settings.MagicDefencePerLevel);

            Validate(editor);
        }

        private void EditorChanged(object sender, EventArgs e)
        {
            foreach (SkillEditor editor in _editors) Validate(editor);
        }

        /// <summary>
        /// Marks the fields that would be rejected on save and refreshes the level formula of the
        /// skill. A missing monster database only skips the name check, it does not block editing.
        /// </summary>
        private bool Validate(SkillEditor editor)
        {
            bool valid = true;

            bool known = Resolve(editor.Monster.Text) != null;

            if (_monsters.Count == 0)
            {
                editor.MonsterStatus.Text = GameLanguage.Get("Name not checked, monster database is not loaded.");
                editor.MonsterStatus.ForeColor = SystemColors.GrayText;
                editor.Monster.BackColor = SystemColors.Window;
            }
            else if (known)
            {
                editor.MonsterStatus.Text = string.Empty;
                editor.MonsterStatus.ForeColor = Color.Firebrick;
                editor.Monster.BackColor = SystemColors.Window;
            }
            else
            {
                editor.MonsterStatus.Text = GameLanguage.Get("Monster not found");
                editor.MonsterStatus.ForeColor = Color.Firebrick;
                editor.Monster.BackColor = Color.MistyRose;
                valid = false;
            }

            valid &= ValidateGrowth(editor.Health);
            valid &= ValidateGrowth(editor.Damage);
            valid &= ValidateGrowth(editor.Defence);
            valid &= ValidateGrowth(editor.MagicDefence);

            long limit = (long)editor.LevelBase.Value + (long)editor.LevelStep.Value * 3;

            if (limit > byte.MaxValue) limit = byte.MaxValue;

            editor.Preview.Text = GameLanguage.Get(
                "Max pet level = base + step x spell level (0-3), capped at 255. Spell level 3 gives {0}. New pets start at the cast level; name colour only changes between levels 1 and 7.",
                limit);

            return valid;
        }

        private static bool ValidateGrowth(TextBox box)
        {
            bool valid = PetSummonSettings.TryParseGrowth(box.Text, out _);

            box.BackColor = valid ? SystemColors.Window : Color.MistyRose;

            return valid;
        }

        /// <summary>
        /// Mirrors Envir.GetMonsterInfo(name): case insensitive, ordinary spaces ignored.
        /// </summary>
        private MonsterInfo Resolve(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            MonsterInfo exact = _monsters.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            string compact = name.Replace(" ", string.Empty);

            return _monsters.FirstOrDefault(x => string.Equals(x.Name.Replace(" ", string.Empty), compact, StringComparison.OrdinalIgnoreCase));
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

            foreach (SkillEditor editor in _editors)
            {
                editor.Monster.Text = editor.Monster.Text.Trim();
                valid &= Validate(editor);
            }

            if (!valid)
            {
                lblStatus.Text = GameLanguage.Get("Fix the red fields before saving.");
                return;
            }

            Settings.PetSummonSkeleton = ReadSummon(_editors[0]);
            Settings.PetSummonShinsu = ReadSummon(_editors[1]);
            Settings.PetSummonHolyDeva = ReadSummon(_editors[2]);

            Settings.PetSummonShared = new PetSummonSharedSettings
            {
                MaxActivePets = (int)nudMaxActivePets.Value,
                ExperienceMultiplier = (int)nudExpMultiplier.Value,
                ExperienceBase = (int)nudExpBase.Value,
                ExperiencePerLevel = (int)nudExpPerLevel.Value,
                MoveSpeedPerLevel = (int)nudMoveSpeed.Value,
                AttackSpeedPerLevel = (int)nudAttackSpeed.Value,
                MinimumMoveSpeed = (int)nudMinMoveSpeed.Value,
                MinimumAttackSpeed = (int)nudMinAttackSpeed.Value
            };

            Settings.TaoistPetInheritance = new PetInheritanceSettings
            {
                OwnerStat = Stat.MaxSC,
                Enabled = chkTaoistInherit.Checked,
                MinDamagePercent = (int)nudInheritMinDamage.Value,
                MaxDamagePercent = (int)nudInheritMaxDamage.Value,
                MinDefencePercent = (int)nudInheritMinDefence.Value,
                MaxDefencePercent = (int)nudInheritMaxDefence.Value,
                MinMagicDefencePercent = (int)nudInheritMinMagicDefence.Value,
                MaxMagicDefencePercent = (int)nudInheritMaxMagicDefence.Value,
                HealthPerPoint = (int)nudInheritHealth.Value
            };

            Settings.SavePetSummon();

            // The world loop rebuilds the pet stats, so the editor never touches live objects.
            SMain.Envir?.RequiresPetInheritanceUpdate();

            DialogResult = DialogResult.OK;
            Close();
        }

        private static PetSummonSettings ReadSummon(SkillEditor editor)
        {
            PetSummonSettings.TryParseGrowth(editor.Health.Text, out int[] health);
            PetSummonSettings.TryParseGrowth(editor.Damage.Text, out int[] damage);
            PetSummonSettings.TryParseGrowth(editor.Defence.Text, out int[] defence);
            PetSummonSettings.TryParseGrowth(editor.MagicDefence.Text, out int[] magicDefence);

            return new PetSummonSettings
            {
                MonsterName = editor.Monster.Text.Trim(),
                AmuletCost = (int)editor.Amulet.Value,
                SummonLimit = (int)editor.Limit.Value,
                SummonDelay = (int)editor.Delay.Value,
                MaxLevelBase = (int)editor.LevelBase.Value,
                MaxLevelPerMagicLevel = (int)editor.LevelStep.Value,
                HealthPerLevel = health,
                DamagePerLevel = damage,
                DefencePerLevel = defence,
                MagicDefencePerLevel = magicDefence
            };
        }
    }
}
