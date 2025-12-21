using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared._White.BloodCult.Runes.Components;

[RegisterComponent]
public sealed partial class RuneDrawerComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist = new() {Components = ["BloodCultist", ], };

    [DataField]
    public float EraseTime = 4f;

    [DataField]
    public SoundSpecifier StartDrawingSound = new SoundPathSpecifier("/Audio/_White/Magic/BloodCult/butcher.ogg");

    [DataField]
    public SoundSpecifier EndDrawingSound = new SoundPathSpecifier("/Audio/_White/Magic/BloodCult/blood.ogg");
}
