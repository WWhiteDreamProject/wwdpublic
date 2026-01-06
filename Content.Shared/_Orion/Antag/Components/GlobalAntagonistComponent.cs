using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Orion.Antag.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GlobalAntagonistComponent : Component
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<AntagonistPrototype>))]
    public string? AntagonistPrototype;
}
