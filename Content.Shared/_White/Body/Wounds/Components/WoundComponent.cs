using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, EntityCategory("Wounds")]
public sealed partial class WoundComponent : Component
{
    /// <summary>
    /// Indicates whether the wound is a scar or not.
    /// </summary>
    [DataField]
    public bool IsScar;

    /// <summary>
    /// Damage type of this wound.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> DamageType;

    /// <summary>
    /// The description to use for this wound. Highest threshold used.
    /// </summary>
    [DataField]
    public SortedDictionary<WoundSeverity, LocId> Descriptions = new();

    /// <summary>
    /// Thresholds for determining the severity of a wound relative to the damage received.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, WoundSeverity> Thresholds = new()
    {
        {0, WoundSeverity.Healthy},
        {15, WoundSeverity.Minor},
        {40, WoundSeverity.Moderate},
        {70, WoundSeverity.Severe},
        {90, WoundSeverity.Critical},
    };

    /// <summary>
    /// Body of this wound.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// Parent body part for this wound.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Parent;

    /// <summary>
    /// Actually, damage to this wound.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 DamageAmount;

    /// <summary>
    /// The time of the last positive change in the amount of damage.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan WoundedAt = TimeSpan.Zero;

    /// <summary>
    /// The current severity of the wound.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public WoundSeverity WoundSeverity;
}

public enum WoundSeverity : byte
{
    Healthy,
    Minor,
    Moderate,
    Severe,
    Critical,
}
