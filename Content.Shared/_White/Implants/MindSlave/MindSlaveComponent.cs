using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Implants.MindSlave;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class MindSlaveComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<NetEntity> Slaves = [];

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity? Master;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string SlaveStatusIcon = "SlaveMindSlaveIcon";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string MasterStatusIcon = "MasterMindSlaveIcon";
}
