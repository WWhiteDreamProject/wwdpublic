using Content.Server._White.Xenomorphs.Components;
using Content.Server.Actions;

namespace Content.Server._White.Xenomorphs.Systems;

/// <summary>
/// This handles resin structure production for xenomorphs, including walls, windows and nests.
/// </summary>.
public sealed class ResinSpinnerSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResinSpinnerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ResinSpinnerComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentInit(EntityUid uid, ResinSpinnerComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ResinWallActionEntity, component.ResinWallAction, uid);
        _actions.AddAction(uid, ref component.ResinWindowActionEntity, component.ResinWindowAction, uid);
        _actions.AddAction(uid, ref component.NestActionEntity, component.NestAction, uid);
    }

    private void OnComponentShutdown(EntityUid uid, ResinSpinnerComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ResinWallActionEntity);
        _actions.RemoveAction(uid, component.ResinWindowActionEntity);
        _actions.RemoveAction(uid, component.NestActionEntity);
    }
}
