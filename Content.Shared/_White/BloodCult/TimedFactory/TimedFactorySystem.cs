using Content.Shared._White.RadialSelector;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.RadialSelector;
using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult;
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

    private void OnTryOpenMenu(Entity<TimedFactoryComponent> factory, ref ActivatableUIOpenAttemptEvent args)
    {
        if (factory.Comp.CooldownIn > _gameTiming.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("timed-factory-cooldown", ("cooldown", factory.Comp.CooldownIn.TotalSeconds)), factory, args.User);
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

        _ui.CloseUi(args.Actor, RadialSelectorUiKey.Key);
    }
}
