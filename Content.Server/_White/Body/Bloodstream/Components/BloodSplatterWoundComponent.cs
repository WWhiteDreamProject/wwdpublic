using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Body.Bloodstream.Components;

[RegisterComponent]
public sealed partial class BloodSplatterWoundComponent : Component
{
    /// <summary>
    /// The maximum spread angle defining the cone in which emitted splatters will disperse relative to the damage direction.
    /// </summary>
    [DataField]
    public Angle EmissionSpreadAngle = Angle.FromDegrees(50);

    /// <summary>
    /// The prototype ID for the splatter entity (e.g., blood splatter) that will be emitted upon taking damage.
    /// </summary>
    [DataField]
    public EntProtoId SplatterPrototype = "BloodSplatterBluntParticle";

    /// <summary>
    /// The minimum damage threshold required for any splatter emission to occur.
    /// </summary>
    [DataField]
    public FixedPoint2 MinDamageThreshold = 7;

    /// <summary>
    /// The damage threshold at which splatter emission is guaranteed or reaches maximum probability/count.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxDamageThreshold = 25;

    /// <summary>
    /// The maximum volume that a single emitted splatter can contain.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxSplatterVolume = 2;

    /// <summary>
    /// A coefficient determining what fraction of the received damage is converted into splatters emitted distance.
    /// </summary>
    [DataField]
    public float DamageToDistanceFactor = 0.2f;

    /// <summary>
    /// A coefficient determining what fraction of the received damage is converted into the total volume of emitted splatters (liquid/blood).
    /// </summary>
    [DataField]
    public float DamageToVolumeFactor = 0.17f;

    /// <summary>
    /// A coefficient that determines the possible difference in the emission range of splatters.
    /// </summary>
    [DataField]
    public float EmissionSpreadDistance = 2.5f;

    /// <summary>
    /// A coefficient that determines the possible difference in the total volume of emitted splatters.
    /// </summary>
    [DataField]
    public float EmissionSpreadVolume = 2f;

    /// <summary>
    /// The maximum total count of splatters that can be emitted during a single damage event.
    /// </summary>
    [DataField]
    public int MaxSplatterCount = 3;

    /// <summary>
    /// The sound specifier played when this wound emitted splatter.
    /// </summary>
    [DataField]
    public SoundSpecifier? SplatterSound = new SoundCollectionSpecifier("BloodSplatter", AudioParams.Default.WithVariation(0.25f).WithMaxDistance(6f));

    /// <summary>
    /// Determines how the splatters will be emitted, randomly or in a line.
    /// </summary>
    [DataField]
    public SplatterType SplatterType = SplatterType.Random;
}

[Serializable]
public enum SplatterType : byte
{
    Random,
    Line
}
