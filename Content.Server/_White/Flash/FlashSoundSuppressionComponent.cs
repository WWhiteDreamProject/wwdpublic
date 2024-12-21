namespace Content.Server._White.Flash;

[RegisterComponent]
public sealed partial class FlashSoundSuppressionComponent : Component
{
    [DataField]
    public float MaxRange = 2f;
}
