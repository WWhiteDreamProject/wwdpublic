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
        SubscribeLocalEvent<SkeletonProviderComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
    }

    #region Event Handling

    private void OnGotInsertedIntoParent(Entity<SkeletonProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        ent.Comp.Parent = args.Parent;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Parent));

        var ev = new SkeletonSeverityChangedEvent(ent.Comp.Severity, ent);
        RaiseLocalEvent(args.Parent, ref ev);
    }

    private void OnGotRemovedFromParent(Entity<SkeletonProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        ent.Comp.Parent = null;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Parent));

        var ev = new SkeletonSeverityChangedEvent(WoundSeverity.None, ent);
        RaiseLocalEvent(args.Parent, ref ev);
    }

    private void OnWoundSeverityChanged(Entity<SkeletonProviderComponent> ent, ref WoundSeverityChangedEvent args)
    {
        ent.Comp.Severity = args.Severity;
        DirtyField(ent, ent.Comp, nameof(SkeletonProviderComponent.Severity));

        var ev = new SkeletonSeverityChangedEvent(args.Severity, ent);
        RaiseLocalEvent(ent, ref ev);

        if (ent.Comp.Parent is not { } parent)
            return;

        RaiseLocalEvent(parent, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on entity after changing his skeleton severity.
/// </summary>
[ByRefEvent]
public record struct SkeletonSeverityChangedEvent(WoundSeverity Severity, Entity<SkeletonProviderComponent> Provider);
