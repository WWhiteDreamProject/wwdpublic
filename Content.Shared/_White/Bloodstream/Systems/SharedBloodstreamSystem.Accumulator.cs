using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Wounds.Systems;

namespace Content.Shared._White.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeAccumulator()
    {
        SubscribeLocalEvent<BloodstreamAccumulatorComponent, BodyProviderGotInsertedEvent>(OnAccumulatorGotInserted);
        SubscribeLocalEvent<BloodstreamAccumulatorComponent, BodyProviderGotRemovedEvent>(OnAccumulatorGotRemoved);
        SubscribeLocalEvent<BloodstreamAccumulatorComponent, BodyRelayedEvent<GetBloodReductionEvent>>(OnGetBloodReduction);
        SubscribeLocalEvent<BloodstreamAccumulatorComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
    }

    #region Event Handling

    private void OnAccumulatorGotInserted(Entity<BloodstreamAccumulatorComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(BloodstreamAccumulatorComponent.Body));

        UpdateBloodReduction(args.Body.Owner);
    }

    private void OnAccumulatorGotRemoved(Entity<BloodstreamAccumulatorComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(BloodstreamAccumulatorComponent.Body));

        UpdateBloodReduction(args.Body.Owner);
    }

    private void OnGetBloodReduction(Entity<BloodstreamAccumulatorComponent> ent, ref BodyRelayedEvent<GetBloodReductionEvent> args)
    {
        args.Args = new(args.Args.Reduction + ent.Comp.Reduction);
    }

    private void OnWoundSeverityChanged(Entity<BloodstreamAccumulatorComponent> ent, ref WoundSeverityChangedEvent args)
    {
        if (!ent.Comp.ReductionThresholds.TryGetValue(args.Severity, out var reduction))
            return;

        if (ent.Comp.Reduction == reduction)
            return;

        ent.Comp.Reduction = reduction;
        DirtyField(ent, ent.Comp, nameof(BloodstreamAccumulatorComponent.Reduction));

        if (ent.Comp.Body is not {} body)
            return;

        UpdateBloodReduction(body);
    }

    #endregion
}
