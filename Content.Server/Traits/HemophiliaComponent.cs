using Content.Shared.Damage;
namespace Content.Server.Traits.Assorted;

/// <summary>
///   This is used for the Hemophilia trait.
/// </summary>
[RegisterComponent]
public sealed partial class HemophiliaComponent : Component
{
    // WD EDIT START
    /// <summary>
    /// Multiplier to use for the amount of bloodloss reduction during a bleed tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedReductionMultiplier = 0.33f;

    /// <summary>
    /// Multiplier to use for the amount of blood lost during a bleed tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedAmountMultiplier = 1f;
    // WD EDIT END
}
