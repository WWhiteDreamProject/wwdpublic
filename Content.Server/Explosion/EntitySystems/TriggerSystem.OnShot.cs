using Content.Server.Sound.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeOnShot()
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
        if (args.Handled)
            return;

        if (!TryComp<OnUseTimerTriggerComponent>(uid, out var triggerComponent))
            return;

        triggerComponent.Delay = Math.Max(0.5f, triggerComponent.Delay - component.DelayReduction);

        StartTimer((uid, triggerComponent), uid);

        args.Handled = true;
    }
}
