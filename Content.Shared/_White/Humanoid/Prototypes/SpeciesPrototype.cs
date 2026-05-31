using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Prototypes;

/// <summary>
/// Represents a species prototype for humanoids.
/// </summary>
[Prototype]
public sealed class SpeciesPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Determines if the player can select this species in the character editor.
    /// </summary>
    [DataField]
    public bool SetPreference { get; }

    /// <summary>
    /// Defaults tone for this species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorationPrototype>, Color> DefaultTone { get; } = new()
    {
        {"Skin", Color.White},
        {"Eye", Color.Black},
    };

    /// <summary>
    /// Defaults unary tone for this species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorationPrototype>, int> DefaultUnaryTone { get; } = new()
    {
        {"Skin", 20},
        {"Eye", 20},
    };

    /// <summary>
    /// Prototype IDs for the coloration method used by this species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<BodyColorationPrototype>, ProtoId<ColorationPrototype>> Coloration { get; } = new()
    {
        {"Skin", "HumanToned"},
        {"Eye", "All"},
    };

    /// <summary>
    /// Entity prototype ID for the dress-up doll used by this species.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId DollPrototype { get; }

    /// <summary>
    /// Entity prototype ID for the humanoid variant of this species.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype { get; }

    /// <summary>
    /// The average height in centimeters for this species.
    /// </summary>
    [DataField]
    public float AverageHeight { get; } = 176.1f;

    /// <summary>
    /// The average shoulder-to-shoulder width in centimeters for this species.
    /// </summary>
    [DataField]
    public float AverageWidth { get; } = 40f;

    /// <summary>
    /// The default height for this species.
    /// </summary>
    [DataField]
    public float DefaultHeight { get; } = 1f;

    /// <summary>
    /// The default width for this species.
    /// </summary>
    [DataField]
    public float DefaultWidth { get; } = 1f;

    /// <summary>
    /// The maximum height for this species.
    /// </summary>
    [DataField]
    public float MaxHeight { get; } = 1.1f;

    /// <summary>
    /// The maximum width for this species.
    /// </summary>
    [DataField]
    public float MaxWidth { get; } = 1.1f;

    /// <summary>
    /// The minimum height for this species.
    /// </summary>
    [DataField]
    public float MinHeight { get; } = 0.9f;

    /// <summary>
    /// The minimum width for this species.
    /// </summary>
    [DataField]
    public float MinWidth { get; } = 0.9f;

    /// <summary>
    /// The minimum ratio of height to width for this species.
    /// </summary>
    [DataField]
    public float SizeRatio { get; } = 1.2f;

    /// <summary>
    /// The maximum age for a character.
    /// </summary>
    [DataField]
    public int MaxAge { get; } = 120;

    /// <summary>
    /// The minimum age for a character.
    /// </summary>
    [DataField]
    public int MinAge { get; } = 18;

    /// <summary>
    /// The age above which characters are considered old. Characters between <see cref="YoungAge"/> and <see cref="OldAge"/> are middle-aged.
    /// </summary>
    [DataField]
    public int OldAge { get; } = 60;

    /// <summary>
    /// The number of trait points this species has.
    /// </summary>
    [DataField]
    public int TraitPoints { get; }

    /// <summary>
    /// The age below which characters are considered young. Characters between <see cref="YoungAge"/> and <see cref="OldAge"/> are middle-aged.
    /// </summary>
    [DataField]
    public int YoungAge { get; } = 30;

    /// <summary>
    /// A set of body types for this species.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<BodyTypePrototype>> BodyTypes { get; } = new() { "Normal", };

    /// <summary>
    /// List of possible sexes for this species.
    /// </summary>
    [DataField]
    public HashSet<Sex> Sexes { get; } = new() { Sex.Male, Sex.Female };

    /// <summary>
    /// User-visible name of the species.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; }

    /// <summary>
    /// The naming prototype ID for this species.
    /// </summary>
    [DataField]
    public ProtoId<NamingPrototype> Naming { get; } = "";
}
