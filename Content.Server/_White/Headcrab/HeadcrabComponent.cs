using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._White.Headcrab;

[Access(typeof(HeadcrabSystem))]
[RegisterComponent]
public sealed partial class HeadcrabComponent : Component
{
    /// <summary>
    /// WorldTargetAction
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string JumpAction = "ActionHeadcrabJump";

    [DataField("paralyzeTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 3f;

    [DataField("chancePounce"), ViewVariables(VVAccess.ReadWrite)]
    public int ChancePounce = 33;

    [DataField("damage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    public bool IsDead = false;

    public EntityUid EquippedOn;

    [ViewVariables] public float Accumulator = 0;

    [DataField("damageFrequency"), ViewVariables(VVAccess.ReadWrite)]
    public float DamageFrequency = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField("jumpSound")]
    public SoundSpecifier? HeadcrabJumpSound = new SoundPathSpecifier("/Audio/_White/Misc/Headcrab/headcrab_jump.ogg");

}
