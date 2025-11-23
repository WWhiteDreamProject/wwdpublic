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
    }

    /// <summary>
    /// Gets the container Id for the specified slotId.
    /// </summary>
    public static string GetBodyPartSlotContainerId(string slotId) => BodyPartSlotContainerIdPrefix + slotId;

    /// <summary>
    /// Gets the container Id for the specified slotId.
    /// </summary>
    public static string GetOrganContainerId(string slotId) => OrganSlotContainerIdPrefix + slotId;

    /// <summary>
    /// Gets the container Id for the specified slotId.
    /// </summary>
    public static string GetBoneContainerId(string slotId) => BoneSlotContainerIdPrefix + slotId;
}
