using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Guns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class GunFluxComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool SafetyEnabled = true;

    [DataField, AutoNetworkedField]
    public bool CanChangeSafety = true;

    [DataField, AutoNetworkedField]
    public string CoreSlot = "gun-flux-core-slot";

    /// <summary>
    /// Multiplies lamp breaking chance by this value. Each lamp can have it's own safe operating mode, while this value is set per-gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MalfunctionChanceMultiplier = 1;

    /// <summary>
    /// How much the gun will heat up
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatCost = 1;

    [DataField]
    public Dictionary<string, float> MalfunctionWeightedList = new();

    [DataField]
    public float OverflowDamage = 0.3f;

    [DataField]
    public float UnderflowDamage = 1.0f;

    [DataField]
    public float MaxOverflowDamage = 10f;

    [DataField]
    public float MaxUnderflowDamage = 10f;

    [DataField]
    public string OverflowDamageType = "Heat";

    [DataField]
    public string UnderflowDamageType = "Heat"; // cold damage does not work as expected, todo see why

    [DataField]
    public string OverflowDamageMessage = "gun-overheat-hurt-hot";

    [DataField]
    public string UnderflowDamageMessage = "gun-overheat-hurt-cold";

    [DataField]
    public SoundSpecifier OverflowDamageSound = new SoundCollectionSpecifier("MeatLaserImpact", AudioParams.Default.WithVolume(4f));

    [DataField]
    public SoundSpecifier UnderflowDamageSound = new SoundCollectionSpecifier("MeatLaserImpact", AudioParams.Default.WithVolume(4f));

    [DataField]
    public SoundSpecifier ToggleSafetySound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default);
}

