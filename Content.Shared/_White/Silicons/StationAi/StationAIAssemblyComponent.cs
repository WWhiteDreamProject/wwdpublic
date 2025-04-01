using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared._White.Silicons.StationAi;


[RegisterComponent]
public sealed partial class StationAIAssemblyComponent : Component
{
    [DataField, ViewVariables]
    public string BrainSlotId = "brain_slot";

    [DataField, ViewVariables]
    public string StationAIMindSlot = "station_ai_mind_slot";

    [DataField, ViewVariables]
    public EntProtoId StationAIPrototype = "PlayerStationAiEmpty";

    [DataField, ViewVariables]
    public string CoverMaterialStackPrototype = "ReinforcedGlass";

    [DataField, ViewVariables]
    public int CoverMaterialStackSize = 2;
}

[Serializable, NetSerializable]
public enum StationAIAssemblyVisualLayers : byte
{
    Brain
}

[Serializable, NetSerializable]
public enum StationAIAssemblyVisuals : byte
{
    HasBrain
}
