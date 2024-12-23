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
    [DataField("jumpAction", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string JumpAction = "ActionHeadcrabJump";

    public EntityUid? WormJumpAction;

    [DataField("paralyzeTime"), ViewVariables(VVAccess.ReadWrite)]
    public float ParalyzeTime = 3f;

    [DataField("chansePounce"), ViewVariables(VVAccess.ReadWrite)]
    public int ChansePounce = 33;

    [DataField("damage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    public bool IsDeath = false;

    public EntityUid EquipedOn;

    [ViewVariables] public float Accumulator = 0;

    [DataField("damageFrequency"), ViewVariables(VVAccess.ReadWrite)]
    public float DamageFrequency = 5;

    [ViewVariables(VVAccess.ReadWrite), DataField("jumpSound")]
    public SoundSpecifier? HeadcrabJumpSound = new SoundPathSpecifier("/Audio/_White/Misc/Headcrab/headcrab_jump.ogg");

}
