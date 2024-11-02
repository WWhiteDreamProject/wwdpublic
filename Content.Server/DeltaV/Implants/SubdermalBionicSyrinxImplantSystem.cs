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

        SubscribeLocalEvent<VoiceMaskerComponent, SubdermalImplantInserted>(OnInsert); // WD EDIT
    }

    // WD EDIT START
    private void OnInsert(EntityUid uid, VoiceMaskerComponent component, SubdermalImplantInserted args)
    {
        if (_tag.HasTag(uid, BionicSyrinxImplant))
            return;

        var voicemask = EnsureComp<SyrinxVoiceMaskComponent>(args.Target);
        voicemask.VoiceName = MetaData(args.Target).EntityName;
        Dirty(args.Target, voicemask);
    }
    // WD EDIT END
}
