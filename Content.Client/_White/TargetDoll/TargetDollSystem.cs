using Content.Shared._White.TargetDoll;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._White.TargetDoll;

public sealed class TargetDollSystem : SharedTargetDollSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    /// Raised whenever selected body part changes.
    /// </summary>
    public event Action<BodyPart>? LocalPlayerTargetDollUpdated;

    public event Action<TargetDollComponent>? LocalPlayerTargetDollAdded;
    public event Action? LocalPlayerTargetDollRemoved;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetDollComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<TargetDollComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TargetDollComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<TargetDollComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TargetDollComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnHandleState(EntityUid uid, TargetDollComponent component, AfterAutoHandleStateEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        LocalPlayerTargetDollUpdated?.Invoke(component.SelectedBodyPart);
    }

    private void OnPlayerAttached(EntityUid uid, TargetDollComponent component, LocalPlayerAttachedEvent args)
    {
        LocalPlayerTargetDollAdded?.Invoke(component);
    }

    private void OnPlayerDetached(EntityUid uid, TargetDollComponent component, LocalPlayerDetachedEvent args)
    {
        LocalPlayerTargetDollRemoved?.Invoke();
    }

    private void OnStartup(EntityUid uid, TargetDollComponent component, ComponentStartup args)
    {
        LocalPlayerTargetDollAdded?.Invoke(component);
    }

    private void OnShutdown(EntityUid uid, TargetDollComponent component, ComponentShutdown args)
    {
        LocalPlayerTargetDollRemoved?.Invoke();
    }

    public override void SelectBodyPart(Entity<TargetDollComponent> ent, BodyPart bodyPart)
    {
        base.SelectBodyPart(ent, bodyPart);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        LocalPlayerTargetDollUpdated?.Invoke(ent.Comp.SelectedBodyPart);
    }
}
