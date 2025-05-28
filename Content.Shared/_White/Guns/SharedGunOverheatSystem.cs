using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

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
        SubscribeLocalEvent<RegulatorLampComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<RegulatorLampComponent, ExaminedEvent>(OnLampExamined);
        SubscribeLocalEvent<GunOverheatComponent, ExaminedEvent>(OnGunExamined);
        SubscribeLocalEvent<GunOverheatComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
    }

    private void OnLampExamined(EntityUid uid, RegulatorLampComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("gun-regulator-lamp-examine-intact", ("intact", comp.Intact)));

        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-regulator-lamp-examine-temperature-range", ("safetemp", MathF.Round(comp.SafeTemperature - 273.15f)), ("unsafetemp", MathF.Round(comp.UnsafeTemperature - 273.15f))));
    }

    private void OnGunExamined(EntityUid uid, GunOverheatComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString($"gun-regulator-examine-safety{(comp.CanChangeSafety ? "-toggleable" : "")}", ("enabled", comp.SafetyEnabled), ("limit", MathF.Round(comp.TemperatureLimit - 273.15f))));
        if (comp.RequiresLamp)
        {
            int lampStatus = 0; // missing
            if (GetLamp(uid, out var lamp, comp) && lamp is not null)
                lampStatus = lamp.Intact ? 2 : 1; // present : broken
            args.PushMarkup(Loc.GetString($"gun-regulator-examine-lamp", ("lampstatus", lampStatus)));
        }
    }

    private void OnBreak(EntityUid uid, RegulatorLampComponent comp, BreakageEventArgs args)
    {
        /*_appearance.SetData(uid, RegulatorLampVisuals.Glass, RegulatorLampState.Broken);*/
        comp.Intact = false;
        Dirty(uid, comp);
    }

    private void OnAttemptShoot(EntityUid uid, GunOverheatComponent comp, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.CurrentTemperature >= comp.TemperatureLimit && comp.SafetyEnabled)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-temperature-limit-exceeded-popup");
            return;
        }

        if (!comp.RequiresLamp)
            return;

        if (GetLamp(uid, out var lampComp, comp) && lampComp is not null)
        {
            if (lampComp.Intact)
                return;

            args.Cancelled = true;
            args.Message = Loc.GetString($"gun-regulator-lamp-broken-popup");
            return;
        }

        args.Cancelled = true;
        args.Message = Loc.GetString($"gun-regulator-lamp-missing-popup");
        return;
    }

    protected virtual void OnGunShot(EntityUid uid, GunOverheatComponent comp, ref GunShotEvent args)
    {
        if (_timing.IsFirstTimePredicted)
            comp.CurrentTemperature += comp.HeatCost;
    }

    public void AdjustTemperatureLimit(GunOverheatComponent comp, float tempChange)
    {
        comp.TemperatureLimit = MathHelper.Clamp(comp.TemperatureLimit + tempChange, -250f + 273.15f, comp.MaxSafetyTemperature); // from -250C to MaxSafetyTemperature
    }

    private void OnAltVerbs(EntityUid uid, GunOverheatComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess || !comp.CanChangeSafety)
            return;

        AddVerb(-1, "fireselector-100up-verb", () => _adjustSafety(comp, 100));
        AddVerb(-2, "fireselector-10up-verb", () => _adjustSafety(comp, 10));
        AddVerb(-3, "fireselector-toggle-verb", () => _toggleSafety(comp));
        AddVerb(-4, "fireselector-10down-verb", () => _adjustSafety(comp, -10));
        AddVerb(-5, "fireselector-100down-verb", () => _adjustSafety(comp, -100));

        void AddVerb(int priority, string text, Action act)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Category = VerbCategory.Safety,
                Priority = priority,
                CloseMenu = false,
                DoContactInteraction = true,
                Text = Loc.GetString(text),
                Act = act,
            });
        }

        void _adjustSafety(GunOverheatComponent comp, float T)
        {
            if (!_timing.IsFirstTimePredicted)
                return;
            _audio.PlayPredicted(T >= 0 ? comp.clickUpSound : comp.clickDownSound, uid, args.User);
            AdjustTemperatureLimit(comp, T);
        }

        void _toggleSafety(GunOverheatComponent comp)
        {
            if (!_timing.IsFirstTimePredicted)
                return;
            _audio.PlayPredicted(comp.clickSound, uid, args.User);
            comp.SafetyEnabled = !comp.SafetyEnabled;
        }
    }

    /// <summary>
    /// Returns false if called on something without GunTemperatureRegulatorComponent.
    /// Otherwise returns true.
    /// </summary>
    public bool GetLamp(EntityUid gunUid, out RegulatorLampComponent? lampComp, GunOverheatComponent? comp = null)
    {
        lampComp = null;
        if (!Resolve(gunUid, ref comp))
            return false;

        if (TryComp<ItemSlotsComponent>(gunUid, out var slotComp) && _slots.TryGetSlot(gunUid, comp.LampSlot, out var slot, slotComp))
            TryComp(slot.Item, out lampComp);
        return true;
    }

    protected void BurnoutLamp(RegulatorLampComponent comp, EntityUid? shooter = null)
    {
        var lampUid = comp.Owner;
        _audio.PlayEntity(comp.BreakSound, Filter.Pvs(lampUid), lampUid, true);
        /*_appearance.SetData(lampUid, RegulatorLampVisuals.Filament, RegulatorLampState.Broken);*/
        comp.Intact = false;
        Dirty(lampUid, comp);
    }

    public float GetLampBreakChance(float temp, RegulatorLampComponent comp, float multiplier = 1) => MathHelper.Clamp01((temp - comp.SafeTemperature) / (comp.UnsafeTemperature - comp.SafeTemperature) * multiplier);
}


// I do not know why, but it refuses to work on the server. TODO: return it
/*
[Serializable, NetSerializable]
public enum RegulatorLampVisuals : byte
{
    Glass,
    Filament
}

[Serializable, NetSerializable]
public enum RegulatorLampLayers : byte
{
    Glass,
    Filament
}

[Serializable, NetSerializable]
public enum RegulatorLampState : byte
{
    Intact,
    Broken
}
*/
