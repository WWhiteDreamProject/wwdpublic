using Content.Shared._White.Bloodstream.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Bloodstream.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedBloodstreamSystem))]
public sealed partial class BleedingWoundComponent : Component
{
    /// <summary>
    /// Coefficient applied to damage to determine the rate at which bleeding occurs.
    /// </summary>
    [DataField]
    public FixedPoint2 BleedingCoefficient = 0.015f;

    /// <summary>
    /// The maximum amount of bleeding that can occur on a wound.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxBleeding = 20f;

    /// <summary>
    /// The threshold above which a wound requires tending to stop bleeding.
    /// </summary>
    [DataField]
    public FixedPoint2 RequiresTendingAbove = 25;

    /// <summary>
    /// The minimum amount of damage a wound must inflict to start causing bleeding.
    /// </summary>
    [DataField]
    public FixedPoint2 StartsBleedingAbove = 5;

    /// <summary>
    /// The coefficient used to determine the duration of bleeding based on damage.
    /// </summary>
    [DataField]
    public float BleedingDurationCoefficient = 1f;

    /// <summary>
    /// The body entity containing this wound, if any.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// The amount of bleeding at <see cref="BleedingSince"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Bleeding;

    /// <summary>
    /// Actually, damage to this wound.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 Damage;

    /// <summary>
    /// When the bleeding was last changed.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan BleedingSince = TimeSpan.Zero;

    /// <summary>
    /// How long it takes the wound to stop bleeding on its own, counted from <see cref="BleedingSince"/>.
    /// Null if the wound requires tending and won't stop bleeding by itself.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan? BleedingDuration;
}
