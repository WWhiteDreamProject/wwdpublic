using Content.Shared._White.Layer.Systems;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Layer.Components;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedHideableLayersSystem))]
public sealed partial class HideableLayersComponent : Component
{
    /// <summary>
    ///     A map of the visual layers currently hidden to the equipment
    ///     slots that are currently hiding them. This will affect the base
    ///     sprite on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, SlotFlags> HiddenLayers = new();

    /// <summary>
    ///     Client only - which layers were last hidden
    /// </summary>
    [ViewVariables]
    public HashSet<Enum> LastHiddenLayers = new();
}
