using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.Components;

[Serializable, NetSerializable]
public enum NetProgramType : byte
{
    Quickhack,
    Network
}

[Serializable, NetSerializable]
public enum QuickhackType : byte
{
    None,
    Ping,
    Blind,
    WeaponDrop,
    Damage,
    HighJack,
    Backdoor // Safe exit from hacking
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetProgramComponent : Component
{
    [DataField("programType"), ViewVariables(VVAccess.ReadWrite)]
    public NetProgramType ProgramType = NetProgramType.Quickhack;

    [DataField("ramCost"), ViewVariables(VVAccess.ReadWrite)]
    public int RamCost = 1;

    [DataField("energyCost"), ViewVariables(VVAccess.ReadWrite)]
    public int EnergyCost = 0;

    [DataField("uploadTime"), ViewVariables(VVAccess.ReadWrite)]
    public float UploadTime = 1.0f;

    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 5.0f;

    [DataField("beamColor"), ViewVariables(VVAccess.ReadWrite)]
    public Color BeamColor = Color.Green;

    [DataField("quickhackType"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public QuickhackType QuickhackType = QuickhackType.None;

    [DataField("damage"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Damage = 10;

    [DataField("stunDuration"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float StunDuration = 0f;

    [DataField("defense"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Defense = 0; // Damage reduction
}
