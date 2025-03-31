using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._White.EntityGenerator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedEntityGeneratorSystem))]
public sealed partial class EntityGeneratorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? PrototypeId;

    [DataField, AutoNetworkedField]
    public int MaxCharges = 3;

    [DataField, AutoNetworkedField]
    public int Charges = 3;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RechargeDuration = TimeSpan.FromSeconds(5);

    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LastExtractTime;

    [DataField, AutoNetworkedField]
    public bool UseSingleCharge = true;

    [DataField, AutoNetworkedField]
    public bool OnlyFullRecharge;
}
