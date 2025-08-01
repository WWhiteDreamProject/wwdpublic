using Content.Server._White.Xenomorphs.Infection.Components;
using Content.Server._White.Xenomorphs.Larva.Components;
using Content.Shared.Body.Events;
using Content.Shared.EntityEffects;
using Robust.Server.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Infection;

public sealed class XenomorphInfectionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphInfectionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<XenomorphInfectionComponent, OrganAddedToBodyEvent>(OnOrganAddedToBody);
        SubscribeLocalEvent<XenomorphInfectionComponent, OrganRemovedFromBodyEvent>(OnOrganRemovedFromBody);
    }

    private void OnShutdown(EntityUid uid, XenomorphInfectionComponent component, ComponentShutdown args)
    {
        if (component.Infected.HasValue)
            RemComp<XenomorphInfectedComponent>(component.Infected.Value);
    }

    private void OnOrganAddedToBody(EntityUid uid, XenomorphInfectionComponent component, OrganAddedToBodyEvent args)
    {
        AddComp(args.Body, new XenomorphInfectedComponent { Infection = uid, });
        component.Infected = args.Body;
    }

    private void OnOrganRemovedFromBody(EntityUid uid, XenomorphInfectionComponent component, OrganRemovedFromBodyEvent args)
    {
        RemComp<XenomorphInfectedComponent>(args.OldBody);
        component.Infected = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<XenomorphInfectionComponent>();
        while (query.MoveNext(out var uid, out var infection))
        {
            if (!infection.Infected.HasValue || infection.GrowthStage >= infection.MaxGrowthStage || time < infection.NextPointsAt)
                continue;

            infection.NextPointsAt = time + infection.GrowTime;

            if (!_random.Prob(infection.GrowProb))
                continue;

            infection.GrowthStage++;

            if (infection.Effects.TryGetValue(infection.GrowthStage, out var effects))
            {
                var effectsArgs = new EntityEffectBaseArgs(infection.Infected.Value, EntityManager);
                foreach (var effect in effects)
                    effect.Effect(effectsArgs);
            }

            if (infection.GrowthStage < infection.MaxGrowthStage)
                continue;

            if (!_container.TryGetContainingContainer((uid, null, null), out var container))
            {
                QueueDel(uid);
                continue;
            }

            var larva = Spawn(infection.LarvaPrototype);

            var larvaComponent = EnsureComp<XenomorphLarvaComponent>(larva);
            larvaComponent.Victim = infection.Infected.Value;

            var larvaVictim = EnsureComp<XenomorphLarvaVictimComponent>(infection.Infected.Value);
            if (infection.InfectedIcons.TryGetValue(infection.GrowthStage, out var infectedIcon))
                larvaVictim.InfectedIcon = infectedIcon;

            _container.Remove(uid, container);
            _container.Insert(larva, container);

            QueueDel(uid);
        }
    }
}
