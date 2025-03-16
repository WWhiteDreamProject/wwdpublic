using Content.Server.Popups;
using Content.Server.Jittering;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class AlienInfectedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlienInfectedComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, AlienInfectedComponent component, ComponentInit args)
    {
        // var torsoPart = Comp<BodyComponent>(uid).RootContainer.ContainedEntities[0];
        // _body.TryCreateOrganSlot(torsoPart, "alienLarvaOrgan", out _);
        // _body.InsertOrgan(torsoPart, Spawn(component.OrganProtoId, Transform(uid).Coordinates), "alienLarvaOrgan");
        component.NextGrowRoll = _timing.CurTime + TimeSpan.FromSeconds(component.GrowTime);
        component.Stomach = _container.EnsureContainer<Container>(uid, "stomach");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AlienInfectedComponent>();
        while (query.MoveNext(out var uid, out var infected))
        {
            if (_timing.CurTime < infected.NextGrowRoll)
                continue;

            if (TryComp<InsideAlienLarvaComponent>(infected.SpawnedLarva, out var insideAlienLarvaComponent) && insideAlienLarvaComponent.IsGrown)
            {
                _container.EmptyContainer(infected.Stomach);
                RemComp<AlienInfectedComponent>(uid);
                _mobStateSystem.ChangeMobState(uid, MobState.Dead);
                _damageable.TryChangeDamage(uid, infected.BurstDamage, true, false); // TODO: Only torso damage
                _popup.PopupEntity(Loc.GetString("larva-burst-entity"),
                    uid, PopupType.LargeCaution);
                _popup.PopupEntity(Loc.GetString("larva-burst-entity-other"),
                    uid, PopupType.MediumCaution);
            }

            if (infected.GrowthStage == 6)
            {
                var larva = Spawn(infected.Prototype, Transform(uid).Coordinates);
                _container.Insert(larva, infected.Stomach);
                infected.SpawnedLarva = larva;
                infected.GrowthStage++;
                _jittering.DoJitter(uid, TimeSpan.FromSeconds(8), true);
                _popup.PopupEntity(Loc.GetString("larva-inside-entity"),
                    uid, PopupType.Medium);
            }

            if (_random.Prob(infected.GrowProb))
            {
                infected.GrowthStage++;
            }
            infected.NextGrowRoll = _timing.CurTime + TimeSpan.FromSeconds(infected.GrowTime);
        }
    }
}
