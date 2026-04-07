using Content.Shared._White.Actions.Events;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._White.Psionics.Abilities;

public sealed class SpawnImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PsionicHookPowerComponent, PsionicHookPowerActionEvent>(OnPowerUse);
        SubscribeLocalEvent<CuffableComponent, PsionicUncuffDoAfterEvent>(OnUncuff);
    }

    private void OnPowerUse(EntityUid uid, PsionicHookPowerComponent component, PsionicHookPowerActionEvent args)
    {
        if (!_psionics.OnAttemptPowerUse(args.Performer, "hook"))
            return;

        if (component.Hook != null && !Deleted(component.Hook)) //if we already have hook in hand
        {
            Del(component.Hook);
            component.Hook = null;

            _audio.PlayPvs(component.SoundOnDespawn, uid);

            args.Handled = true;
            return;
        }

        if (TryComp<CuffableComponent>(uid, out var cuffable) && cuffable.CuffedHandCount > 0)
        {
            StartUncuffing(uid, component);
            args.Handled = true;
            return;
        }

        var coords = Transform(uid).Coordinates;

        var hook = EntityManager.SpawnEntity(component.HookPrototype, coords);

        if (_hands.TryPickupAnyHand(uid, hook))
        {
            component.Hook = hook;
            _audio.PlayPvs(component.SoundOnSpawn, hook);
            args.Handled = true;
            return;
        }

        component.Hook = null;
        Del(hook);
        args.Handled = true;
    }

    private void StartUncuffing(EntityUid uid, PsionicHookPowerComponent component)
    {
        var ev = new PsionicUncuffDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, uid, component.BreakoutTime, ev, uid)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,
            DistanceThreshold = 1f
        };

        _popup.PopupEntity(Loc.GetString(component.UncuffPopup), uid, uid, PopupType.SmallCaution);
        _doAfter.TryStartDoAfter(args);
    }

    private void OnUncuff(EntityUid uid, CuffableComponent component, PsionicUncuffDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<CuffableComponent>(uid, out var cuffs))
            return;
        
        if (cuffs.CuffedHandCount == 0)
            return;

        var lastAddedCuffs = cuffs.LastAddedCuffs;
        _cuffs.Uncuff(uid, uid, lastAddedCuffs);
    }
}
