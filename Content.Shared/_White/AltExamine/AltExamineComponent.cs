using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Shared._White.AltExamine
{
    [RegisterComponent]
    public sealed partial class AltExamineComponent : Component
    {
        // All comments are made through Google Translate.

        /// <summary>
        /// Shows a specific direction after turning off direction change (EnableOverride = true)
        /// </summary>
        [DataField("overrideDirection")]
        public Direction OverrideDirection = Direction.South;

        [DataField("offsetDistance")]
        public float OffsetDistance = 0.5f;

        /// <summary>
        /// Responsible for disabling direction change
        /// </summary>
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

        [DataField("visualLayer")]
        public string VisualLayer = "AltExamineOverlay";

        /// <summary>
        /// These parameters are needed to force specific values when the button is released.
        /// Some systems may have visual bugs, so it's better to force the desired values directly instead of remembering them.
        /// </summary>
        [DataField("forceColor")]
        public Color? ForceColor;

        [DataField("forceAlpha")]
        public float? ForceAlpha;

        [DataField("forceScale")]
        public Vector2? ForceScale;

        [DataField("forceOffset")]
        public Vector2? ForceOffset;

        [DataField("forceShader")]
        public string? ForceShader;

        [DataField("forceEnableOverride")]
        public bool? ForceEnableOverride;

        [DataField("forceDrawDepth")]
        public int? ForceDrawDepth;
    }
}
