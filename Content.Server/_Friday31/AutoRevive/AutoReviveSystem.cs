using Content.Server.Administration.Systems;
using Content.Shared._Friday31.AutoRevive;
using Content.Shared._Friday31.Jason;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Friday31.AutoRevive;

public sealed class AutoReviveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoReviveComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AutoReviveComponent, MobStateComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var autoRevive, out var mobState))
        {
            if (!_mobState.IsDead(uid, mobState))
                continue;

            if (autoRevive.DeathTime == null)
                continue;

            var timeSinceDeath = currentTime - autoRevive.DeathTime.Value;
            if (timeSinceDeath.TotalSeconds < autoRevive.ReviveDelay)
                continue;

            PerformAutoRevive(uid, autoRevive);
        }
    }

    private void OnMobStateChanged(EntityUid uid, AutoReviveComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            component.DeathTime = _timing.CurTime;
            
            var message = Loc.GetString("auto-revive-death-message", 
                ("name", Name(uid)), 
                ("seconds", component.ReviveDelay));
            _popup.PopupEntity(message, uid, PopupType.LargeCaution);
        }
    }

    private void PerformAutoRevive(EntityUid uid, AutoReviveComponent component)
    {
        _rejuvenate.PerformRejuvenate(uid);

        var message = Loc.GetString("auto-revive-revived-message", ("name", Name(uid)));
        _popup.PopupEntity(message, uid, PopupType.Large);

        _audio.PlayPvs("/Audio/_Friday31/jason_revive.ogg", uid, AudioParams.Default.WithVolume(5f));

        _standing.Stand(uid);

        if (TryComp<JasonDecapitateAbilityComponent>(uid, out var decapitateAbility) && decapitateAbility.ActionEntity != null)
        {
            _actions.StartUseDelay(decapitateAbility.ActionEntity);
        }

        component.DeathTime = null;
    }
}
