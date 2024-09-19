namespace Content.Server._White.Melee.Crit;

[RegisterComponent]
public sealed partial class CritComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CritChance = 0.2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CritMultiplier = 2f;

    public float? RealChance;
}
