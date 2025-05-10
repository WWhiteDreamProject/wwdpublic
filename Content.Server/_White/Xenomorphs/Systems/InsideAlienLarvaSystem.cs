using Content.Server.Polymorph.Systems;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Actions;

namespace Content.Server._White.Xenomorphs.Systems;

/// <summary>
/// This handles the lifecycle and evolution of alien larvae inside hosts.
/// </summary>
public sealed class InsideAlienLarvaSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InsideAlienLarvaComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<InsideAlienLarvaComponent, AlienLarvaGrowActionEvent>(OnGrow);
    }

    private void OnComponentInit(EntityUid uid, InsideAlienLarvaComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.EvolutionActionEntity, component.EvolutionAction, uid);

        _actionsSystem.SetCooldown(component.EvolutionActionEntity, component.EvolutionCooldown);
    }

    public void OnGrow(EntityUid uid, InsideAlienLarvaComponent component, AlienLarvaGrowActionEvent args)
    {
        component.IsGrown = true;
        _polymorphSystem.PolymorphEntity(uid, component.PolymorphPrototype);
    }
}
