using Content.Server._NC.Cyberware.Systems;
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._NC.Cyberware.Systems;
using Content.Shared._NC.Trail;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Server.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._NC.Cyberware.Systems;

public sealed class ServerSandevistanSystem : SharedSandevistanSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HumanitySystem _humanity = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanComponent, ComponentStartup>(OnSandevistanStartup);
        SubscribeLocalEvent<SandevistanComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<SandevistanToggleActionEvent>(OnToggleAction);
    }

    private void OnSandevistanStartup(EntityUid uid, SandevistanComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.ActionId);
        
        var xform = Transform(uid);
        if (xform.ParentUid.IsValid() && HasComp<CyberwareComponent>(xform.ParentUid))
        {
            _actions.AddAction(xform.ParentUid, component.ActionEntity!.Value, uid);
        }
    }

    private void OnParentChanged(EntityUid uid, SandevistanComponent component, ref EntParentChangedMessage args)
    {
        if (component.ActionEntity == null)
            return;

        // Удаляем экшен у старого владельца
        if (args.OldParent != null && args.OldParent.Value.IsValid())
        {
            _actions.RemoveAction(args.OldParent.Value, component.ActionEntity.Value);
        }

        // Добавляем экшен НОВОМУ владельцу только если это установка (CyberwareComponent)
        var newParent = args.Transform.ParentUid;
        if (newParent.IsValid() && HasComp<CyberwareComponent>(newParent))
        {
            _actions.AddAction(newParent, component.ActionEntity.Value, uid);
        }
    }

    private void OnToggleAction(SandevistanToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var action = args.Action;

        if (!_actions.TryGetActionData(action, out var actionComp) || actionComp.Container == null)
            return;

        var implant = actionComp.Container.Value;

        if (!TryComp<SandevistanComponent>(implant, out var sande))
            return;

        if (HasComp<ActiveSandevistanComponent>(user))
        {
            Deactivate(user);
        }
        else
        {
            Activate(user, implant, sande);
        }

        args.Handled = true;
    }

    private void Activate(EntityUid user, EntityUid implant, SandevistanComponent sande)
    {
        if (TryComp<StaminaComponent>(user, out var stamina))
        {
            if (stamina.StaminaDamage > stamina.CritThreshold * 0.8f)
            {
                _popup.PopupEntity(Loc.GetString("sandevistan-no-stamina"), user, user, PopupType.MediumCaution);
                return;
            }
        }

        // Мгновенное списание человечности за активацию (вместо кулдауна)
        _humanity.DeductHumanity(user, sande.ActivationHumanityCost);

        var active = EnsureComp<ActiveSandevistanComponent>(user);
        active.TimeRemaining = sande.MaxDuration;
        active.ImplantEntity = implant;
        active.HumanityLossAccumulator = 0f;

        var trail = EnsureComp<TrailComponent>(user);
        trail.RenderedEntity = user;
        trail.Color = sande.TrailColor;
        trail.Frequency = sande.TrailInterval;
        trail.Lifetime = sande.TrailLifetime;
        trail.AlphaLerpAmount = 0.15f;
        trail.AlphaLerpTarget = 0f;
        trail.Scale = 1f;
        trail.RenderedEntityRotationStrategy = RenderedEntityRotationStrategy.Particle;

        var audioResult = _audio.PlayPvs("/Audio/_NC/Cyberware/sandevistan/sande_start.ogg", user);
        if (audioResult != null)
            active.RunningSound = audioResult.Value.Entity;

        MovementSpeedModifier.RefreshMovementSpeedModifiers(user);
        
        if (sande.ActionEntity != null)
            _actions.SetToggled(sande.ActionEntity.Value, true);
    }

    private void Deactivate(EntityUid user, bool overload = false)
    {
        if (!TryComp<ActiveSandevistanComponent>(user, out var active))
            return;

        if (active.RunningSound != null)
        {
            _audio.Stop(active.RunningSound.Value);
            active.RunningSound = null;
        }

        if (TryComp<SandevistanComponent>(active.ImplantEntity, out var sande))
        {
            if (sande.ActionEntity != null)
            {
                _actions.SetToggled(sande.ActionEntity.Value, false);
            }

            if (overload)
            {
                if (sande.OverloadDamage != null)
                    _damageable.TryChangeDamage(user, sande.OverloadDamage, true);

                _stun.TryParalyze(user, TimeSpan.FromSeconds(sande.OverloadStunDuration), true);
                _popup.PopupEntity(Loc.GetString("sandevistan-overload"), user, user, PopupType.LargeCaution);
            }
            else
            {
                _audio.PlayPvs("/Audio/_NC/Cyberware/sandevistan/sande_end.ogg", user);
            }
        }

        RemComp<ActiveSandevistanComponent>(user);
        RemComp<TrailComponent>(user);
        MovementSpeedModifier.RefreshMovementSpeedModifiers(user);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSandevistanComponent, StaminaComponent>();
        while (query.MoveNext(out var uid, out var active, out var stamina))
        {
            if (TryComp<SandevistanComponent>(active.ImplantEntity, out var sande))
            {
                _stamina.TakeStaminaDamage(uid, sande.StaminaDrainPerSecond * frameTime, component: stamina, visual: false);
                
                if (stamina.StaminaDamage >= stamina.CritThreshold)
                {
                    Deactivate(uid, true);
                    continue;
                }

                active.HumanityLossAccumulator += frameTime;
                if (active.HumanityLossAccumulator >= active.HumanityLossInterval)
                {
                    _humanity.DeductHumanity(uid, sande.HumanityLossPerInterval);
                    active.HumanityLossAccumulator -= active.HumanityLossInterval;
                }
            }

            active.TimeRemaining -= frameTime;
            if (active.TimeRemaining <= 0)
            {
                Deactivate(uid);
            }
        }
    }
}
