using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GunOverheatComponent : Component
{
    /// <summary>
    /// The user will not be able to set the safety above this value.
    /// </summary>
    [DataField("maxSafetyTemperature"), AutoNetworkedField]
    public float MaxSafetyTemperatureCelcius { get => MaxSafetyTemperature - 273.15f; set => MaxSafetyTemperature = value + 273.15f; }
    public float MaxSafetyTemperature = 2000 + 273.15f;

    /// <summary>
    /// Limits the temperature displayed in the weapon's status control. (next to the hand slot)
    /// </summary>
    [DataField("maxDisplayTemperature"), AutoNetworkedField]
    public float MaxDisplayTemperatureCelcius { get => MaxDisplayTemperature - 273.15f; set => MaxDisplayTemperature = value + 273.15f; }
    public float MaxDisplayTemperature = 9999 + 273.15f;

    /// <summary>
    /// If <see cref="SafetyEnabled"/> is true, prevents gun from shooting when above this temperature
    /// </summary>
    [DataField("temperatureLimit"), AutoNetworkedField]
    public float TemperatureLimitCelcius { get => TemperatureLimit - 273.15f; set => TemperatureLimit = value + 273.15f; }
    public float TemperatureLimit = 100 + 273.15f;

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
    public float CurrentTemperature = Atmospherics.T20C;

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
