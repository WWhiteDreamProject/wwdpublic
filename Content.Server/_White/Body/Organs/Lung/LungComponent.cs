using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Body.Organs.Lung;

[RegisterComponent]
public sealed partial class LungComponent : Component
{
    /// <summary>
    /// Can reactions occur in the lung?
    /// </summary>
    [DataField]
    public bool CanReact;

    /// <summary>
    /// The volume that the lung inhales.
    /// </summary>
    [DataField]
    public float BreathVolume = 2f;

    /// <summary>
    /// Multiplier applied to <see cref="BreathVolume"/> for adjusting based on health.
    /// </summary>
    [DataField]
    public float BreathVolumeMultiplier = 1f;

    /// <summary>
    /// Maximum lung volume.
    /// </summary>
    [DataField]
    public float MaxVolume = 100f;

    /// <summary>
    /// A mixture of gases inside the lung.
    /// </summary>
    [DataField]
    public GasMixture Air = new()
    {
        Volume = 10,
        Temperature = Atmospherics.NormalBodyTemperature
    };

    /// <summary>
    /// The type of gas this lung needs. Used only for the breathing alerts, not actual metabolism.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "LowOxygen";

    /// <summary>
    /// The name/key of the solution on this entity which these lungs act on.
    /// </summary>
    [DataField]
    public string SolutionName = "lung";

    /// <summary>
    /// The solution on this entity that these lungs act on.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// Adjusted breath volume.
    /// </summary>
    [ViewVariables]
    public float AdjustedBreathVolume => BreathVolume * BreathVolumeMultiplier;
}
