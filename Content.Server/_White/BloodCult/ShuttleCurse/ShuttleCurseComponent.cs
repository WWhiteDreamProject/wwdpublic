using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._White.BloodCult.ShuttleCurse;

[RegisterComponent]
public sealed partial class ShuttleCurseComponent : Component
{
    [DataField]
    public TimeSpan DelayTime = TimeSpan.FromMinutes(3);

    [DataField]
    public SoundSpecifier ScatterSound = new SoundCollectionSpecifier("GlassBreak");

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CurseMessages = "ShuttleCurse";
}
