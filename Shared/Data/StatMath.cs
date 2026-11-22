using System;
using System.Collections.Generic;

/// <summary>
/// Pure fold helpers for the attribute layer. No RNG, no environment, so the same
/// functions can be reused by the server and by the client preview engine.
/// </summary>
public static class StatMath
{
    /// <summary>Armour reduction applied by red poison to players.</summary>
    public const float PlayerRedPoisonArmourPenalty = 0.10F;

    /// <summary>Armour reduction applied by red poison to monsters.</summary>
    public const float MonsterRedPoisonArmourPenalty = 0.5F;

    /// <summary>
    /// Multiplies every Mul rule that targets <paramref name="stat"/>. Returns 1
    /// when there is none, which makes the caller's arithmetic an identity.
    /// </summary>
    public static float MulFactor(IReadOnlyList<StatModifier> modifiers, Stat stat)
    {
        if (modifiers == null || modifiers.Count == 0) return 1F;

        float factor = 1F;

        for (int i = 0; i < modifiers.Count; i++)
        {
            StatModifier modifier = modifiers[i];

            if (modifier.Op != StatModOp.Mul) continue;
            if (modifier.Stat != stat) continue;

            factor *= modifier.Value;
        }

        return factor;
    }

    /// <summary>
    /// Keeps the scaled endpoint as a float. Rounding here would lose a whole
    /// point of range at low values (3..5 * 0.5 must stay 1.5..2.5).
    /// </summary>
    public static float Scale(int raw, float factor)
    {
        return raw * factor;
    }

    /// <summary>
    /// The only place a scaled value becomes an integer. It truncates: settlement rolls
    /// an integer armour value first and scales that single value afterwards, so the
    /// final conversion has to drop the fraction instead of rounding it away.
    /// </summary>
    public static int Truncate(float value)
    {
        return (int)value;
    }
}
