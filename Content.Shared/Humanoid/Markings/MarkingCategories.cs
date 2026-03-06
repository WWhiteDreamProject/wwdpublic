using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings
{
    [Serializable, NetSerializable]
    public enum MarkingCategories : byte
    {
        Special,
        Face,
        Hair,
        FacialHair,
        Head,
        HeadTop,
        HeadSide,
        Snout,
        Chest,
        Groin, // WD EDIT
        UndergarmentTop,
        UndergarmentBottom,
        RightArm,
        RightHand,
        LeftArm,
        LeftHand,
        RightLeg,
        RightFoot,
        LeftLeg,
        LeftFoot,
        Wings,
        Tail,
        Overlay
    }

    public static class MarkingCategoriesConversion
    {
        public static MarkingCategories FromHumanoidVisualLayers(HumanoidVisualLayers layer)
        {
            return layer switch
            {
                HumanoidVisualLayers.Special => MarkingCategories.Special,
                HumanoidVisualLayers.Face => MarkingCategories.Face,
                HumanoidVisualLayers.Hair => MarkingCategories.Hair,
                HumanoidVisualLayers.FacialHair => MarkingCategories.FacialHair,
                HumanoidVisualLayers.Head => MarkingCategories.Head,
                HumanoidVisualLayers.HeadTop => MarkingCategories.HeadTop,
                HumanoidVisualLayers.HeadSide => MarkingCategories.HeadSide,
                HumanoidVisualLayers.Snout => MarkingCategories.Snout,
                HumanoidVisualLayers.Chest => MarkingCategories.Chest,
                HumanoidVisualLayers.UndergarmentTop => MarkingCategories.UndergarmentTop,
                HumanoidVisualLayers.UndergarmentBottom => MarkingCategories.UndergarmentBottom,
                HumanoidVisualLayers.RArm => MarkingCategories.RightArm,
                HumanoidVisualLayers.LArm => MarkingCategories.LeftArm,
                HumanoidVisualLayers.RHand => MarkingCategories.RightHand,
                HumanoidVisualLayers.LHand => MarkingCategories.LeftHand,
                HumanoidVisualLayers.LLeg => MarkingCategories.LeftLeg,
                HumanoidVisualLayers.RLeg => MarkingCategories.RightLeg,
                HumanoidVisualLayers.LFoot => MarkingCategories.LeftFoot,
                HumanoidVisualLayers.RFoot => MarkingCategories.RightFoot,
                HumanoidVisualLayers.Wings => MarkingCategories.Wings,
                HumanoidVisualLayers.Tail => MarkingCategories.Tail,
                _ => MarkingCategories.Overlay
            };
        }
    }
}
