using Content.Shared.RadialSelector;

namespace Content.Server._White.RadialEntityMorph;

[RegisterComponent]
public sealed partial class RadialEntityMorphComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = new();
}
