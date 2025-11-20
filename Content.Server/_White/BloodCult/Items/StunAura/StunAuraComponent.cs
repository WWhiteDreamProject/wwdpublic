using Content.Shared._White.BloodCult.Items.BaseAura;

namespace Content.Server._White.BloodCult.Items.StunAura;

[RegisterComponent]
public sealed partial class StunAuraComponent : BaseAuraComponent
{
    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(16);

    [DataField]
    public TimeSpan MuteDuration = TimeSpan.FromSeconds(12);
}
