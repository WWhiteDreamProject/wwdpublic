using Content.Shared._White.Body.Components;
using Content.Shared._White.TargetDoll;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._White.TargetDoll;

public sealed class TargetDollSystem : SharedTargetDollSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    /// Raised whenever selected body part changes.
    /// </summary>
    public event Action<BodyPartType>? LocalPlayerTargetDollUpdated;

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

        LocalPlayerTargetDollUpdated?.Invoke(component.SelectedBodyPartType);
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
        if (_playerManager.LocalEntity == uid)
            LocalPlayerTargetDollAdded?.Invoke(component);
    }

    private void OnShutdown(EntityUid uid, TargetDollComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == uid)
            LocalPlayerTargetDollRemoved?.Invoke();
    }

    public override void SelectBodyPart(Entity<TargetDollComponent> ent, BodyPartType bodyPartType)
    {
        base.SelectBodyPart(ent, bodyPartType);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        LocalPlayerTargetDollUpdated?.Invoke(ent.Comp.SelectedBodyPartType);
    }
}
