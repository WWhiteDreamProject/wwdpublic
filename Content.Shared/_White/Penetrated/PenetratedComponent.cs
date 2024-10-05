namespace Content.Shared._White.Penetrated;

[RegisterComponent]
public sealed partial class PenetratedComponent : Component
{
    [DataField]
    public EntityUid? ProjectileUid;

    [DataField]
    public bool IsPinned;
}
