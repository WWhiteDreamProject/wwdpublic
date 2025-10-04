using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Administration;
using Content.Shared._White.Actions.Events;
using Content.Shared.Abilities.Psionics;
using Robust.Shared.Player;

namespace Content.Server._White.Abilities.Psionics.Abilities;

public sealed class DirectionalTelepathyPowerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelepathyComponent, TelepathyPowerActionEvent>(OnPowerUsed);
    }

    private void OnPowerUsed(EntityUid uid, TelepathyComponent component, 
TelepathyPowerActionEvent args)
    {
        if (!TryComp(uid, out ActorComponent? actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("telepathy-title"),
            Loc.GetString("telepathy-message"), (string message) =>
            {
                _popup.PopupEntity(message, uid, uid, PopupType.Medium);

                _popup.PopupEntity(message, args.Target, args.Target, PopupType.Medium);
            });

        args.Handled = true;
    }
}