using Content.Server._White.Xenomorphs.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared._White.Xenomorphs.Systems;
using Content.Shared.Actions;
using Content.Shared.Devour.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class AlienEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlienEvolutionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PraetorianEvolutionComponent, ComponentInit>(OnComponentInitPraetorian);
        SubscribeLocalEvent<QueenEvolutionComponent, ComponentInit>(OnComponentInitQueen);

        SubscribeLocalEvent<AlienEvolutionComponent, AlienDroneEvolveActionEvent>(OnEvolveDrone);
        SubscribeLocalEvent<AlienEvolutionComponent, AlienSentinelEvolveActionEvent>(OnEvolveSentinel);
        SubscribeLocalEvent<AlienEvolutionComponent, AlienHunterEvolveActionEvent>(OnEvolveHunter);
        SubscribeLocalEvent<PraetorianEvolutionComponent, AlienPraetorianEvolveActionEvent>(OnEvolvePraetorian);
        SubscribeLocalEvent<QueenEvolutionComponent, AlienQueenEvolveActionEvent>(OnEvolveQueen);
    }

    private void OnComponentInit(EntityUid uid, AlienEvolutionComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.DroneEvolutionActionEntity, component.DroneEvolutionAction, uid);
        _actionsSystem.AddAction(uid, ref component.SentinelEvolutionActionEntity, component.SentinelEvolutionAction, uid);
        _actionsSystem.AddAction(uid, ref component.HunterEvolutionActionEntity, component.HunterEvolutionAction, uid);

        _actionsSystem.SetCooldown(component.DroneEvolutionActionEntity, component.EvolutionCooldown);
        _actionsSystem.SetCooldown(component.SentinelEvolutionActionEntity, component.EvolutionCooldown);
        _actionsSystem.SetCooldown(component.HunterEvolutionActionEntity, component.EvolutionCooldown);
    }

    private void OnComponentInitPraetorian(EntityUid uid, PraetorianEvolutionComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.PraetorianEvolutionActionEntity, component.PraetorianEvolutionAction, uid);
    }

    private void OnComponentInitQueen(EntityUid uid, QueenEvolutionComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.QueenEvolutionActionEntity, component.QueenEvolutionAction, uid);
    }

    private void OnEvolveDrone(EntityUid uid, AlienEvolutionComponent component, AlienDroneEvolveActionEvent args)
    {
        Evolve(uid, component.DronePolymorphPrototype);
    }

    private void OnEvolveSentinel(EntityUid uid, AlienEvolutionComponent component, AlienSentinelEvolveActionEvent args)
    {
        Evolve(uid, component.SentinelPolymorphPrototype);
    }

    private void OnEvolveHunter(EntityUid uid, AlienEvolutionComponent component, AlienHunterEvolveActionEvent args)
    {
        Evolve(uid, component.HunterPolymorphPrototype);
    }

    public void Evolve(EntityUid uid, ProtoId<PolymorphPrototype> polymorphProtoId)
    {
        if (TryComp(uid, out DevourerComponent? component))
            _containerSystem.EmptyContainer(component.Stomach, true);
        _polymorphSystem.PolymorphEntity(uid, polymorphProtoId);
    }

    private void OnEvolveQueen(EntityUid uid, QueenEvolutionComponent component, AlienQueenEvolveActionEvent args)
    {
        if (TryComp<PlasmaVesselComponent>(uid, out var plasmaComp)
            && plasmaComp.Plasma <= component.PlasmaCost)
        {
            _popupSystem.PopupEntity(Loc.GetString("alien-action-fail-plasma"), uid, uid);
            return;
        }

        if (EntityQueryEnumerator<AlienQueenComponent>().MoveNext(out var id, out var _) && id != uid && !_mobStateSystem.IsDead(id))
        {
            _popupSystem.PopupEntity(Loc.GetString("alien-evolution-fail"), uid, uid);
            return;
        }

        Evolve(uid, component.QueenPolymorphPrototype);
    }

    private void OnEvolvePraetorian(EntityUid uid, PraetorianEvolutionComponent component, AlienPraetorianEvolveActionEvent args)
    {
        if (TryComp<PlasmaVesselComponent>(uid, out var plasmaComp)
            && plasmaComp.Plasma <= component.PlasmaCost)
        {
            _popupSystem.PopupEntity(Loc.GetString("alien-action-fail-plasma"), uid, uid);
            return;
        }

        var query = EntityQueryEnumerator<QueenEvolutionComponent>();

        if (query.MoveNext(out var id, out var comp) && id != uid && !_mobStateSystem.IsDead(id))
        {
            _popupSystem.PopupEntity(Loc.GetString("alien-evolution-fail"), uid, uid);
            return;
        }

        Evolve(uid, component.PraetorianPolymorphPrototype);
    }
}


