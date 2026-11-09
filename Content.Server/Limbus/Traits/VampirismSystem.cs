using Content.Server.Limbus.Traits.Components;
using Content.Server.Vampiric;
using Content.Shared._White.Bloodstream.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Nutrition.Components;
using Content.Shared._White.Nutrition.Systems;

namespace Content.Server.Limbus.Traits;

public sealed class VampirismSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedIngestionSystem _stomach = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VampirismComponent, MapInitEvent>(OnInitVampire);
    }

    private void OnInitVampire(Entity<VampirismComponent> ent, ref MapInitEvent args)
    {
        // TODO
        /*EnsureBloodSucker(ent);

        if (!_body.TryGetProviders(ent.Owner, out var organs)) // WD EDIT
            return;

        foreach (var organ in organs) // WD EDIT
        {
            if (!TryComp<IngestionProviderComponent>(organ.Owner, out var stomach)) // WD EDIT
                continue
            if (!TryComp<MetabolizerComponent>(organ.Owner, out var metabolizer)) // WD EDIT
                continue;

            metabolizer.Types = ent.Comp.MetabolizerPrototypes; // WD EDIT

            _stomach.SetSpecialDigestible((organ.Owner, stomach), ent.Comp.SpecialDigestible); // WD EDIT
        }*/
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
