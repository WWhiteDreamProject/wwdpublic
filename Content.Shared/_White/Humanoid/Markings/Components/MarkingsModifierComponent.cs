using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Markings.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Components;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas:true)]
[Access(typeof(MarkingsModifierSystem))]
public sealed partial class MarkingsModifierComponent : Component
{
    /// <summary>
    /// The entity currently being modified.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Time multiplier when modifying yourself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SelfTimeMultiply = 3f;

    /// <summary>
    /// Allowed marking categories that can be modified.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<MarkingCategoryPrototype>> Categories = new();

    /// <summary>
    /// Sound emitted when slots are changed.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Items/scissors.ogg");

    /// <summary>
    /// Do after time to modify an entity's markings.
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(7);

    /// <summary>
    /// The id for a doAfter our <see cref="Target"/> is doing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort? DoAfter;
}
