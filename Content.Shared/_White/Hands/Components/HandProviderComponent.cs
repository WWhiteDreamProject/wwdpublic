using Content.Shared._White.Hands.Systems;
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Hands.Components;

/// <summary>
/// Body provider with this component provides a hand with the given ID and data to the body when inserted.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(HandProviderSystem))]
public sealed partial class HandProviderComponent : Component
{
    /// <summary>
    /// The location of the hand of the created hand.
    /// </summary>
    [DataField]
    public HandLocation HandLocation = HandLocation.Middle;

    /// <summary>
    /// The hand ID used by <see cref="HandsComponent" /> on the body.
    /// </summary>
    [DataField(required: true)]
    public string HandId;
}
