using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared._White.RemoteControl;
using Content.Shared._White.MindProjection;
using Content.Shared._White.MindProjection.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Mobs;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._White.RemoteControl;

public partial class RemoteControlSystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    private void InitializeTarget()
    {
        SubscribeLocalEvent<RemoteControllableComponent, ComponentShutdown>(OnTurretShutdown);
        SubscribeLocalEvent<RemoteControllableComponent, MobStateChangedEvent>(OnTurretMobStateChanged);
        SubscribeLocalEvent<RemoteControllableComponent, PowerChangedEvent>(OnTurretPowerChanged);
    }

    private void OnTurretShutdown(EntityUid uid, RemoteControllableComponent comp, ComponentShutdown args)
    {
        UpdateStateForAllConnected(uid);
    }

    private void OnTurretMobStateChanged(EntityUid uid, RemoteControllableComponent comp, MobStateChangedEvent args)
    {
        UpdateStateForAllConnected(uid);
    }

    private void OnTurretPowerChanged(EntityUid uid, RemoteControllableComponent comp, PowerChangedEvent args)
    {
        UpdateStateForAllConnected(uid);
    }

    protected override void OnRenameVerb(ICommonSession player, EntityUid target, RemoteControllableComponent comp)
    {
        var currentName = comp.Name;
        _quickDialog.OpenDialog(player, "Change Turret ID", ("Turret ID", currentName), (string newName) =>
        {
            comp.Name = newName;
            Dirty(target, comp);
        });
    }
}
