using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.UserInterface;

namespace Content.Shared._White.BloodCult.BloodRites;

public abstract class SharedBloodRitesSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodRitesComponent, ExaminedEvent>(OnExamining);

        SubscribeLocalEvent<BloodRitesComponent, ActivatableUIOpenAttemptEvent>(AttemptUiOpen);
        SubscribeLocalEvent<BloodRitesComponent, BeforeActivatableUIOpenEvent>(BeforeUiOpen);
        SubscribeLocalEvent<BloodRitesComponent, BloodRitesMessage>(OnRitesMessage);
    }

    private void OnExamining(Entity<BloodRitesComponent> rites, ref ExaminedEvent args)
    {
        if (TryGetStoredBloodAmount(rites.Owner, out var amount))
            args.PushMarkup(Loc.GetString("blood-rites-stored-blood", ("amount", amount)));
    }

    private void AttemptUiOpen(Entity<BloodRitesComponent> rites, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<BloodCultistComponent>(rites))
            args.Cancel();
    }

    private void BeforeUiOpen(Entity<BloodRitesComponent> rites, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryGetStoredBloodAmount(rites.Owner, out var amount))
            return;

        var state = new BloodRitesUiState(rites.Comp.Crafts, amount.Value);
        _userInter.SetUiState(rites.Owner, BloodRitesUiKey.Key, state);
    }

    private void OnRitesMessage(Entity<BloodRitesComponent> rites, ref BloodRitesMessage args)
    {
        QueueDel(rites);

        var ent = Spawn(args.SelectedProto, _transform.GetMapCoordinates(args.Actor));
        _hands.TryPickup(args.Actor, ent);
    }

    protected bool TryGetStoredBloodAmount(Entity<BloodCultistComponent?> entity, [NotNullWhen(true)] out FixedPoint2? amount)
    {
        amount = null;

        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        amount = entity.Comp.StoredBloodAmount;
        return true;
    }
}
