using Robust.Shared.GameStates;

namespace Content.Shared._White.Weapons.Ranged.DualWield;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DualWieldComponent : Component
{
    /// <summary>
    /// EntityUid of the linked dual-wielded weapon
    /// </summary>
    [DataField]
    public EntityUid? LinkedWeapon;

    /// <summary>
    /// Delay between firing main weapon and linked weapon in seconds
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FireDelay = 0.1f;

    /// <summary>
    /// Multiplier applied to weapon spread when dual-wielding
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpreadMultiplier = 6f;
}
