using Robust.Shared.GameStates;

namespace Content.Shared._White.Bed.Components;

/// <summary>
/// Tracking component added to entities buckled to stasis beds.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StasisBedBuckledComponent : Component;
