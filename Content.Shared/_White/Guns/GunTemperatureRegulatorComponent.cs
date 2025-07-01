using Content.Shared.Atmos;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GunOverheatComponent : Component
{
    /// <summary>
    /// The user will not be able to set the safety above this value.
    /// </summary>
    [DataField]
    public float MaxSafetyTemperature = 9900;

    /// <summary>
    /// Limits the temperature displayed in the weapon's status control. (next to the hand slot)
    /// </summary>
    [DataField]
    public float MaxDisplayTemperature = 9999;

    /// <summary>
    /// If <see cref="SafetyEnabled"/> is true, prevents gun from shooting when above this temperature
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TemperatureLimit = 500;

    [DataField, AutoNetworkedField]
    public float CoolingSpeed = 2;

    /// <summary>
    /// Set to zero to disable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TimeToStartVenting = 5;

    [DataField, AutoNetworkedField]
    public float VentingSpeed = 50;

    [DataField]
    public SoundSpecifier VentingSound = new SoundPathSpecifier("/Audio/Magic/Cults/ClockCult/steam_whoosh.ogg", AudioParams.Default.WithMaxDistance(3.5f).WithVolume(-5));

    [DataField]
    public float VentingFinishedSoundTimeOffset = 1f;

    [DataField]
    public SoundSpecifier VentingFinishedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg", AudioParams.Default.WithMaxDistance(3.5f).WithVolume(-2));

    // used only for playing audio
    public int VentingStage = 2; // starts at 2 so newly created guns don't play sounds


    /// <summary>
    /// Mirrors <see cref="GunComponent.LastFire"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan LastFire;

    /// <summary>
    /// If enabled, prevents the gun from shooting if its hotter than <see cref="TemperatureLimit"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SafetyEnabled = true;

    /// <summary>
    /// If enabled, allows the user to change <see cref="TemperatureLimit"/> and <see cref="SafetyEnabled"/> via altverbs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanChangeSafety = false;

    /// <summary>
    /// Will require an intact lamp in <see cref="LampSlot"/> slot to fire. Will also enable lamp breaking when firing while overheated.
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public bool RequiresLamp = false;

    [DataField, AutoNetworkedField]
    public string LampSlot = "gun-regulator-lamp-slot";

    /// <summary>
    /// Multiplies lamp breaking chance by this value. Each lamp can have it's own safe operating mode, while this value is set per-gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LampBreakChanceMultiplier = 1;

    // prediction n' shiet
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentTemperature = 0;

    /// <summary>
    /// see <see cref="SharedGunOverheatSystem.UpdateTemp(GunOverheatComponent)"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastTempUpdate;

    /// <summary>
    /// How much the gun will heat up (in kelvin, not joules, so the weapon's mass is irrelevant)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatCost = 50;

    [DataField]
    public SoundSpecifier clickUpSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default.WithPitchScale(1.25f));

    [DataField]
    public SoundSpecifier clickSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default);

    [DataField]
    public SoundSpecifier clickDownSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default.WithPitchScale(0.75f));
}
