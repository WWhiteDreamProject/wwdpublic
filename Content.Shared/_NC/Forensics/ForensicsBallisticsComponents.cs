using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._NC.Forensics;

/// <summary>
/// Компонент для хранения уникального хэша оружия.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsWeaponHashComponent : Component
{
    [DataField("hash"), AutoNetworkedField]
    public string Hash = string.Empty;

    /// <summary>
    /// Тир оружия. Самодельное оружие (Тир 1) можно пересобрать для смены хэша.
    /// </summary>
    [DataField("tier"), AutoNetworkedField]
    public int Tier = 1;
}

/// <summary>
/// Компонент для хранения хэша на летящей пуле.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsProjectileHashComponent : Component
{
    [DataField("hash"), AutoNetworkedField]
    public string Hash = string.Empty;
}

/// <summary>
/// Компонент для сущности, которая находится внутри тела (застрявшая пуля).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsStuckBulletComponent : Component
{
    [DataField("hash"), AutoNetworkedField]
    public string Hash = string.Empty;

    /// <summary>
    /// Сделан ли надрез для извлечения этой пули.
    /// </summary>
    [DataField("incisionMade"), AutoNetworkedField]
    public bool IncisionMade = false;
}

/// <summary>
/// Компонент для извлеченной деформированной пули (улика).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForensicsBulletComponent : Component
{
    [DataField("hash"), AutoNetworkedField]
    public string Hash = string.Empty;
}
