using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Examine;
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
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._White.Guns;

/// <summary>
/// Handles gun overheating mechanics.
/// TODO - decide whether or not i should separate lamp handling into a different system or is it good enough as it is
/// </summary>
public abstract class SharedGunOverheatSystem : EntitySystem
{
    [Dependency] protected readonly ItemSlotsSystem _slots = default!;
    [Dependency] protected readonly IRobustRandom _rng = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunOverheatComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunOverheatComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunOverheatComponent, ExaminedEvent>(OnGunExamined);
        SubscribeLocalEvent<GunOverheatComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);

        SubscribeLocalEvent<RegulatorLampComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<RegulatorLampComponent, ExaminedEvent>(OnLampExamined);
    }

    private void OnAttemptShoot(EntityUid uid, GunOverheatComponent comp, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (GetCurrentTemperature(comp) >= comp.TemperatureLimit && comp.SafetyEnabled)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-temperature-limit-exceeded-popup");
            return;
        }

        if (!comp.RequiresLamp)
            return;

        if (!GetLamp(uid, comp, out var lamp))
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-lamp-missing-popup");
            return;
        }

        if (!lamp.Intact)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-lamp-broken-popup");
        }
    }

    protected virtual void OnGunShot(EntityUid uid, GunOverheatComponent comp, ref GunShotEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        UpdateTemp(comp);
        comp.VentingStage = 0;
        comp.LastFire = _timing.CurTime;
        comp.CurrentTemperature += comp.HeatCost;
    }

    private void OnGunExamined(EntityUid uid, GunOverheatComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString(
                $"gun-regulator-examine-safety{(comp.CanChangeSafety ? "-toggleable" : "")}",
                ("enabled", comp.SafetyEnabled), ("limit", MathF.Round(comp.TemperatureLimit - 273.15f))));
        if (!comp.RequiresLamp)
            return;

        var lampStatus = 0; // missing
        if (GetLamp(uid, comp, out var lamp))
            lampStatus = lamp.Intact ? 2 : 1; // present : broken
        args.PushMarkup(Loc.GetString("gun-regulator-examine-lamp", ("lampstatus", lampStatus)));
    }

    private void OnAltVerbs(EntityUid uid, GunOverheatComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess || !comp.CanChangeSafety)
            return;
        var player = args.User;

        AddVerb(-1, "fireselector-100up-verb", ref args, () => AdjustSafety(comp, 100, player));
        AddVerb(-2, "fireselector-10up-verb", ref args, () => AdjustSafety(comp, 10, player));
        AddVerb(-3, "fireselector-toggle-verb", ref args, () => ToggleSafety(comp, player));
        AddVerb(-4, "fireselector-10down-verb", ref args, () => AdjustSafety(comp, -10, player));
        AddVerb(-5, "fireselector-100down-verb", ref args, () => AdjustSafety(comp, -100, player));
        return;

        void AddVerb(int priority, string text, ref GetVerbsEvent<AlternativeVerb> args, Action act) =>
            args.Verbs.Add(
                new()
                {
                    Category = VerbCategory.Safety,
                    Priority = priority,
                    CloseMenu = false,
                    DoContactInteraction = true,
                    Text = Loc.GetString(text),
                    Act = act
                });

        void AdjustSafety(GunOverheatComponent heat, float T, EntityUid user)
        {
            if (!_timing.IsFirstTimePredicted)
                return;
            _audio.PlayPredicted(T >= 0 ? heat.clickUpSound : heat.clickDownSound, heat.Owner, user);
            AdjustTemperatureLimit(heat, T);
        }

        void ToggleSafety(GunOverheatComponent heat, EntityUid user)
        {
            if (!_timing.IsFirstTimePredicted)
                return;
            _audio.PlayPredicted(heat.clickSound, heat.Owner, user);
            heat.SafetyEnabled = !heat.SafetyEnabled;
        }
    }

    private void OnBreak(EntityUid uid, RegulatorLampComponent comp, BreakageEventArgs args)
    {
        _appearance.SetData(uid, RegulatorLampGlass.Intact, false);
        comp.Intact = false;
        Dirty(uid, comp);
    }

    private void OnLampExamined(EntityUid uid, RegulatorLampComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-regulator-lamp-examine-intact", ("intact", comp.Intact)));
        args.PushMarkup(
            Loc.GetString(
                "gun-regulator-lamp-examine-temperature-range",
                ("safetemp", MathF.Round(comp.SafeTemperature - 273.15f)),
                ("unsafetemp", MathF.Round(comp.UnsafeTemperature - 273.15f))));
    }

    public void AdjustTemperatureLimit(GunOverheatComponent comp, float tempChange)
    {
        comp.TemperatureLimit = MathHelper.Clamp(
            comp.TemperatureLimit + tempChange, -250f + 273.15f,
            comp.MaxSafetyTemperature); // from -250C to MaxSafetyTemperature
    }

    /// <summary>
    /// Returns false if called on something without GunTemperatureRegulatorComponent.
    /// Otherwise returns true.
    /// </summary>
    public bool GetLamp(EntityUid uid, GunOverheatComponent comp, [NotNullWhen(true)] out RegulatorLampComponent? lamp)
    {
        lamp = null;
        if (!TryComp<ItemSlotsComponent>(uid, out var slotComp) ||
            !_slots.TryGetSlot(uid, comp.LampSlot, out var slot, slotComp) ||
            !TryComp(slot.Item, out lamp))
            return false;

        return true;
    }

    protected void BurnoutLamp(RegulatorLampComponent lamp, EntityUid? shooter = null)
    {
        var uid = lamp.Owner;
        _audio.PlayEntity(lamp.BreakSound, Filter.Pvs(uid), uid, true);
        _appearance.SetData(uid, RegulatorLampFilament.Intact, false);
        lamp.Intact = false;
        Dirty(uid, lamp);
    }

    public float GetLampBreakChance(float temp, RegulatorLampComponent comp, float multiplier = 1)
    {
        return MathHelper.Clamp01(
            (temp - comp.SafeTemperature) / (comp.UnsafeTemperature - comp.SafeTemperature) * multiplier);
    }

    public float GetCurrentTemperature(GunOverheatComponent comp)
    {
        var time = (float)(_timing.CurTime - comp.LastTempUpdate).TotalSeconds;
        var timeSinceShot = (float)(_timing.CurTime - comp.LastFire).TotalSeconds;
        var ventTime = comp.TimeToStartVenting;
        if (timeSinceShot < ventTime)
            return MathF.Max(0, comp.CurrentTemperature - time * comp.CoolingSpeed);
        return MathF.Max(0, comp.CurrentTemperature - ventTime * comp.CoolingSpeed - (time - ventTime) * comp.VentingSpeed);
    }

    protected void UpdateTemp(GunOverheatComponent comp)
    {
        comp.CurrentTemperature = GetCurrentTemperature(comp);
        comp.LastTempUpdate = _timing.CurTime;
    }
}

[Serializable, NetSerializable]
public enum RegulatorLampGlass : byte
{
    Layer,
    Intact
}

[Serializable, NetSerializable]
public enum RegulatorLampFilament : byte
{
    Layer,
    Intact
}
