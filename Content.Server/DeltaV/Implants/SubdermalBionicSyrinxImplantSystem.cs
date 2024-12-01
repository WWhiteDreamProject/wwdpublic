using Content.Server.VoiceMask;
using Content.Shared.Implants;
using Content.Shared.Tag;

namespace Content.Server.Implants;

public sealed class SubdermalBionicSyrinxImplantSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string BionicSyrinxImplant = "BionicSyrinxImplant";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceMaskComponent, SubdermalImplantInserted>(OnInsert); // WD EDIT
    }

    // WD EDIT START
    private void OnInsert(EntityUid uid, VoiceMaskComponent component, SubdermalImplantInserted args)
    {
        if (_tag.HasTag(uid, BionicSyrinxImplant))
            return;

        component.VoiceMaskName = Name(args.Target);
    }
    // WD EDIT END
}
