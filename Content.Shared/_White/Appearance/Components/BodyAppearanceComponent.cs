using Content.Shared._White.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Appearance.Components;

/// <summary>
/// Component on an entity with <see cref="BodyComponent" /> that modifies its appearance based on contained provider with <see cref="BodyAppearanceProviderComponent" />.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BodyAppearanceComponent : Component;
