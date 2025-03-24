using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared.Actions.Events;
using Content.Shared.Popups;

namespace Content.Shared._White.Xenomorphs.Actions;

public sealed class BasePlasmaCostActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPlasmaSystem _plasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BasePlasmaCostActionComponent, ActionAttemptEvent>(OnActionAttempt);
    }

    private void OnActionAttempt(EntityUid uid, BasePlasmaCostActionComponent component, ref ActionAttemptEvent args)
    {
        if (TryComp<PlasmaVesselComponent>(args.User, out var plasmaComp)
            && _plasma.ChangePlasmaAmount(args.User, -component.PlasmaCost, plasmaComp))
            return;

        _popup.PopupClient(Loc.GetString("alien-action-fail-plasma"), args.User, args.User);
        args.Cancelled = true;
    }
}
