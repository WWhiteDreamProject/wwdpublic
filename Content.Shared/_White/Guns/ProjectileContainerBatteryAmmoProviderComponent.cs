using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Guns;


public abstract partial class ContainerBatteryAmmoProviderComponent : BatteryAmmoProviderComponent
{
    public EntityUid? Linked = null;
}

/// <summary>
/// Uses the battery of the container it's currently in.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProjectileContainerBatteryAmmoProviderComponent : ContainerBatteryAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanContainerBatteryAmmoProviderComponent : ContainerBatteryAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<HitscanPrototype>))]
    public string Prototype = default!;
}

[RegisterComponent]
public sealed partial class ContainerBatteryAmmoTrackerComponent : Component
{
    public List<EntityUid> Linked = new();
}
