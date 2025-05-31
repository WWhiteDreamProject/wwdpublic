using System.Diagnostics.CodeAnalysis;
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
    [Dependency] protected readonly ItemSlotsSystem Slots = default!;
    [Dependency] protected readonly IRobustRandom Rng = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunOverheatComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunOverheatComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunOverheatComponent, ExaminedEvent>(OnGunExamined);
        SubscribeLocalEvent<GunOverheatComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);

        SubscribeLocalEvent<RegulatorLampComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<RegulatorLampComponent, ExaminedEvent>(OnLampExamined);
    }

    private void OnAttemptShoot(Entity<GunOverheatComponent> gun, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (gun.Comp.CurrentTemperature >= gun.Comp.TemperatureLimit && gun.Comp.SafetyEnabled)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-temperature-limit-exceeded-popup");
            return;
        }

        if (!gun.Comp.RequiresLamp)
            return;

        if (!GetLamp(gun, out var lamp))
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-lamp-missing-popup");
            return;
        }

        if (!lamp.Value.Comp.Intact)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("gun-regulator-lamp-broken-popup");
        }
    }

    protected virtual void OnGunShot(Entity<GunOverheatComponent> gun, ref GunShotEvent args)
    {
        if (Timing.IsFirstTimePredicted)
            gun.Comp.CurrentTemperature += gun.Comp.HeatCost;
    }

    private void OnGunExamined(Entity<GunOverheatComponent> gun, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString(
                $"gun-regulator-examine-safety{(gun.Comp.CanChangeSafety ? "-toggleable" : "")}",
                ("enabled", gun.Comp.SafetyEnabled), ("limit", MathF.Round(gun.Comp.TemperatureLimit - 273.15f))));
        if (!gun.Comp.RequiresLamp)
            return;

        var lampStatus = 0; // missing
        if (GetLamp(gun, out var lamp))
            lampStatus = lamp.Value.Comp.Intact ? 2 : 1; // present : broken
        args.PushMarkup(Loc.GetString("gun-regulator-examine-lamp", ("lampstatus", lampStatus)));
    }

    private void OnAltVerbs(Entity<GunOverheatComponent> gun, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess || !gun.Comp.CanChangeSafety)
            return;
        var player = args.User;

        AddVerb(-1, "fireselector-100up-verb", ref args, () => AdjustSafety(gun.Comp, 100, player));
        AddVerb(-2, "fireselector-10up-verb", ref args, () => AdjustSafety(gun.Comp, 10, player));
        AddVerb(-3, "fireselector-toggle-verb", ref args, () => ToggleSafety(gun.Comp, player));
        AddVerb(-4, "fireselector-10down-verb", ref args, () => AdjustSafety(gun.Comp, -10, player));
        AddVerb(-5, "fireselector-100down-verb", ref args, () => AdjustSafety(gun.Comp, -100, player));
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
            if (!Timing.IsFirstTimePredicted)
                return;
            Audio.PlayPredicted(T >= 0 ? heat.clickUpSound : heat.clickDownSound, gun, user);
            AdjustTemperatureLimit(heat, T);
        }

        void ToggleSafety(GunOverheatComponent heat, EntityUid user)
        {
            if (!Timing.IsFirstTimePredicted)
                return;
            Audio.PlayPredicted(heat.clickSound, gun, user);
            heat.SafetyEnabled = !heat.SafetyEnabled;
        }
    }

    private void OnBreak(Entity<RegulatorLampComponent> lamp, ref BreakageEventArgs args)
    {
        Appearance.SetData(lamp, RegulatorLampGlass.Intact, false);
        lamp.Comp.Intact = false;
        Dirty(lamp);
    }

    private void OnLampExamined(Entity<RegulatorLampComponent> lamp, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-regulator-lamp-examine-intact", ("intact", lamp.Comp.Intact)));
        args.PushMarkup(
            Loc.GetString(
                "gun-regulator-lamp-examine-temperature-range",
                ("safetemp", MathF.Round(lamp.Comp.SafeTemperature - 273.15f)),
                ("unsafetemp", MathF.Round(lamp.Comp.UnsafeTemperature - 273.15f))));
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
    public bool GetLamp(Entity<GunOverheatComponent> gun, [NotNullWhen(true)] out Entity<RegulatorLampComponent>? lamp)
    {
        lamp = null;
        if (!TryComp<ItemSlotsComponent>(gun, out var slotComp) ||
            !Slots.TryGetSlot(gun, gun.Comp.LampSlot, out var slot, slotComp) ||
            !TryComp(slot.Item, out RegulatorLampComponent? comp))
            return false;

        lamp = (slot.Item.Value, comp);
        return true;
    }

    protected void BurnoutLamp(Entity<RegulatorLampComponent> lamp, EntityUid? shooter = null)
    {
        Audio.PlayEntity(lamp.Comp.BreakSound, Filter.Pvs(lamp), lamp, true);
        Appearance.SetData(lamp, RegulatorLampFilament.Intact, false);
        lamp.Comp.Intact = false;
        Dirty(lamp);
    }

    public float GetLampBreakChance(float temp, RegulatorLampComponent comp, float multiplier = 1)
    {
        return MathHelper.Clamp01(
            (temp - comp.SafeTemperature) / (comp.UnsafeTemperature - comp.SafeTemperature) * multiplier);
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
