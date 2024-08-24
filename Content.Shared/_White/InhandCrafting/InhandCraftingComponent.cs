using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.InhandCrafting;

[RegisterComponent, NetworkedComponent]
public sealed partial class InhandCraftingComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<ConstructionPrototype>> Prototypes;
}
