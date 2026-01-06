using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Orion.Antag.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GlobalAntagonistComponent : Component
{
    [DataField] // WWDP EDIT
    public ProtoId<AntagonistPrototype> AntagonistPrototype = "globalAntagonistUnknown"; // WWDP EDIT
}
