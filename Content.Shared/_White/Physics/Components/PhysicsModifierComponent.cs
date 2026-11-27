using Content.Shared._White.Physics.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Physics.Components;

/// <summary>
/// Remembers which collision bits of an entity's fixtures were changed by <see cref="PhysicsModifierSystem"/>,
/// so they can be reverted without touching the bits changed by anything else.
/// Added on the first modification and removed once nothing is modified anymore.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PhysicsModifierSystem))]
public sealed partial class PhysicsModifierComponent : Component
{
    /// <summary>
    /// The current modifications, by fixture ID. Fixtures without modifications aren't here.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, FixtureCollisionModification> Fixtures = new();
}

/// <summary>
/// The collision bits of a single fixture changed by <see cref="PhysicsModifierSystem"/>.
/// Removed bits were present before the modification, added bits were absent.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct FixtureCollisionModification
{
    [DataField]
    public int RemovedMask;

    [DataField]
    public int AddedMask;

    [DataField]
    public int RemovedLayer;

    [DataField]
    public int AddedLayer;

    [ViewVariables]
    public readonly bool IsEmpty => RemovedMask == 0 && AddedMask == 0 && RemovedLayer == 0 && AddedLayer == 0;
}
