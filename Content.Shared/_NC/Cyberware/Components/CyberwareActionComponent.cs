using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Универсальный компонент для киберимплантов, выдающий экшен при установке в пациента.
///     Добавляется на сущность самого импланта (предмета).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberwareActionComponent : Component
{
    /// <summary>
    ///     Идентификатор прототипа экшена (например, ActionToggleSandevistan).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntProtoId ActionId = string.Empty;

    /// <summary>
    ///     Созданная сущность экшена.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? ActionEntity;
}
