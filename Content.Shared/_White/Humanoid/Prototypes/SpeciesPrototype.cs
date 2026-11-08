using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Colors.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

/// <summary>
/// Represents a species prototype for humanoids.
/// </summary>
[Prototype]
public sealed partial class SpeciesPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Determines if the player can select this species in the character editor.
    /// </summary>
    [DataField]
    public bool SetPreference { get; private set; }

    /// <summary>
    /// Defaults colors for this species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorGroupPrototype>, Color> DefaultColors { get; private set; } = new()
    {
        {"Eyes", Color.Black},
        {"Skin", Color.White}
    };

    /// <summary>
    /// Defaults unary colors for this species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorGroupPrototype>, int> DefaultUnaryColors { get; private set; } = new()
    {
        {"Eyes", 20},
        {"Skin", 20}
    };

    /// <summary>
    /// Prototype IDs for the coloration strategies used by this species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorGroupPrototype>, ProtoId<ColorationStrategyPrototype>> Colorations { get; private set; } = new()
    {
        {"Eyes", "Simple"},
        {"Skin", "Simple"}
    };

    /// <summary>
    /// Entity prototype ID for the dress-up doll used by this species.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Doll { get; private set; }

    /// <summary>
    /// Entity prototype ID for the humanoid variant of this species.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype { get; private set; }

    /// <summary>
    /// The average height in centimeters for this species.
    /// </summary>
    [DataField]
    public float AverageHeight { get; private set; } = 176.1f;

    /// <summary>
    /// The average shoulder-to-shoulder width in centimeters for this species.
    /// </summary>
    [DataField]
    public float AverageWidth { get; private set; } = 40f;

    /// <summary>
    /// The default height for this species.
    /// </summary>
    [DataField]
    public float DefaultHeight { get; private set; } = 1f;

    /// <summary>
    /// The default width for this species.
    /// </summary>
    [DataField]
    public float DefaultWidth { get; private set; } = 1f;

    /// <summary>
    /// The maximum height for this species.
    /// </summary>
    [DataField]
    public float MaxHeight { get; private set; } = 1.1f;

    /// <summary>
    /// The maximum width for this species.
    /// </summary>
    [DataField]
    public float MaxWidth { get; private set; } = 1.1f;

    /// <summary>
    /// The minimum height for this species.
    /// </summary>
    [DataField]
    public float MinHeight { get; private set; } = 0.9f;

    /// <summary>
    /// The minimum width for this species.
    /// </summary>
    [DataField]
    public float MinWidth { get; private set; } = 0.9f;

    /// <summary>
    /// The minimum ratio of height to width for this species.
    /// </summary>
    [DataField]
    public float SizeRatio { get; private set; } = 1.2f;

    /// <summary>
    /// The maximum age for a character.
    /// </summary>
    [DataField]
    public int MaxAge { get; private set; } = 120;

    /// <summary>
    /// The minimum age for a character.
    /// </summary>
    [DataField]
    public int MinAge { get; private set; } = 18;

    /// <summary>
    /// The age above which characters are considered old. Characters between <see cref="YoungAge"/> and <see cref="OldAge"/> are middle-aged.
    /// </summary>
    [DataField]
    public int OldAge { get; private set; } = 60;

    /// <summary>
    /// The number of trait points this species has.
    /// </summary>
    [DataField]
    public int TraitPoints { get; private set; }

    /// <summary>
    /// The age below which characters are considered young. Characters between <see cref="YoungAge"/> and <see cref="OldAge"/> are middle-aged.
    /// </summary>
    [DataField]
    public int YoungAge { get; private set; } = 30;

    /// <summary>
    /// A set of body types for this species.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<BodyTypePrototype>> BodyTypes { get; private set; } = new() { "Normal", };

    /// <summary>
    /// List of possible sexes for this species.
    /// </summary>
    [DataField]
    public HashSet<Sex> Sexes { get; private set; } = new() { Sex.Male, Sex.Female };

    /// <summary>
    /// User-visible name of the species.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// The naming prototype ID for this species.
    /// </summary>
    [DataField]
    public ProtoId<NamingPrototype> Naming { get; private set; } = "";
}
