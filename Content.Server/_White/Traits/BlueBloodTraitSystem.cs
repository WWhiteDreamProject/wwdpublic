using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using System.Diagnostics.CodeAnalysis;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server._White.Traits;
public sealed class BlueBloodTraitSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedBodySystem _bodySys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlueBloodTraitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BlueBloodTraitComponent component, MapInitEvent args)
    {
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            _bloodstream.TryModifyBloodLevel(uid, 0, bloodstream, false);
            _bloodstream.ChangeBloodReagent(uid, component.Blood, bloodstream);
            _bloodstream.TryModifyBloodLevel(uid, bloodstream.BloodMaxVolume, bloodstream, false);
        }

        if (TryGetHeart(uid, out var heartUid))
        {
            if (!TryComp<MetabolizerComponent>(heartUid, out var metabolizer))
                return;

            metabolizer.MetabolizerTypes = component.MetabolizerPrototype;
        }
    }

    private bool TryGetHeart(EntityUid uid, out EntityUid? heartUid)
    {
        heartUid = null;

        foreach (var (organId, organComp) in _bodySys.GetBodyOrgans(uid))
        {
            if (HasComp<HeartComponent>(organId))
            {
                heartUid = organId;
                return true;
            }
        }

        return false;
    }
}
