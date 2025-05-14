using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
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
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GunTemperatureRegulatorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TemperatureLimit = 450;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool SafetyEnabled = true;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool RequiresLamp = true;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string LampSlot = "gun-regulator-lamp-slot";

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float LampBreakChanceMultiplier = 1;

    // prediction n' shiet
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentTemperature = Atmospherics.T20C;
    
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float HeatCost = 50;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunRegulatorLampComponent : Component
{
    /// <summary>
    /// Temperature below which the lamp is guaranteed to work
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SafeTemperature = 450;

    /// <summary>
    /// Temperature at or above which the lamp is guaranteed to break immediately after shooting
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float UnsafeTemperature = 750;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Intact = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BreakSound = new SoundCollectionSpecifier("GlassBreak");


}

public abstract class SharedGunTemperatureRegulatorSystem : EntitySystem
{
    [Dependency] protected readonly ItemSlotsSystem _slots = default!;
    [Dependency] protected readonly IRobustRandom _rng = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunTemperatureRegulatorComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunTemperatureRegulatorComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunRegulatorLampComponent, BreakageEventArgs>(OnBreak);
    }

    private void OnBreak(EntityUid uid, GunRegulatorLampComponent comp, BreakageEventArgs args)
    {
        _appearance.SetData(uid, RegulatorLampVisuals.Glass, RegulatorLampState.Broken);
        comp.Intact = false;
        Dirty(uid, comp);
    }

    private void OnAttemptShoot(EntityUid uid, GunTemperatureRegulatorComponent comp, ref AttemptShootEvent args)
    {
        // instead of implementing SharedTemperatureComponent and being forced to copy this code to both server- and client-side separately,
        // i'll just pull the temperature value off of BatteryAmmoProvider, where it is replicated by server and used for prediction purposes.
        // Yes, this is awful. No, i will not take blame. Blame whoever thought it was a good idea to make temperature fully server-sided.
        if (args.Cancelled)
            return;

        if (comp.CurrentTemperature >= comp.TemperatureLimit && comp.SafetyEnabled)
        {
            args.Cancelled = true;
            return;
        }

        if (comp.RequiresLamp)
        {
            if (!_slots.TryGetSlot(uid, comp.LampSlot, out var slot) ||
                !TryComp<GunRegulatorLampComponent>(slot.Item, out var lampComp) ||
                !lampComp.Intact)
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    protected virtual void OnGunShot(EntityUid uid, GunTemperatureRegulatorComponent comp, ref GunShotEvent args)
    {
        if(IoCManager.Resolve<IGameTiming>().IsFirstTimePredicted)
            comp.CurrentTemperature += comp.HeatCost;
    }

    public bool GetLamp(EntityUid gunUid,  out GunRegulatorLampComponent? lampComp, GunTemperatureRegulatorComponent? comp = null)
    {
        lampComp = null;
        if (!Resolve(gunUid, ref comp))
            return false;

        if (_slots.TryGetSlot(gunUid, comp.LampSlot, out var slot))
            TryComp<GunRegulatorLampComponent>(slot.Item, out lampComp);
        return true;
    }

    protected void BurnoutLamp(GunRegulatorLampComponent comp, EntityUid? shooter = null)
    {
        var lampUid = comp.Owner;
        _audio.PlayEntity(comp.BreakSound, Filter.Pvs(lampUid), lampUid, true);
        _appearance.SetData(lampUid, RegulatorLampVisuals.Shtuka, RegulatorLampState.Broken);
        comp.Intact = false;
        Dirty(lampUid, comp);
    }
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
    BurnedOut,
    Broken
}
