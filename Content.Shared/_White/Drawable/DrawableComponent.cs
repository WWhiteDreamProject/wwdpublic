using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;

namespace Content.Shared._White.Drawable;

[RegisterComponent]
public sealed partial class DrawableComponent : Component
{
    [ViewVariables]
    public bool Drawn;

    [DataField, ViewVariables]
    public SoundSpecifier? SoundDraw = new SoundPathSpecifier("/Audio/Weapons/drawbow2.ogg");

    public BallisticAmmoProviderComponent Provider = default!;
}
