using Content.Shared.Whitelist;

namespace Content.Shared._White.BloodCult.CultItem;

[RegisterComponent]
public sealed partial class CultItemComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist = new() { Components = ["BloodCultist", "Ghost",], };

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(2);
}
