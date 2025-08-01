namespace Content.Server._White.Xenomorphs.Larva.Components;

[RegisterComponent]
public sealed partial class XenomorphLarvaComponent : Component
{
    [DataField]
    public TimeSpan BurstDelay = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public EntityUid? Victim;
}
