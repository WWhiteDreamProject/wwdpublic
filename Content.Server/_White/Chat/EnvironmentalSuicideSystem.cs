using Content.Server.Chat;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._White.Chat;

public sealed class EnvironmentalSuicideSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize() =>
        SubscribeLocalEvent<MobStateComponent, SuicideEvent>(OnEnvironmentalSuicide, before: new[] { typeof(SuicideSystem) });

    /// <summary>
    /// Raise event to attempt to use held item, or surrounding entities to attempt to commit suicide
    /// </summary>
    private void OnEnvironmentalSuicide(Entity<MobStateComponent> victim, ref SuicideEvent args)
    {
        if (args.Handled || _mobState.IsCritical(victim))
            return;

        var suicideByEnvironmentEvent = new SuicideByEnvironmentEvent(victim);

        // Try to suicide by raising an event on the held item
        if (EntityManager.TryGetComponent(victim, out HandsComponent? handsComponent)
            && handsComponent.ActiveHandEntity is { } item)
        {
            RaiseLocalEvent(item, suicideByEnvironmentEvent);
            if (suicideByEnvironmentEvent.Handled)
            {
                args.Handled = suicideByEnvironmentEvent.Handled;
                return;
            }
        }

        // Try to suicide by nearby entities, like Microwaves or Crematoriums, by raising an event on it
        // Returns upon being handled by any entity
        var itemQuery = GetEntityQuery<ItemComponent>();
        foreach (var entity in _entityLookupSystem.GetEntitiesInRange(victim, 1, LookupFlags.Approximate | LookupFlags.Static))
        {
            // Skip any nearby items that can be picked up, we already checked the active held item above
            if (itemQuery.HasComponent(entity))
                continue;

            RaiseLocalEvent(entity, suicideByEnvironmentEvent);
            if (!suicideByEnvironmentEvent.Handled)
                continue;

            args.Handled = suicideByEnvironmentEvent.Handled;
            return;
        }
    }
}
