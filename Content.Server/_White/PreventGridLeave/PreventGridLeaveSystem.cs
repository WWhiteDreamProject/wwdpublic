using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;


namespace Content.Server._White.PreventGridLeave;

public sealed class PreventGridLeaveSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreventGridLeaveComponent, ComponentInit>(OnInitialize);
    }

    private void OnInitialize(EntityUid uid, PreventGridLeaveComponent comp, ComponentInit args)
    {
        comp.GridId = _transform.GetGrid(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PreventGridLeaveComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.GridId == null || _transform.GetGrid(uid) == component.GridId)
            {
                component.IsTimerOn = false; // Turn off the timer if on the correct grid
                continue;
            }

            if (!component.IsTimerOn)
                StartKillTimer(uid, component);

            else if (component.TimerStarted + TimeSpan.FromSeconds(component.KillTimer) <= _timing.CurTime)
                Kill(uid);
        }
    }

    private void StartKillTimer(EntityUid uid, PreventGridLeaveComponent component)
    {
        // Start the timer and alert the player
        component.IsTimerOn = true;
        component.TimerStarted = _timing.CurTime;

        _popupSystem.PopupEntity(Loc.GetString("kill-timer-start", ("timer", component.KillTimer)), uid, uid, PopupType.LargeCaution);
    }

    private void Kill(EntityUid uid)
    {
        // Try to activate any gibbing implant
        if (TryComp<ImplantedComponent>(uid, out var implants))
        {
            foreach (var implant in implants.ImplantContainer.ContainedEntities)
            {
                if (HasComp<GibOnTriggerComponent>(implant))
                {
                    _trigger.Trigger(implant);
                    return;
                }
            }
        }

        // In case there is no implant, gib or delete
        if (TryComp<BodyComponent>(uid, out var body))
            _body.GibBody(uid, true, body);
        else
            Del(uid);
    }
}
