using Content.Shared.Whitelist;

namespace Content.Shared._White.Repulse.Components;

[RegisterComponent]
public sealed partial class RepulseComponent : Component
{
    [DataField]
    public float ForceMultiplier = 13000;

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public EntityWhitelist? TargetBlacklist;
}
