using Content.Shared._White.Body.Components;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    /// <summary>
    /// Container ID prefix for any organs.
    /// </summary>
    public const string OrganSlotContainerIdPrefix = "organ_slot_";

    /// <summary>
    /// Container ID prefix for any body parts.
    /// </summary>
    public const string BodyPartSlotContainerIdPrefix = "body_part_slot_";

    /// <summary>
    /// Container ID prefix for any bones.
    /// </summary>
    public const string BoneSlotContainerIdPrefix = "bone_slot_";

    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] protected new readonly IPrototypeManager Prototype = default!;

    [Dependency] private readonly SharedContainerSystem _container = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("body");

        InitializeAppearance();
        InitializeBody();
        InitializeBodyPart();
        InitializeBone();
        InitializeOrgan();
        InitializeRelay();
    }

    /// <summary>
    /// Gets the container id for the specified slotId.
    /// </summary>
    public static string GetBodyPartSlotContainerId(string slotId) => BodyPartSlotContainerIdPrefix + slotId;

    /// <summary>
    /// Gets the slot id for the specified container id.
    /// </summary>
    public string GetBodyPartSlotId(string containerSlotId)
    {
        var slotIndex = containerSlotId.IndexOf(BodyPartSlotContainerIdPrefix, StringComparison.Ordinal);

        if (slotIndex < 0)
            return string.Empty;

        return containerSlotId.Remove(slotIndex, BodyPartSlotContainerIdPrefix.Length);
    }

    /// <summary>
    /// Gets the container id for the specified slotId.
    /// </summary>
    public static string GetOrganContainerId(string slotId) => OrganSlotContainerIdPrefix + slotId;

    /// <summary>
    /// Gets the slot id for the specified container id.
    /// </summary>
    public string GetOrganSlotId(string containerSlotId)
    {
        var slotIndex = containerSlotId.IndexOf(OrganSlotContainerIdPrefix, StringComparison.Ordinal);

        if (slotIndex < 0)
            return string.Empty;

        return containerSlotId.Remove(slotIndex, OrganSlotContainerIdPrefix.Length);
    }

    /// <summary>
    /// Gets the container id for the specified slotId.
    /// </summary>
    public static string GetBoneContainerId(string slotId) => BoneSlotContainerIdPrefix + slotId;

    /// <summary>
    /// Gets the slot id for the specified container id.
    /// </summary>
    public string GetBoneSlotId(string containerSlotId)
    {
        var slotIndex = containerSlotId.IndexOf(BoneSlotContainerIdPrefix, StringComparison.Ordinal);

        if (slotIndex < 0)
            return string.Empty;

        return containerSlotId.Remove(slotIndex, BoneSlotContainerIdPrefix.Length);
    }
}

/// <summary>
/// Raised on an organ after its toggled.
/// </summary>
public record struct AfterOrganToggledEvent(bool Enable);

/// <summary>
/// An event wrapper for passing events related to body parts.
/// </summary>
[ByRefEvent]
public record struct BodyPartRelayedEvent<TEvent>(Entity<BodyComponent> Body, TEvent Args);

/// <summary>
/// An event wrapper for passing events related to bones.
/// </summary>
[ByRefEvent]
public record struct BoneRelayedEvent<TEvent>(Entity<BodyComponent> Body, TEvent Args);

/// <summary>
/// An event wrapper for passing events related to organs.
/// </summary>
[ByRefEvent]
public record struct OrganRelayedEvent<TEvent>(Entity<BodyComponent> Body, TEvent Args);

/// <summary>
/// Raised when a body part is attached to body.
/// </summary>
/// <param name="Part">The attached body part.</param>
/// <param name="Body">The body to which the body part was attached.</param>
/// <param name="SlotId">Container ID of Part.</param>
public readonly record struct BodyPartAddedEvent(
    Entity<BodyPartComponent> Part,
    Entity<BodyComponent>? Body,
    EntityUid Parent,
    string SlotId);

/// <summary>
/// Raised when a body part is detached from body.
/// </summary>
/// <param name="Part">The detached body part.</param>
/// <param name="Body">The body from which the body part was detached.</param>
/// <param name="SlotId">Container ID of Part.</param>
public readonly record struct BodyPartRemovedEvent(
    Entity<BodyPartComponent> Part,
    Entity<BodyComponent>? Body,
    EntityUid Parent,
    string SlotId);

public readonly record struct BoneAddedEvent(
    Entity<BoneComponent> Bone,
    Entity<BodyComponent>? Body,
    EntityUid Parent,
    string SlotId);

public readonly record struct BoneRemovedEvent(
    Entity<BoneComponent> Bone,
    Entity<BodyComponent>? Body,
    EntityUid Parent,
    string SlotId);

/// <summary>
/// Raised when an organ attaches to body.
/// </summary>
/// <param name="Body">The body to which the organ is attached.</param>
/// <param name="Parent">The parent to which the organ is attached.</param>
public readonly record struct OrganAddedEvent(
    Entity<OrganComponent> Organ,
    Entity<BodyComponent>? Body,
    EntityUid Parent,
    string SlotId);

/// <summary>
/// Raised when an organ detaches from body.
/// </summary>
/// <param name="Body">The body from which the organ is detached.</param>
/// <param name="Parent">The parent from which the organ is detached.</param>
public readonly record struct OrganRemovedEvent(
    Entity<OrganComponent> Organ,
    Entity<BodyComponent>? Body,
    EntityUid Parent,
    string SlotId);
