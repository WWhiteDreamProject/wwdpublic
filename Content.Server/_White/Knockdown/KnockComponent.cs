namespace Content.Server._White.Knockdown;

[RegisterComponent]
public sealed partial class KnockComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(5);
}
