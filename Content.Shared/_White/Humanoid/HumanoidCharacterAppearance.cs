using Content.Shared._White.Body.Components;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.SkinColoration;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : IEquatable<HumanoidCharacterAppearance>
{
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public Dictionary<ProtoId<MarkingCategoryPrototype>, Dictionary<Enum, List<Marking>>> Markings { get; set; } = new();

    [DataField]
    public Dictionary<string, EntProtoId> BodyParts { get; set; } = new();

    public HumanoidCharacterAppearance(
        Color eyeColor,
        Color skinColor,
        Dictionary<ProtoId<MarkingCategoryPrototype>, Dictionary<Enum, List<Marking>>> markings,
        Dictionary<string, EntProtoId> bodyParts
        )
    {
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        BodyParts = bodyParts;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.EyeColor, other.SkinColor, new(other.Markings), new(other.BodyParts)) { }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(newColor, SkinColor, Markings, BodyParts);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(EyeColor, newColor, Markings, BodyParts);
    }

    public HumanoidCharacterAppearance WithMarkings(Dictionary<ProtoId<MarkingCategoryPrototype>, Dictionary<Enum, List<Marking>>> newMarkings)
    {
        return new(EyeColor, SkinColor, newMarkings, BodyParts);
    }

    public HumanoidCharacterAppearance WithBodyParts(Dictionary<string, EntProtoId> bodyParts)
    {
        return new(EyeColor, SkinColor, Markings, bodyParts);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var speciesPrototype = prototypeManager.Index(species);
        var skinColoration = prototypeManager.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        var appearance = new HumanoidCharacterAppearance(
            Color.Black,
            skinColor,
            new(),
            new()
        );
        return EnsureValid(appearance, species, sex);
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black,
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        // TODO: Add random markings

        var newEyeColor = random.Pick(_realisticEyeColors);

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var skinType = protoMan.Index<SpeciesPrototype>(species).SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

        return new (newEyeColor, newSkinColor, new(), new());
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var eyeColor = ClampColor(appearance.EyeColor);

        var markingManager = IoCManager.Resolve<MarkingManager>();
        var componentFactory = IoCManager.Resolve<IComponentFactory>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

        var skinColor = appearance.SkinColor;
        var validatedMarkings = appearance.Markings.ShallowClone();
        var validatedBodyParts = appearance.BodyParts.ShallowClone();

        foreach (var (bodyPartId, bodyPart) in appearance.BodyParts)
        {
            if (prototypeManager.HasIndex(bodyPart))
                continue;

            validatedBodyParts.Remove(bodyPartId);
        }

        if (!prototypeManager.TryIndex(species, out var speciesPrototype))
            return new (eyeColor, skinColor, validatedMarkings, validatedBodyParts);

        var strategy = prototypeManager.Index(speciesPrototype.SkinColoration).Strategy;
        skinColor = strategy.EnsureVerified(skinColor);

        var bodyParts = new List<EntProtoId>();

        var dollPrototype = prototypeManager.Index(speciesPrototype.DollPrototype);
        if (dollPrototype.TryGetComponent<BodyComponent>(out var body, componentFactory))
        {
            var bodyPrototype = prototypeManager.Index(body.Prototype);

            foreach (var bodyPart in bodyPrototype.BodyParts.Values)
            {
                if (!bodyPart.StartingBodyPart.HasValue)
                    continue;

                bodyParts.Add(bodyPart.StartingBodyPart.Value);
            }

            foreach (var bodyPart in validatedBodyParts)
            {
                if (bodyPrototype.BodyParts.ContainsKey(bodyPart.Key))
                    continue;

                validatedBodyParts.Remove(bodyPart.Key);
            }

            bodyParts.A
        }

        foreach (var markingCategory in prototypeManager.EnumeratePrototypes<MarkingCategoryPrototype>())
        {
            var actualMarkings = appearance.Markings.GetValueOrDefault(markingCategory)?.ShallowClone() ?? [];

            markingManager.EnsureValidColors(actualMarkings);
            markingManager.EnsureValidGroupAndSex(actualMarkings, organData.Value.Group, sex);
            markingManager.EnsureValidLayers(actualMarkings, organData.Value.Layers);
            markingManager.EnsureValidLimits(actualMarkings, organData.Value.Group, organData.Value.Layers, skinColor, eyeColor);

            validatedMarkings[markingCategory] = actualMarkings;
        }

        return new (eyeColor, skinColor, validatedMarkings, validatedBodyParts);
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               MarkingManager.MarkingsAreEqual(Markings, other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
