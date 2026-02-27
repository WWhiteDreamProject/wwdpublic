using Content.Shared._White.Body.Components;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared.DragDrop;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] protected new readonly IPrototypeManager Prototype = default!;

    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BodyProviderComponent> _providerQuery;

    /// <summary>
    /// Container ID prefix for any body provider.
    /// </summary>
    public const string ProviderSlotContainerIdPrefix = "body_provider_slot_";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("body");

        SubscribeLocalEvent<BodyComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnMapInit);

        InitializeAppearance();
        InitializeProvider();
        InitializeRelay();

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _providerQuery = GetEntityQuery<BodyProviderComponent>();
    }

    #region Event Handling

    private void OnCanDrag(Entity<BodyComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnMapInit(Entity<BodyComponent> body, ref MapInitEvent args)
    {
        SetupProvider(body.Comp.RootProvider, body, body, body.Comp.RootProviderId);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets the container id for the specified slotId.
    /// </summary>
    public static string GetProviderSlotContainerId(string slotId)
    {
        return ProviderSlotContainerIdPrefix + slotId;
    }

    /// <summary>
    /// Gets the slot id for the specified container id.
    /// </summary>
    public string GetProviderSlotId(string containerSlotId)
    {
        var slotIndex = containerSlotId.IndexOf(ProviderSlotContainerIdPrefix, StringComparison.Ordinal);

        return slotIndex < 0 ? string.Empty : containerSlotId.Remove(slotIndex, ProviderSlotContainerIdPrefix.Length);
    }

    #endregion
}

/// <summary>
/// Event raised on body provider entity, when it is inserted into a body.
/// </summary>
[ByRefEvent]
public readonly record struct BodyProviderGotInsertedEvent(EntityUid Body);

/// <summary>
/// Event raised on body provider entity, when it is inserted into a parent.
/// </summary>
[ByRefEvent]
public readonly record struct BodyProviderGotInsertedIntoParentEvent(EntityUid Parent);

/// <summary>
/// Event raised on body provider entity, when it is removed from a body.
/// </summary>
[ByRefEvent]
public readonly record struct BodyProviderGotRemovedEvent(EntityUid Body);

/// <summary>
/// Event raised on body provider entity, when it is removed from a parent.
/// </summary>
[ByRefEvent]
public readonly record struct BodyProviderGotRemovedFromParentEvent(EntityUid Parent);

/// <summary>
/// Event raised on body entity, when a body provider is inserted into it.
/// </summary>
[ByRefEvent]
public readonly record struct BodyProviderInsertedIntoEvent(EntityUid Provider);

/// <summary>
/// Event raised on body entity, when a body provider is removed from it.
/// </summary>
[ByRefEvent]
public readonly record struct BodyProviderRemovedFromEvent(EntityUid Provider);

/// <summary>
/// Event raised on body entity, when an organ/body part/bone is having its appearance copied to it.
/// </summary>
[ByRefEvent]
public readonly record struct BodyCopyAppearanceEvent(EntityUid Provider);
