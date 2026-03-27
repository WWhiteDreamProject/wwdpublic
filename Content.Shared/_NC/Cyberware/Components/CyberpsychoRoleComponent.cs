using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Маркерный компонент для роли Киберпсиха.
///     Должен быть в Shared для корректной работы системы ролей.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberpsychoRoleComponent : Component
{
}

/// <summary>
///     Компонент, который вешается на СУЩНОСТЬ РОЛИ (Mind Role Entity).
///     Нужен для того, чтобы MindRemoveRole смог найти и удалить именно эту роль.
/// </summary>
[RegisterComponent]
public sealed partial class CyberpsychoRoleRoleComponent : Component
{
}