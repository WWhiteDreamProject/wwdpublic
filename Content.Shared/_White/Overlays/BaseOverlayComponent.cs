namespace Content.Shared._White.Overlays;

[RegisterComponent]
public abstract partial class BaseOverlayComponent : Component
{
    [DataField]
    public virtual Vector3 Tint { get; set; }

    [DataField]
    public virtual float Strength { get; set; }

    [DataField]
    public virtual float Noise { get; set; }

    [DataField]
    public virtual Color Color { get; set; }
}
