using System.Collections.Generic;

/// <summary>
/// Classifies spells by where their cast point comes from.
/// Self spells are anchored on the caster: the cursor position the client sends with the cast carries no
/// meaning for them, so it may neither be out of range nor block the cast.
/// </summary>
public static class SpellTargets
{
    private static readonly HashSet<Spell> SelfCast = new HashSet<Spell>
    {
        //Warrior
        Spell.ShoulderDash,
        Spell.LionRoar,
        Spell.BladeAvalanche,
        Spell.ProtectionField,
        Spell.Rage,
        Spell.SlashingBurst,
        Spell.Fury,
        Spell.ImmortalSkin,

        //Wizard
        Spell.Repulsion,
        Spell.HellFire,
        Spell.Lightning,
        Spell.ThunderStorm,
        Spell.MagicShield,
        Spell.Mirroring,
        Spell.FlameField,
        Spell.MagicBooster,
        Spell.IceThrust,

        //Taoist
        Spell.SummonSkeleton,
        Spell.Hiding,
        Spell.EnergyRepulsor,
        Spell.SummonShinsu,
        Spell.SummonHolyDeva,

        //Assassin
        Spell.Haste,
        Spell.FlashDash,
        Spell.LightBody,
        Spell.HeavenlySword,
        Spell.FireBurst,
        Spell.PoisonSword,
        Spell.MoonLight,
        Spell.SwiftFeet,
        Spell.MoonMist,
        Spell.CrescentSlash,

        //Archer
        Spell.BackStep,
        Spell.Concentration,
        Spell.ElementalBarrier,
        Spell.ExplosiveTrap,

        //Custom
        Spell.BattleCry
    };

    public static bool CastsOnSelf(this Spell spell) => SelfCast.Contains(spell);
}
