using Content.Server._White.Respirator.Systems;
using Content.Shared._White.Wounds;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Respirator.Components;

[RegisterComponent]
[Access(typeof(RespiratorSystem))]
public sealed partial class RespiratorProviderComponent : Component
{
    /// <summary>
    /// Determines whether chemical reactions can occur within the provider's solution.
    /// </summary>
    [DataField]
    public bool SolutionCanReact;

    /// <summary>
    /// A dictionary mapping different wound severities to their corresponding provider volume.
    /// </summary>
    [DataField]
    public Dictionary<WoundSeverity, float> VolumeThresholds = new()
    {
        {WoundSeverity.Healthy, 2f},
        {WoundSeverity.Minor, 1.65f},
        {WoundSeverity.Moderate, 1.15f},
        {WoundSeverity.Severe, 0.5f},
        {WoundSeverity.Critical, 0f},
    };

    /// <summary>
    /// The maximum capacity of the provider solution.
    /// </summary>
    [DataField]
    public float SolutionMaxVolume = 100f;

    /// <summary>
    /// The volume of gas inhaled or exhaled in a single breath cycle.
    /// </summary>
    [DataField]
    public float Volume = 2f;

    /// <summary>
    /// Represents the current mixture of gases present within the provider.
    /// </summary>
    [DataField]
    [Access(typeof(RespiratorSystem), Other = AccessPermissions.ReadExecute)] // TODO: FIXME Friends
    public GasMixture Air = new()
    {
        Volume = 10,
        Temperature = Atmospherics.NormalBodyTemperature,
    };

    /// <summary>
    /// The identifier for the alert prototype that should be displayed when the provider's will not get oxygen.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "LowOxygen";

    /// <summary>
    /// The key name used to identify the provider solution.
    /// </summary>
    [DataField]
    public string SolutionName = "lung";

    /// <summary>
    /// The body entity containing this provider, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Body;

    /// <summary>
    /// Reference to the entity's provider solution.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;
}
