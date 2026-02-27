using Content.Shared._White.Teleportation.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Teleportation.Systems;

public sealed class BeaconTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly WhitePortalSystem _portal = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeaconTeleporterComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BeaconTeleporterComponent, BeaconTeleporterDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BeaconTeleporterComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnAfterInteract(Entity<BeaconTeleporterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryComp<TeleportBeaconComponent>(args.Target, out var beacon))
        {
            if (!Deleted(ent.Comp.Beacon))
            {
                _popup.PopupClient(Loc.GetString("beacon-teleporter-already-linked"), ent, args.User);
                return;
            }

            ent.Comp.Beacon = args.Target;
            Dirty(ent);

            _audio.PlayPredicted(beacon.LinkSound, ent, args.User);
            _popup.PopupClient(Loc.GetString("beacon-teleporter-linked"), ent, args.User);

            args.Handled = true;
            return;
        }

        if (Deleted(ent.Comp.Beacon) || _useDelay.IsDelayed(ent.Owner))
            return;

        var coordinates = GetNetCoordinates(_transform.GetMoverCoordinates(args.ClickLocation).SnapToGrid(EntityManager));
        var doAfterEvent = new BeaconTeleporterDoAfterEvent(coordinates);
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.PortalCreationDelay, doAfterEvent, ent, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<BeaconTeleporterComponent> ent, ref BeaconTeleporterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || Deleted(ent.Comp.Beacon))
            return;

        _portal.SpawnPortal(GetCoordinates(args.Coordinates), _transform.GetMoverCoordinates(ent.Comp.Beacon.Value));
        _audio.PlayPredicted(ent.Comp.PortalCreateSound, ent, args.User);

        _useDelay.TryResetDelay(ent.Owner);

        args.Handled = true;
    }

    private void OnUseInHand(Entity<BeaconTeleporterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || Deleted(ent.Comp.Beacon) || _useDelay.IsDelayed(ent.Owner))
            return;

        var coordinates = GetNetCoordinates(_transform.GetMoverCoordinates(ent).SnapToGrid(EntityManager));
        var doAfterEvent = new BeaconTeleporterDoAfterEvent(coordinates);
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.PortalCreationDelay, doAfterEvent, ent, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        args.ApplyDelay = true;
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class BeaconTeleporterDoAfterEvent : DoAfterEvent
{
    public readonly NetCoordinates Coordinates;

    public BeaconTeleporterDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone() => this;
}
