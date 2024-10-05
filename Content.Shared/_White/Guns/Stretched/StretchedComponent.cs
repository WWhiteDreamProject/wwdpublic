using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;

namespace Content.Shared._White.Guns.Stretched;

[RegisterComponent]
public sealed partial class StretchedComponent : Component
{
    [ViewVariables]
    public bool Stretched;

    [DataField, ViewVariables]
    public SoundSpecifier? SoundDraw = new SoundPathSpecifier("/Audio/Weapons/drawbow2.ogg");

    public BallisticAmmoProviderComponent Provider = default!;
}
