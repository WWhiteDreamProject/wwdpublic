using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Systems;

namespace Content.Shared._White.Body.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeGenerator()
    {
        SubscribeLocalEvent<BloodGeneratorComponent, BodyPartRelayedEvent<GetBloodReductionEvent>>(OnGetBloodReduction);
        SubscribeLocalEvent<BloodGeneratorComponent, BoneRelayedEvent<GetBloodReductionEvent>>(OnGetBloodReduction);
        SubscribeLocalEvent<BloodGeneratorComponent, OrganRelayedEvent<GetBloodReductionEvent>>(OnGetBloodReduction);
    }

    #region Event Handling

    private void OnGetBloodReduction(Entity<BloodGeneratorComponent> ent, ref BodyPartRelayedEvent<GetBloodReductionEvent> args) =>
        args.Args = new (BloodReduction: args.Args.BloodReduction + ent.Comp.BleedReductionAmount);

    private void OnGetBloodReduction(Entity<BloodGeneratorComponent> ent, ref BoneRelayedEvent<GetBloodReductionEvent> args) =>
        args.Args = new (BloodReduction: args.Args.BloodReduction + ent.Comp.BleedReductionAmount);

    private void OnGetBloodReduction(Entity<BloodGeneratorComponent> ent, ref OrganRelayedEvent<GetBloodReductionEvent> args) =>
        args.Args = new (BloodReduction: args.Args.BloodReduction + ent.Comp.BleedReductionAmount);

    #endregion
}
