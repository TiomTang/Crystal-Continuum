/// <summary>
/// How a modifier combines with the value produced by earlier stages.
/// Fold order is Add, then Pct (summed), then Caps, then Mul (multiplied).
/// </summary>
public enum StatModOp
{
    Add,
    Mul,
}

/// <summary>
/// Where a modifier came from. Used for traceability and for dropping a whole
/// source (for example all poison-derived modifiers) without touching the rest.
/// </summary>
public enum ModSourceKind
{
    Level,
    Equipment,
    Set,
    Passive,
    Buff,
    Poison,
    Guild,
}

public readonly struct ModSource
{
    public readonly ModSourceKind Kind;
    public readonly int Id;

    public ModSource(ModSourceKind kind, int id)
    {
        Kind = kind;
        Id = id;
    }
}

/// <summary>
/// A single change to one stat. Rules are collected from their sources on every
/// rebuild instead of being registered and unregistered, so a missed removal can
/// never leave a permanent bonus behind.
/// </summary>
public readonly struct StatModifier
{
    public readonly Stat Stat;
    public readonly StatModOp Op;
    public readonly float Value;
    public readonly ModSource Source;

    public StatModifier(Stat stat, StatModOp op, float value, ModSource source)
    {
        Stat = stat;
        Op = op;
        Value = value;
        Source = source;
    }
}
