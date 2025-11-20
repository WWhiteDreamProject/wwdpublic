using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.Network;

namespace Content.Shared._White.BloodCult.BloodRites;

public abstract class SharedBloodRitesSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;

    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

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
        if (TryGetStoredBloodAmount(args.Examiner, out var amount))
            args.PushMarkup(Loc.GetString("blood-rites-stored-blood", ("amount", amount.Value.Int())));
    }

    private void AttemptUiOpen(Entity<BloodRitesComponent> rites, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.User))
            args.Cancel();
    }

    private void BeforeUiOpen(Entity<BloodRitesComponent> rites, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryGetStoredBloodAmount(args.User, out var amount))
            return;

        var state = new BloodRitesUiState(rites.Comp.Crafts, amount.Value);
        _userInterface.SetUiState(rites.Owner, BloodRitesUiKey.Key, state);
    }

    private void OnRitesMessage(Entity<BloodRitesComponent> rites, ref BloodRitesMessage args)
    {
        if (!rites.Comp.Crafts.TryGetValue(args.SelectedProto, out var cost)
            || !TryComp<BloodCultistComponent>(args.Actor, out var bloodCultist))
            return;

        bloodCultist.StoredBloodAmount -= cost;
        Dirty(args.Actor, bloodCultist);

        _userInterface.CloseUi(rites.Owner, BloodRitesUiKey.Key);

        if (!_netManager.IsServer)
            return;

        Del(rites);
        _hands.TryPickup(args.Actor, Spawn(args.SelectedProto, _transform.GetMapCoordinates(args.Actor)));
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
