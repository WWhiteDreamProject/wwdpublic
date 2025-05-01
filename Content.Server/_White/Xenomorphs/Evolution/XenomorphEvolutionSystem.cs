using Content.Server.Actions;
using Content.Shared._White.Xenomorphs.Evolution;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Evolution;

public sealed class XenomorphEvolutionSystem : SharedXenomorphEvolutionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly ActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphEvolutionComponent, MapInitEvent>(OnXenomorphEvolutionMapInit);
        SubscribeLocalEvent<XenomorphEvolutionComponent, ComponentShutdown>(OnXenomorphEvolutionShutdown);
    }

    private void OnXenomorphEvolutionMapInit(EntityUid uid, XenomorphEvolutionComponent component, MapInitEvent args) =>
        _actions.AddAction(uid, ref component.EvolutionAction, component.EvolutionActionId);

    private void OnXenomorphEvolutionShutdown(EntityUid uid, XenomorphEvolutionComponent component, ComponentShutdown args) =>
        _actions.RemoveAction(uid, component.EvolutionAction);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<XenomorphEvolutionComponent>();
        while (query.MoveNext(out var uid, out var alienEvolution))
        {
            if (alienEvolution.Points == alienEvolution.Max || time < alienEvolution.NextPointsAt)
                continue;

            alienEvolution.NextPointsAt = time + TimeSpan.FromSeconds(1);
            alienEvolution.Points += alienEvolution.PointsPerSecond;
            Dirty(uid, alienEvolution);

            if (alienEvolution.Points != alienEvolution.Max)
                continue;

            Popup.PopupEntity(Loc.GetString("xenomorphs-evolution-ready"), uid, uid, PopupType.Large);
        }
    }
}
