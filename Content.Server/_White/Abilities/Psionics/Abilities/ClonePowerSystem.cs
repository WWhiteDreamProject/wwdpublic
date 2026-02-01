using Content.Server.Actions;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Hands.Systems;
using Content.Server.Abilities.Psionics;
using Content.Shared._White.Abilities.Psionics;
using Content.Shared._White.Actions.Events;
using Content.Server.Station.Systems;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Physics.Systems;

namespace Content.Server._White.Abilities.Psionics.Abilities;

public sealed class ClonePowerSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedPsionicAbilitiesSystem _psionicsAbilities = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClonePowerComponent, ClonePowerActionEvent>(OnPowerUsed);
        SubscribeLocalEvent<ClonePowerComponent, CloneSwitchPowerActionEvent>(CloneSwitch);
        SubscribeLocalEvent<PsionicCloneComponent, DispelledEvent>(OnDispelled);
        SubscribeLocalEvent<PsionicCloneComponent, OriginalSwitchPowerActionEvent>(OriginalSwitch);
        SubscribeLocalEvent<PsionicCloneComponent, MobStateChangedEvent>(OnCloneDeath);
    }

    public void OnPowerUsed(EntityUid uid, ClonePowerComponent component, ClonePowerActionEvent args)
    {
        if (!_psionicsAbilities.OnAttemptPowerUse(args.Performer, "clone")
            || !TryComp<ActorComponent>(uid, out var actorComponent))
            return;
        
        if (component.CloneUid != null && TryComp<TransformComponent>(component.CloneUid.Value, out var cloneTransform)) 
        {
            StripCloneEquipment(component.CloneUid.Value);

            var coord = Transform(uid).Coordinates;
            cloneTransform.Coordinates = coord;
            _audio.PlayPvs(component.CloneSound, coord);
            Spawn(component.CloneEffect, coord);

            args.Handled = true;
            return;
        }

        var coords = Transform(uid).Coordinates;
        _audio.PlayPvs(component.CloneSound, coords);
        Spawn(component.CloneEffect, coords);

        var stationUid = _station.GetOwningStation(uid);

        var profile = _gameTicker.GetPlayerProfile(actorComponent.PlayerSession);
        var clone = _stationSpawning.SpawnPlayerMob(coords, null, profile, stationUid);
        component.CloneUid = clone;

        var cloneComp = AddComp<PsionicCloneComponent>(clone);
        cloneComp.OriginalUid = uid;
        _actions.AddAction(clone, "ActionOriginalSwitch");

        _psionicsAbilities.LogPowerUsed(uid, "clone");
        args.Handled = true;
    }

    public void CloneSwitch(EntityUid uid, ClonePowerComponent component, CloneSwitchPowerActionEvent args)
    {
        if (component.CloneUid is not { } clone)
        {
            _popup.PopupEntity(Loc.GetString("clone-no-clone"), uid, uid);
            return;
        }

        if (!_mobState.IsAlive(clone))
        {
            _popup.PopupEntity(Loc.GetString("clone-crit-clone"), uid, uid);
            return;
        }

        if (_mind.TryGetMind(clone, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("clone-mind-clone"), uid, uid);
            return;
        }

        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        
        _mind.TransferTo(mindId, component.CloneUid, mind: mind);
        args.Handled = true;
    }

    public void OriginalSwitch(EntityUid uid, PsionicCloneComponent component, OriginalSwitchPowerActionEvent args)
    {
        if (component.OriginalUid is not { } original)
        {
            _popup.PopupEntity(Loc.GetString("clone-no-original"), uid, uid);
            return;
        }
        
        if (!_mobState.IsAlive(original))
        {
            _popup.PopupEntity(Loc.GetString("clone-crit-original"), uid, uid);
            return;
        }

        if (_mind.TryGetMind(original, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("clone-mind-original"), uid, uid);
            return;
        }

        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        
        _mind.TransferTo(mindId, component.OriginalUid, mind: mind);
        args.Handled = true;
    }

    private void OnDispelled(EntityUid uid, PsionicCloneComponent component, DispelledEvent args)
    {
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
        {
            if (component.OriginalUid != null)
                _mind.TransferTo(mindId, component.OriginalUid.Value, mind: mind);
        }

        if (TryComp<ClonePowerComponent>(component.OriginalUid, out var origComp))
            origComp.CloneUid = null;

        StripCloneEquipment(uid);

        QueueDel(uid);
        Spawn("Ectoplasm", Transform(uid).Coordinates);

        _popup.PopupCoordinates(Loc.GetString("psionic-burns-up", ("item", uid)), Transform(uid).Coordinates, Filter.Pvs(uid), true, Shared.Popups.PopupType.MediumCaution);
        _audio.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(uid), uid, true);

        args.Handled = true;
    }

    private void StripCloneEquipment(EntityUid cloneUid)
    {
        if (_inventory.TryGetContainerSlotEnumerator(cloneUid, out var enumerator))
        {
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (_inventory.TryUnequip(cloneUid, cloneUid, slot.Name, true, true))
                    _physics.ApplyAngularImpulse(item, ThrowingSystem.ThrowAngularImpulse);
            }
        }

        if (TryComp<HandsComponent>(cloneUid, out var hands))
        {
            foreach (var hand in _hands.EnumerateHands(cloneUid, hands))
            {
                _hands.TryDrop(cloneUid, hand, checkActionBlocker: false, doDropInteraction: false, handsComp: hands);
            }
        }
    }

    private void OnCloneDeath(EntityUid uid, PsionicCloneComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TryComp<ClonePowerComponent>(component.OriginalUid, out var origComp))
            origComp.CloneUid = null;

        if (component.OriginalUid != null)
        {
            if (_mind.TryGetMind(uid, out var mindId, out var mind) && !_mind.TryGetMind(component.OriginalUid.Value, out _, out _))
            {
                _mind.TransferTo(mindId, component.OriginalUid.Value, mind: mind);
            }
        }

        StripCloneEquipment(uid);

        var xform = Transform(uid);

        Spawn(component.SpawnOnDeathPrototype, xform.Coordinates);
        QueueDel(uid);

        _popup.PopupCoordinates(Loc.GetString("psionic-burns-up", ("item", uid)), xform.Coordinates, Filter.Pvs(uid), true, Shared.Popups.PopupType.MediumCaution);
        _audio.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(uid), uid, true);
    }
}
