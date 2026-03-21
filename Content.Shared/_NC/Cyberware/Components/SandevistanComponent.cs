using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Компонент импланта "Сандевистан". Хранит базовые настройки.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SpeedMultiplier = 2.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float AttackSpeedMultiplier = 2.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float DoAfterMultiplier = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaxDuration = 5.0f;

    /// <summary>
    ///     Стоимость активации в единицах человечности.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ActivationHumanityCost = 5.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float StaminaDrainPerSecond = 10.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float HumanityLossPerInterval = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public DamageSpecifier? OverloadDamage;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float OverloadStunDuration = 4.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color TrailColor = Color.FromHex("#00FFFF").WithAlpha(0.4f);

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TrailInterval = 0.05f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TrailLifetime = 0.3f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntProtoId ActionId = "ActionToggleSandevistan";

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ActionEntity;
}

/// <summary>
///     Маркерный компонент на ИГРОКЕ, когда Сандевистан активен.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveSandevistanComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TimeRemaining;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float HumanityLossAccumulator = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float HumanityLossInterval = 3.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid ImplantEntity;

    /// <summary>
    ///     Сущность проигрываемого звука, чтобы мы могли его остановить.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RunningSound;
}
