using Content.Shared._White.Pain.Systems;

namespace Content.Shared._White.Pain.Components;

/// <summary>
/// Controls visual pain overlay effects rendered on the player's screen.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedPainfulSystem))]
public sealed partial class PainOverlayComponent : Component
{
    /// <summary>
    /// The tint color applied to the pain overlay.
    /// </summary>
    [DataField]
    public Color Color = Color.FromHex("#400303");

    /// <summary>
    /// Maximum pain value that can be accumulated.
    /// Pain is clamped between <see cref="MinPain"/> and this value.
    /// </summary>
    [DataField]
    public float MaxPain = 200f;

    /// <summary>
    /// Pain threshold at which the overlay reaches full intensity (alpha = 1.0).
    /// The overlay's alpha is calculated as currentPain / MaxEffect.
    /// </summary>
    [DataField]
    public float MaxEffect = 100f;

    /// <summary>
    /// Minimum pain value threshold. Pain cannot decay below this value.
    /// </summary>
    [DataField]
    public float MinPain = 5f;
}
