namespace Content.Server._War.StationEngine;

[RegisterComponent]

public sealed partial class StationEngineComponent : Component
{
    [DataField("timer")]
    public TimeSpan ExplosionInterval = TimeSpan.FromSeconds(60);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ExplosionTimer = TimeSpan.MaxValue;

    [DataField]
    public bool EngineBroken = false;
}
