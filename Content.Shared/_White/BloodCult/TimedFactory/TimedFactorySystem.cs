using Content.Shared._White.RadialSelector;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.RadialSelector;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Shared._White.BloodCult.TimedFactory;

public sealed class TimedFactorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedFactoryComponent, ActivatableUIOpenAttemptEvent>(OnTryOpenMenu);
        SubscribeLocalEvent<TimedFactoryComponent, RadialSelectorSelectedMessage>(OnPrototypeSelected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var factoryQuery = EntityQueryEnumerator<TimedFactoryComponent>();
        while (factoryQuery.MoveNext(out var uid, out var factory))
        {
            if (factory.CooldownIn > _gameTiming.CurTime)
                return;

            _appearance.SetData(uid, GenericCultVisuals.State, true);
        }
    }

    private void OnTryOpenMenu(Entity<TimedFactoryComponent> factory, ref ActivatableUIOpenAttemptEvent args)
    {
        if (factory.Comp.CooldownIn > _gameTiming.CurTime)
        {
            _popup.PopupClient(Loc.GetString("timed-factory-cooldown", ("cooldown", factory.Comp.CooldownIn.TotalSeconds)), factory, args.User);
            args.Cancel();
            return;
        }

        _ui.SetUiState(factory.Owner, RadialSelectorUiKey.Key, new TrackedRadialSelectorState(factory.Comp.Entries));
    }

    private void OnPrototypeSelected(Entity<TimedFactoryComponent> factory, ref RadialSelectorSelectedMessage args)
    {
        if (factory.Comp.CooldownIn > _gameTiming.CurTime)
            return;

        var product = Spawn(args.SelectedItem, Transform(args.Actor).Coordinates);
        _hands.TryPickupAnyHand(args.Actor, product);

        factory.Comp.CooldownIn = _gameTiming.CurTime + factory.Comp.Cooldown;

        _appearance.SetData(factory, GenericCultVisuals.State, false);

        _ui.CloseUi(factory.Owner, RadialSelectorUiKey.Key);
    }
}
