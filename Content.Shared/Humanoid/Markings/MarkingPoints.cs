using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Humanoid.Markings;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MarkingPoints
{
    [DataField("points", required: true)]
    public int Points = 0;
    [DataField("required", required: true)]
    public bool Required = false;
    // Default markings for this layer.
    [DataField("defaultMarkings", customTypeSerializer:typeof(PrototypeIdListSerializer<MarkingPrototype>))]
    public List<string> DefaultMarkings = new();

    // WD EDIT START: The marking code is cancer. But I'm too lazy to edit it, and it will cause a lot of problems with upstreams in the future. But sooner or later, this thing will need to be rewritten.
    [DataField]
    public bool MatchSkin;

    [DataField]
    public float LayerAlpha = 1.0f;
    // WD EDIT END

    public static Dictionary<MarkingCategories, MarkingPoints> CloneMarkingPointDictionary(Dictionary<MarkingCategories, MarkingPoints> self)
    {
        var clone = new Dictionary<MarkingCategories, MarkingPoints>();

        foreach (var (category, points) in self)
        {
            clone[category] = new MarkingPoints()
            {
                Points = points.Points,
                Required = points.Required,
                DefaultMarkings = points.DefaultMarkings,
                // WD EDIT START
                MatchSkin = points.MatchSkin,
                LayerAlpha = points.LayerAlpha
                // WD EDIT END
            };
        }

        return clone;
    }
}

[Prototype("markingPoints")]
public sealed partial class MarkingPointsPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     If the user of this marking point set is only allowed to
    ///     use whitelisted markings, and not globally usable markings.
    ///     Only used for validation and profile construction. Ignored anywhere else.
    /// </summary>
    [DataField("onlyWhitelisted")] public bool OnlyWhitelisted;

    [DataField("points", required: true)]
    public Dictionary<MarkingCategories, MarkingPoints> Points { get; private set; } = default!;
}
