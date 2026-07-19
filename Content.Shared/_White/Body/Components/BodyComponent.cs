using Content.Shared._White.Body.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyComponent : Component
{
    [DataField]
    public bool ThermalVisibility = true;

    /// <summary>
    /// Relevant template to spawn for this body.
    /// </summary>
    [DataField(required: true)]
    public BodyProviderSlot RootProvider;

    /// <summary>
    /// Root body provider id. Usually we want it to be chest.
    /// </summary>
    [DataField]
    public string RootProviderId = "chest";

    /// <summary>
    /// Body providers attached to this body.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<(string Id, NetEntity Parent), BodyProviderSlot> Providers = new();
}

[Serializable, NetSerializable]
public sealed class BodyComponentState(BodyComponent component) : ComponentState
{
    public readonly Dictionary<(string, NetEntity), BodyProviderSlot> Providers = new(component.Providers);
}
