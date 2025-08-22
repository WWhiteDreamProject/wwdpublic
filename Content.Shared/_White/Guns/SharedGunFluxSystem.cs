using Content.Shared._Shitmed.Targeting;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using MathNet.Numerics.Distributions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._White.Guns;

/// <summary>
/// Handles gun overheating mechanics.
/// TODO - decide whether or not i should separate lamp handling into a different system or is it good enough as it is
/// </summary>
public abstract class SharedGunFluxSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly ItemSlotsSystem _slots = default!;
    [Dependency] protected readonly IRobustRandom _rng = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunFluxComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunFluxComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunFluxComponent, ExaminedEvent>(OnGunExamined);
        SubscribeLocalEvent<GunFluxComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);

        SubscribeLocalEvent<FluxCoreComponent, ExaminedEvent>(OnCoreExamined);
    }

    private void OnAttemptShoot(EntityUid uid, GunFluxComponent comp, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (!GetFluxCore(uid, comp, out var core))
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-flux-missing-core");
            return;
        }

        if (!comp.SafetyEnabled)
            return;

        var newFlux = GetCurrentFlux(core) + comp.HeatCost;

        if(newFlux > core.SafeFlux)
        {
            args.Cancelled = true;
            if (core.SafeFlux == 0)
            {
                if (core.Owner == comp.Owner)
                    args.Message = Loc.GetString("gun-flux-integrated-core-unsafe");
                else
                    args.Message = Loc.GetString("gun-flux-core-unsafe");
            }
            else
                args.Message = Loc.GetString("gun-flux-safety-too-hot");
            return;
        }

        if(newFlux < 0)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-flux-safety-too-cold");
            return;
        }
    }

    protected virtual void OnGunShot(EntityUid uid, GunFluxComponent comp, ref GunShotEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        GetFluxCore(comp, out var core);
        DebugTools.Assert(core is not null);
        var overflow = AddFlux(core, comp.HeatCost);
        TryMalfunction(args.User, uid, comp, core);
        if (overflow != 0)
        {
            var hand = _hands.GetActiveHand(args.User);

            // i hate shitmed I Hate Shitmed I HATE SHITMED
            var targetBodyPart = hand?.Location switch
            {
                Shared.Hands.Components.HandLocation.Right => TargetBodyPart.RightHand,
                Shared.Hands.Components.HandLocation.Left => TargetBodyPart.LeftHand,
                Shared.Hands.Components.HandLocation.Middle => TargetBodyPart.Torso
            };

            if(overflow > 0)
            {
                var damage = overflow * comp.OverflowDamage * core.OverflowDamageMultiplier;
                damage = MathF.Min(damage, comp.MaxOverflowDamage);

                _popup.PopupClient(Loc.GetString(comp.OverflowDamageMessage, ("gun", Name(uid))), args.User, PopupType.SmallCaution);
                _audio.PlayLocal(comp.OverflowDamageSound, args.User, args.User);
                ApplyOverheatDamage(args.User, damage, comp.OverflowDamageType, targetBodyPart);
            }
            else
            {
                var damage = -overflow * comp.UnderflowDamage * core.UnderflowDamageMultiplier;
                damage = MathF.Min(damage, comp.MaxUnderflowDamage);
                DebugTools.Assert(damage > 0);

                _popup.PopupClient(Loc.GetString(comp.UnderflowDamageMessage, ("gun", Name(uid))), args.User, PopupType.SmallCaution);
                _audio.PlayLocal(comp.UnderflowDamageSound, args.User, args.User);
                ApplyOverheatDamage(args.User, damage, comp.UnderflowDamageType, targetBodyPart);
            }
        }
    }

    private void OnGunExamined(EntityUid uid, GunFluxComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString($"gun-examine-flux-safety", ("enabled", comp.SafetyEnabled)));

        if(_slots.TryGetSlot(uid, comp.CoreSlot, out var coreSlot))
            args.PushMarkup(Loc.GetString("gun-examine-flux-core", ("present", coreSlot.HasItem)));
    }

    private void OnAltVerbs(EntityUid uid, GunFluxComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess || !comp.CanChangeSafety)
            return;
        var player = args.User;

        AddVerb(-3, "fireselector-toggle-verb", ref args, () => ToggleSafety(comp, player));
        return;

        void AddVerb(int priority, string text, ref GetVerbsEvent<AlternativeVerb> args, Action act) =>
            args.Verbs.Add(
                new()
                {
                    Icon = new SpriteSpecifier.Texture(new ("/Textures/_White/Interface/VerbIcons/fireselector.192dpi.png")),
                    Priority = priority,
                    DoContactInteraction = true,
                    Text = Loc.GetString(text),
                    Act = act
                });

        void ToggleSafety(GunFluxComponent heat, EntityUid user)
        {
            if (!_timing.IsFirstTimePredicted)
                return;
            _audio.PlayPredicted(heat.ToggleSafetySound, heat.Owner, user);
            heat.SafetyEnabled = !heat.SafetyEnabled;
        }
    }

    private void OnCoreExamined(EntityUid uid, FluxCoreComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("flux-core-examine-levels", ("current", GetCurrentFlux(comp)), ("max", comp.Capacity)));
    }

    public bool GetFluxCore(GunFluxComponent comp, [NotNullWhen(true)] out FluxCoreComponent? core) => GetFluxCore(comp.Owner, comp, out core);
    public bool GetFluxCore(EntityUid uid, GunFluxComponent comp, [NotNullWhen(true)] out FluxCoreComponent? core)
    {
        core = null;

        if (TryComp<ItemSlotsComponent>(uid, out var slotComp) &&
            _slots.TryGetSlot(uid, comp.CoreSlot, out var slot, slotComp) &&
            TryComp(slot.Item, out core))
            return true;

        if (TryComp(uid, out core))
            return true;

        return false;
    }

    public float? GetCurrentFlux(GunFluxComponent comp)
    {
        if (GetFluxCore(comp, out var core))
            return GetCurrentFlux(core);
        return null;
    }

    public float GetCurrentFlux(FluxCoreComponent comp)
    {
        var dt = (float)(_timing.CurTime - comp.DecayTimeToStart - comp.LastFluxUpdate).TotalSeconds;
        if (dt <= 0)
            return MathHelper.Clamp(comp.CurrentFlux, 0, comp.Capacity); // still clamping it just in case

        float decrease = comp.DecayRate * MathF.Pow(dt, comp.DecayCurve);

        //float decrease = comp.DecayRate * dt;

        return MathHelper.Clamp(comp.CurrentFlux - decrease, 0, comp.Capacity);
    }

    public void UpdateFlux(FluxCoreComponent comp)
    {
        comp.CurrentFlux = GetCurrentFlux(comp);
        comp.LastFluxUpdate = _timing.CurTime;
    }

    public float? AddFlux(GunFluxComponent comp, float flux)
    {
        if (!GetFluxCore(comp.Owner, comp, out var core))
            return null;

        return AddFlux(core, flux);
    }

    public float AddFlux(FluxCoreComponent comp, float flux)
    {
        UpdateFlux(comp);

        if (flux == 0)
            return 0;

        var newFlux = MathHelper.Clamp(comp.CurrentFlux + flux, 0, comp.Capacity);
        var overflow = (comp.CurrentFlux + flux) - newFlux; // also handles undeflow for negative flux gain
        comp.CurrentFlux = newFlux;
        return overflow;
    }

    protected virtual void ApplyOverheatDamage(EntityUid shooter, float damage, string type, TargetBodyPart? bodyPart) { }
    protected virtual void TryMalfunction(EntityUid shooter, EntityUid gun, GunFluxComponent fluxComp, FluxCoreComponent core) { }

}
