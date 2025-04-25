using Content.Server.Explosion.EntitySystems;
using Content.Shared._White.Explosion.Components;
using Content.Shared._White.Weapons.Ranged.Events;
using Content.Shared.Examine;
using Content.Shared.Explosion.Components;

namespace Content.Server._White.Explosion.EntitySystems;

public sealed class TriggerOnShotSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OnShotTimerTriggerComponent, ProjectileShotEvent>(OnShot);
        SubscribeLocalEvent<OnShotTimerTriggerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, OnShotTimerTriggerComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushText(Loc.GetString("trigger-activated-when-shot", ("reduction", component.DelayReduction)), priority: -1);
    }

    private void OnShot(EntityUid uid, OnShotTimerTriggerComponent component, ProjectileShotEvent args)
    {
        if (args.Handled || !TryComp<OnUseTimerTriggerComponent>(uid, out var triggerComponent))
            return;

        triggerComponent.Delay = Math.Max(0.5f, triggerComponent.Delay - component.DelayReduction);

        _trigger.StartTimer((uid, triggerComponent), uid);

        args.Handled = true;
    }
}
