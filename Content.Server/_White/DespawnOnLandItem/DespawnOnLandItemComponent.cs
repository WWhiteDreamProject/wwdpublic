namespace Content.Server._White.DespawnOnLandItem;

[RegisterComponent]
public sealed partial class DespawnOnLandItemComponent : Component
{
    [DataField]
    public float TimeDespawnOnLand = 3f;
}
