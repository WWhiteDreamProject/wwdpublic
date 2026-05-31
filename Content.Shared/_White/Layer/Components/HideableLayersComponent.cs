using Content.Shared._White.Layer.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Layer.Components;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedHideableLayersSystem))]
public sealed partial class HideableLayersComponent : Component
{
    /// <summary>
    /// Tracks the current state of hidden layers and the equipment slots responsible for hiding them.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, SlotFlags> HiddenLayers = new();

    /// <summary>
    /// Defines which visual layers  should be hidden when an item is equipped into a corresponding slot.
    /// </summary>
    [DataField]
    public HashSet<Enum> HideLayersOnEquip = [HumanoidVisualLayers.Hair];

    /// <summary>
    /// Stores the set of layers that were hidden during the last update.
    /// </summary>
    [ViewVariables]
    public HashSet<Enum> LastHiddenLayers = new();
}
