using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.Components;

[Serializable, NetSerializable]
public enum NetIceType : byte
{
    Gate,
    Sentry,
    Killer
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NetIceComponent : Component
{
    [DataField("iceType"), ViewVariables(VVAccess.ReadWrite)]
    public NetIceType IceType = NetIceType.Gate;

    [DataField("level"), ViewVariables(VVAccess.ReadWrite)]
    public int Level = 1;
}
