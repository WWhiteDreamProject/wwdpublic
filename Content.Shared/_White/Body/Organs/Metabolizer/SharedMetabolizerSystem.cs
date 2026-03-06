namespace Content.Shared._White.Body.Organs.Metabolizer;

public abstract class SharedMetabolizerSystem : EntitySystem
{
    /// <summary>
    /// Updates the metabolic rate for a given entity,
    /// Raises to retrieve the base rate, to find the active multiplier, and then to update relevant components based on these results.
    /// </summary>
    /// <param name="uid"></param>
    public void UpdateMetabolicRate(EntityUid uid)
    {
        var getRateEv = new GetMetabolicRateEvent();
        RaiseLocalEvent(uid, ref getRateEv);

        var getMultiplierEv = new GetMetabolicMultiplierEvent(getRateEv.Rate);
        RaiseLocalEvent(uid, ref getMultiplierEv);

        var applyEv = new ApplyMetabolicRateEvent(getMultiplierEv.Multiplier);
        RaiseLocalEvent(uid, ref applyEv);
    }
}

/// <summary>
/// Raised on an entity to determine their metabolic rate.
/// </summary>
[ByRefEvent]
public record struct GetMetabolicRateEvent(float Rate = 0f)
{
    /// <summary>
    /// What the metabolism's rate.
    /// </summary>
    public float Rate = Rate;
}

/// <summary>
/// Raised on an entity to determine their metabolic multiplier.
/// </summary>
[ByRefEvent]
public record struct GetMetabolicMultiplierEvent(float Multiplier = 1f)
{
    /// <summary>
    /// What the metabolism's rate will be multiplied by.
    /// </summary>
    public float Multiplier = Multiplier;
}

/// <summary>
/// Raised on an entity to apply their metabolic multiplier to relevant systems.
/// Note that you should be storing this value as to not accrue precision errors when it's modified.
/// </summary>
[ByRefEvent]
public readonly record struct ApplyMetabolicRateEvent(float Rate)
{
    /// <summary>
    /// What the metabolism's rate.
    /// </summary>
    public readonly float Rate = Rate;
}
