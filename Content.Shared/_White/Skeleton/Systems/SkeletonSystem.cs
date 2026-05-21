using Content.Shared._White.Body.Systems;
using Content.Shared._White.Skeleton.Components;
using Content.Shared._White.Wounds;
using Content.Shared._White.Wounds.Systems;

namespace Content.Shared._White.Skeleton.Systems;

public sealed class SkeletonSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkeletonProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<SkeletonProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<SkeletonProviderComponent, WoundableSeverityChangedEvent>(OnWoundableSeverityChanged);
    }

    #region Event Handling

    private void OnGotInsertedIntoParent(Entity<SkeletonProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Parent));

        RaiseLocalEvent(args.Parent, new SkeletonSeverityChangedEvent(ent.Comp.Severity, ent));
    }

    private void OnGotRemovedFromParent(Entity<SkeletonProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Parent));

        RaiseLocalEvent(args.Parent, new SkeletonSeverityChangedEvent(WoundSeverity.None, ent));
    }

    private void OnWoundableSeverityChanged(Entity<SkeletonProviderComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        ent.Comp.Severity = args.Severity;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Severity));

        RaiseLocalEvent(ent, new SkeletonSeverityChangedEvent(args.Severity, ent));

        if (ent.Comp.Parent is not { } parent)
            return;

        RaiseLocalEvent(parent, new SkeletonSeverityChangedEvent(args.Severity, ent));
    }

    #endregion
}

/// <summary>
/// Event raised on entity after changing his skeleton severity.
/// </summary>
public record struct SkeletonSeverityChangedEvent(WoundSeverity Severity, Entity<SkeletonProviderComponent> Provider);
