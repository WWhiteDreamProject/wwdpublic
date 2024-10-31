namespace Content.Server._White.Knockdown;

public abstract partial class BaseKnockdownOnComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan StutterTime = TimeSpan.FromSeconds(15);
}
