using Content.Server.Body.Components;
using Content.Server.Limbus.Traits.Components;
using Content.Server.Vampiric;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;

namespace Content.Server.Limbus.Traits;

public sealed class VampirismSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VampirismComponent, MapInitEvent>(OnInitVampire);
    }

    private void OnInitVampire(Entity<VampirismComponent> ent, ref MapInitEvent args)
    {
        EnsureBloodSucker(ent);

        if (!_body.TryGetOrgans<MetabolizerComponent>(ent.Owner, out var organs, OrganType.Stomach)) // WD EDIT
            return;

        foreach (var organ in organs) // WD EDIT
        {
            if (!TryComp<StomachComponent>(organ.Owner, out var stomach)) // WD EDIT
                continue;

            organ.Comp2.MetabolizerTypes = ent.Comp.MetabolizerPrototypes; // WD EDIT

            if (ent.Comp.SpecialDigestible is {} whitelist)
                stomach.SpecialDigestible = whitelist;
        }
    }

    private void EnsureBloodSucker(Entity<VampirismComponent> uid)
    {
        if (HasComp<BloodSuckerComponent>(uid))
            return;

        AddComp(uid, new BloodSuckerComponent
        {
            Delay = uid.Comp.SuccDelay,
            InjectWhenSucc = false, // The code for it is deprecated, might wanna make it inject something when (if?) it gets reworked
            UnitsToSucc = uid.Comp.UnitsToSucc,
            WebRequired = false
        });
    }
}
