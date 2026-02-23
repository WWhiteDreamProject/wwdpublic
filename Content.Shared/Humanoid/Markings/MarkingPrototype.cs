using Content.Shared._White.Body.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings
{
    [Prototype("marking")]
    public sealed partial class MarkingPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = "uwu";

        public string Name { get; private set; } = default!;

        [DataField("markingCategory", required: true)]
        public MarkingCategories MarkingCategory { get; private set; }

        [DataField("speciesRestriction")]
        public List<string>? SpeciesRestrictions { get; private set; }

        [DataField]
        public bool InvertSpeciesRestriction { get; private set; }

        [DataField]
        public Sex? SexRestriction { get; private set; }

        [DataField]
        public bool InvertSexRestriction { get; private set; }

        [DataField]
        public bool FollowSkinColor { get; private set; }

        [DataField]
        public bool ForcedColoring { get; private set; }

        [DataField]
        public MarkingColors Coloring { get; private set; } = new();

        [DataField]
        public string PreviewDirection { get; private set; } = "South";

        [DataField("sprites", required: true)]
        public List<MarkingLayerInfo> Sprites { get; private set; } = default!; // WD EDIT

        public Marking AsMarking()
        {
            return new Marking(ID, Sprites.Count);
        }
    }

    // WD EDIT START
    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class MarkingLayerInfo
    {
        public MarkingLayerInfo(MarkingLayerInfo other)
        {
            BodyPart = other.BodyPart;
            Color = other.Color;
            Layer = other.Layer;
            MarkingId = other.MarkingId;
            Organ = other.Organ;
            ReplacementBodyPart = other.ReplacementBodyPart;
            Shader = other.Shader;
            Sprite = other.Sprite;
            State = other.State;
            Visible = other.Visible;
        }

        [DataField]
        public BodyPartType BodyPart = BodyPartType.None;

        [DataField]
        public Color Color = Color.White;

        [DataField(required:true)]
        public Enum Layer { get; private set; } = null!;

        [DataField]
        public string MarkingId = string.Empty;

        [DataField]
        public OrganType Organ = OrganType.None;

        [DataField]
        public EntProtoId? ReplacementBodyPart;

        [DataField]
        public string? Shader;

        [DataField]
        public ResPath Sprite;

        [DataField]
        public string State = string.Empty;

        [DataField]
        public bool Visible = true;
    }
    // WD EDIT END
}
