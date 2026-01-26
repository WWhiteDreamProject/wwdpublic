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

[RegisterComponent, NetworkedComponent]
public sealed partial class NetProgramComponent : Component
{
    [DataField("programType"), ViewVariables(VVAccess.ReadWrite)]
    public NetProgramType ProgramType = NetProgramType.Quickhack;

    [DataField("ramCost"), ViewVariables(VVAccess.ReadWrite)]
    public int RamCost = 1;

    [DataField("energyCost"), ViewVariables(VVAccess.ReadWrite)]
    public int EnergyCost = 0;

    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 5.0f;

    [DataField("beamColor"), ViewVariables(VVAccess.ReadWrite)]
    public Color BeamColor = Color.Green;
}
