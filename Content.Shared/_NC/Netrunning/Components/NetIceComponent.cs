using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.Components;

[Serializable, NetSerializable]
public enum NetIceType
{
    Gate,
    Sentry,
    Killer
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetIceComponent : Component
{
    [DataField]
    public NetIceType IceType = NetIceType.Gate;

    [DataField]
    public int Level = 1;

    [DataField]
    public string Password = "1234";

    [DataField, AutoNetworkedField]
    public int MaxHealth = 50;

    [DataField, AutoNetworkedField]
    public int CurrentHealth = 50;

    /// <summary>
    /// Physical damage dealt to player (accumulates, applied as Heat on disconnect).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Damage = 10;

    /// <summary>
    /// RAM damage dealt to cyberdeck per attack.
    /// </summary>
    [DataField]
    public int RamDamage = 5;

    /// <summary>
    /// Chance to disconnect hacker from network (0.0 - 1.0). For Sentry type.
    /// </summary>
    [DataField]
    public float DisconnectChance = 0.1f;

    /// <summary>
    /// Max password attempts before disconnect. For Gate type.
    /// </summary>
    [DataField]
    public int MaxPasswordAttempts = 3;
}
