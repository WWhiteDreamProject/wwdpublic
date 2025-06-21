namespace Content.Shared._White.Guns;

[RegisterComponent]
public sealed partial class GunSlotComponent : Component
{
    [DataField(required: true)]
    public string Slot;
}
