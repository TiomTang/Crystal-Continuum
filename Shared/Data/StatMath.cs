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
    /// The only place a scaled value becomes an integer.
    /// </summary>
    public static int RoundEff(float value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Applies an armour penalty to the four defence stats in place. The client
    /// preview engine folds this directly; the server folds the same penalty through
    /// its modifier list, so the constant lives in one place.
    /// </summary>
    public static void ApplyArmourPenalty(Stats stats, float penalty)
    {
        float factor = 1F - penalty;

        stats[Stat.MinAC] = RoundEff(stats[Stat.MinAC] * factor);
        stats[Stat.MaxAC] = RoundEff(stats[Stat.MaxAC] * factor);
        stats[Stat.MinMAC] = RoundEff(stats[Stat.MinMAC] * factor);
        stats[Stat.MaxMAC] = RoundEff(stats[Stat.MaxMAC] * factor);
    }

    /// <summary>
    /// Picks a value from the effective (already scaled) range. The caller owns
    /// the random source so the same contract can be exercised in tests.
    /// </summary>
    public static int RollBetween(float min, float max, double sample)
    {
        if (min < 0F) min = 0F;
        if (min > max) max = min;

        if (sample <= 0D) return RoundEff(min);
        if (sample >= 1D) return RoundEff(max);

        return RoundEff(min + (float)sample * (max - min));
    }
}

/// <summary>
/// Effective defence range after attribute multipliers. Stored as floats so a
/// 50% reduction of 3..5 becomes 1.5..2.5 instead of collapsing to 1..2.
/// </summary>
public readonly struct DefenceValues
{
    public readonly float MinAC;
    public readonly float MaxAC;
    public readonly float MinMAC;
    public readonly float MaxMAC;

    public DefenceValues(int minAC, int maxAC, int minMAC, int maxMAC, float armourFactor, float magicArmourFactor)
    {
        MinAC = StatMath.Scale(minAC, armourFactor);
        MaxAC = StatMath.Scale(maxAC, armourFactor);
        MinMAC = StatMath.Scale(minMAC, magicArmourFactor);
        MaxMAC = StatMath.Scale(maxMAC, magicArmourFactor);
    }

    public int DisplayMinAC => StatMath.RoundEff(MinAC);
    public int DisplayMaxAC => StatMath.RoundEff(MaxAC);
    public int DisplayMinMAC => StatMath.RoundEff(MinMAC);
    public int DisplayMaxMAC => StatMath.RoundEff(MaxMAC);

    /// <summary>
    /// Display value for one of the four defence stats. Any other stat is not
    /// part of this layer and returns 0.
    /// </summary>
    public int Display(Stat stat)
    {
        switch (stat)
        {
            case Stat.MinAC: return DisplayMinAC;
            case Stat.MaxAC: return DisplayMaxAC;
            case Stat.MinMAC: return DisplayMinMAC;
            case Stat.MaxMAC: return DisplayMaxMAC;
            default: return 0;
        }
    }
}
