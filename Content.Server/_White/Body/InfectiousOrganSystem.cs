using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Body;

public sealed class InfectiousOrganSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectiousOrganComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<InfectiousOrganComponent, OrganAddedToBodyEvent>(OnOrganAddedToBody);
        SubscribeLocalEvent<InfectiousOrganComponent, OrganRemovedFromBodyEvent>(OnOrganRemovedFromBody);
    }

    private void OnShutdown(EntityUid uid, InfectiousOrganComponent component, ComponentShutdown args)
    {
        if (component.Body.HasValue)
            RemComp<InfectedBodyComponent>(component.Body.Value);
    }

    private void OnOrganAddedToBody(EntityUid uid, InfectiousOrganComponent component, OrganAddedToBodyEvent args)
    {
        AddComp(args.Body, new InfectedBodyComponent { InfectiousOrgan = uid, });
        component.Growing = true;
        component.Body = args.Body;
    }

    private void OnOrganRemovedFromBody(EntityUid uid, InfectiousOrganComponent component, OrganRemovedFromBodyEvent args)
    {
        RemComp<InfectedBodyComponent>(args.OldBody);
        component.Growing = false;
        component.Body = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<InfectiousOrganComponent>();
        while (query.MoveNext(out var uid, out var infection))
        {
            if (!infection.Growing || time < infection.NextPointsAt)
                continue;

            infection.NextPointsAt = time + infection.GrowTime;

            if (!_random.Prob(infection.GrowProb))
                continue;

            infection.GrowthStage++;

            if (infection.GrowthStage >= infection.MaxGrowthStage)
                infection.Growing = false;

            if (!infection.Effects.TryGetValue(infection.GrowthStage, out var effects)
                || !TryComp<OrganComponent>(uid, out var organ) || !organ.Body.HasValue)
                continue;

            var effectsArgs = new EntityEffectBaseArgs(organ.Body.Value, EntityManager);
            foreach (var effect in effects)
                effect.Effect(effectsArgs);
        }
    }
}
