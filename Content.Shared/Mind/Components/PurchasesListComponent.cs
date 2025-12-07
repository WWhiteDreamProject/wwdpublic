using Robust.Shared.GameStates;
using Content.Shared.Store;

namespace Content.Shared.Mind.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PurchasesListComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<PurchasedItemRecord> PurchaseHistory { get; set; } = new();
}
