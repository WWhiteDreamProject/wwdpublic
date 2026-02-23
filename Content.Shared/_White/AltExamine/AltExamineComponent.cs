using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Shared._White.AltExamine
{
    [RegisterComponent]
    public sealed partial class AltExamineComponent : Component
    {
        [DataField("overrideDirection")]
        public Direction OverrideDirection = Direction.South;

        [DataField("offsetDistance")]
        public float OffsetDistance = 0.5f;

        [DataField("enableOverride")]
        public bool EnableOverride = true;

        [DataField("shader")]
        public string? Shader;

        [DataField("scale")]
        public Vector2? Scale;

        [DataField("color")]
        public Color? Color;

        [DataField("alpha")]
        public float? Alpha;

        [DataField("outlineColor")]
        public Color? OutlineColor = Robust.Shared.Maths.Color.FromHex("#C8C8C859");

        [DataField("outlineShader")]
        public string OutlineShader = "SelectionOutline";

        [DataField("useAltCalc")]
        public bool UseAltCalc = false;
    }
}
