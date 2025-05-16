using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Maths;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns;

/// <summary>
/// Handles gun overheating mechanics.
/// TODO - decide whether or not i should separate lamp handling into a different system or is it good enough as it is
/// </summary>
public abstract class SharedGunTemperatureRegulatorSystem : EntitySystem
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

        SubscribeLocalEvent<RegulatorLampComponent, ComponentInit>(OnLampInit);
        SubscribeLocalEvent<GunOverheatComponent, ComponentInit>(OnGunInit);
    }

    private void OnLampInit(EntityUid uid, RegulatorLampComponent comp, ComponentInit args)
    {
        comp.SafeTemperature += 273.15f; // celcius in prototypes, kelvin at runtime
        comp.UnsafeTemperature += 273.15f; // celcius in prototypes, kelvin at runtime
    }

    private void OnGunInit(EntityUid uid, GunOverheatComponent comp, ComponentInit args)
    {
        comp.TemperatureLimit += 273.15f; // celcius in prototypes, kelvin at runtime
        comp.MaxSafetyTemperature += 273.15f; // celcius in prototypes, kelvin at runtime
    }

    private void OnLampExamined(EntityUid uid, RegulatorLampComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("gun-regulator-lamp-examine-intact", ("intact", comp.Intact)));
        if (!args.IsInDetailsRange)
            return;
        args.PushMarkup(Loc.GetString("gun-regulator-lamp-examine-temperature-range", ("safetemp", MathF.Round(comp.SafeTemperature-273.15f)), ("unsafetemp", MathF.Round(comp.UnsafeTemperature-273.15f))));
    }

    private void OnGunExamined(EntityUid uid, GunOverheatComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString($"gun-regulator-examine-safety{(comp.CanChangeSafety ? "-toggleable" : "")}", ("enabled", comp.SafetyEnabled), ("limit", MathF.Round(comp.TemperatureLimit - 273.15f))));
        if(comp.RequiresLamp)
        {
            int lampStatus = 0; // missing
            if(GetLamp(uid, out var lamp, comp) && lamp is not null)
                lampStatus = lamp.Intact ? 2 : 1; // present : broken
            args.PushMarkup(Loc.GetString($"gun-regulator-examine-lamp", ("lampstatus", lampStatus)));
        }
    }

    private void OnBreak(EntityUid uid, RegulatorLampComponent comp, BreakageEventArgs args)
    {
        _appearance.SetData(uid, RegulatorLampVisuals.Glass, RegulatorLampState.Broken);
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

        if (comp.RequiresLamp)
        {
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
    }

    protected virtual void OnGunShot(EntityUid uid, GunOverheatComponent comp, ref GunShotEvent args)
    {
        if(_timing.IsFirstTimePredicted)
            comp.CurrentTemperature += comp.HeatCost;
    }

    public void AdjustTemperatureLimit(GunOverheatComponent comp, float tempChange)
    {
        comp.TemperatureLimit = MathHelper.Clamp(comp.TemperatureLimit + tempChange, -250f + 273.15f , comp.MaxSafetyTemperature); // from -250C to MaxSafetyTemperature 
    }

    private static SoundSpecifier clickUpSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default.WithPitchScale(1.25f));
    private static SoundSpecifier clickSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default);
    private static SoundSpecifier clickDownSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default.WithPitchScale(0.75f));
    private void OnAltVerbs(EntityUid uid, GunOverheatComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess || !comp.CanChangeSafety)
            return;

        AddVerb(-1, "fireselector-100up-verb", () => { if(!_timing.IsFirstTimePredicted) return; _audio.PlayPredicted(clickUpSound, uid, args.User); AdjustTemperatureLimit(comp, 100); });
        AddVerb(-2, "fireselector-10up-verb", () => { if(!_timing.IsFirstTimePredicted) return; _audio.PlayPredicted(clickUpSound, uid, args.User); AdjustTemperatureLimit(comp, 10); });
        AddVerb(-3, "fireselector-toggle-verb", () => { if(!_timing.IsFirstTimePredicted) return; _audio.PlayPredicted(clickSound, uid, args.User); comp.SafetyEnabled = !comp.SafetyEnabled; });
        AddVerb(-4, "fireselector-10down-verb", () => { if(!_timing.IsFirstTimePredicted) return; _audio.PlayPredicted(clickDownSound, uid, args.User); AdjustTemperatureLimit(comp, -10); });
        AddVerb(-5, "fireselector-100down-verb", () => { if (!_timing.IsFirstTimePredicted) return; _audio.PlayPredicted(clickDownSound, uid, args.User); AdjustTemperatureLimit(comp, -100); });

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

    }

    /// <summary>
    /// Returns false if called on something without GunTemperatureRegulatorComponent.
    /// Otherwise returns true.
    /// </summary>
    public bool GetLamp(EntityUid gunUid,  out RegulatorLampComponent? lampComp, GunOverheatComponent? comp = null)
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
        _appearance.SetData(lampUid, RegulatorLampVisuals.Shtuka, RegulatorLampState.Broken);
        comp.Intact = false;
        Dirty(lampUid, comp);
    }

    public float GetLampBreakChance(float temp, RegulatorLampComponent comp) => MathHelper.Clamp01((temp - comp.SafeTemperature) / (comp.UnsafeTemperature - comp.SafeTemperature));
    public float GetLampBreakChance(float temp, float multiplier, RegulatorLampComponent comp) => MathHelper.Clamp01((temp - comp.SafeTemperature) / (comp.UnsafeTemperature - comp.SafeTemperature) * multiplier);
}


[Serializable, NetSerializable]
public enum RegulatorLampVisuals
{
    Glass,
    Shtuka
}

[Serializable, NetSerializable]
public enum RegulatorLampState
{
    Intact,
    Broken
}
