 namespace Content.Shared.Flash.Components;

/// <summary>
/// WWDP
/// </summary>

[RegisterComponent]
public sealed partial class FlashModifierComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
