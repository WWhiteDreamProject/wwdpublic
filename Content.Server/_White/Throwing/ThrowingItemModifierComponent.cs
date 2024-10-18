namespace Content.Server._White.Throwing;

[RegisterComponent]
public sealed partial class ThrowingItemModifierComponent : Component
{
    [DataField]
    public float ThrowingMultiplier = 2.0f;
}
