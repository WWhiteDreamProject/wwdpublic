using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Wounds.Systems;

namespace Content.Shared._White.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeAccumulator()
    {
        SubscribeLocalEvent<BloodstreamAccumulatorComponent, BodyRelayedEvent<GetBloodReductionEvent>>(OnGetBloodReduction);
        SubscribeLocalEvent<BloodstreamAccumulatorComponent, WoundableSeverityChangedEvent>(OnWoundableSeverityChanged);
    }

    #region Event Handling

    private void OnGetBloodReduction(Entity<BloodstreamAccumulatorComponent> ent, ref BodyRelayedEvent<GetBloodReductionEvent> args)
    {
        args.Args = new (args.Args.Reduction + ent.Comp.Reduction);
    }

    private void OnWoundableSeverityChanged(Entity<BloodstreamAccumulatorComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        if (!ent.Comp.ReductionThresholds.TryGetValue(args.Severity, out var reduction))
            return;

        if (ent.Comp.Reduction == reduction)
            return;

        ent.Comp.Reduction = reduction;
        DirtyField(ent, ent.Comp, nameof(BloodstreamAccumulatorComponent.Reduction));
    }

    #endregion
}
