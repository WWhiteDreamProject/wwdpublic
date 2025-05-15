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
public sealed partial class GunOverheatComponent : Component
{
    /// <summary>
    /// The user will not be able to set the safety above this value.
    /// Also limits the status control's current temperature display.
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaxSafetyTemperature = 2000;

    /// <summary>
    /// If <see cref="SafetyEnabled"/> is true, prevents gun from shooting when above this temperature
    /// Increased by 273.15f upon component init.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float TemperatureLimit = 100;

    /// <summary>
    /// If enabled, prevents the gun from shooting if its hotter than <see cref="TemperatureLimit"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool SafetyEnabled = true;

    /// <summary>
    /// If enabled, allows the user to change <see cref="TemperatureLimit"/> and <see cref="SafetyEnabled"/> via altverbs.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool CanChangeSafety = false;

    /// <summary>
    /// Will require an intact lamp in <see cref="LampSlot"/> slot to fire. Will also enable lamp breaking when firing while overheated.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, AlwaysPushInheritance]
    public bool RequiresLamp = false;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string LampSlot = "gun-regulator-lamp-slot";

    /// <summary>
    /// Multiplies lamp breaking chance by this value. Each lamp can have it's own safe operating mode, while this value is set per-gun.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float LampBreakChanceMultiplier = 1;

    // prediction n' shiet
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentTemperature = Atmospherics.T20C;

    /// <summary>
    /// How much the gun will heat up (in kelvin, not joules, so the weapon's mass is irrelevant)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float HeatCost = 50;
}
