using Robust.Shared.GameStates;

namespace Content.Shared._NC.Cyberware.Components;

/// <summary>
///     Маркер для импланта "Анти-блик".
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberwareAntiGlareComponent : Component { }

/// <summary>
///     Маркер для импланта "ИК-зрение".
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberwareIRVisionComponent : Component { }
