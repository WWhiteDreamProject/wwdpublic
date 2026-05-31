using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.MagicMirror.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.MagicMirror.Components;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas:true)]
[Access(typeof(MagicMirrorSystem))]
public sealed partial class MagicMirrorComponent : Component
{
    /// <summary>
    /// Magic mirror target, used for validating UI messages.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public float SelfTimeMultiply = 0.3f;

    [DataField(required: true)]
    public HashSet<Enum> Layers = new ();

    [DataField(required: true)]
    public HashSet<ProtoId<MarkingCategoryPrototype>> Categories = new ();

    /// <summary>
    /// Sound emitted when slots are changed
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Items/scissors.ogg");

    /// <summary>
    /// Do after time to modify an entity's markings
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(7);

    /// <summary>
    /// The id for a doAfter our <see cref="Target"/> is doing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort? DoAfter;
}
