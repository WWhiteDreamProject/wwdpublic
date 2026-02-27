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
    /// Raised whenever selected provider changes.
    /// </summary>
    public event Action<BodyProviderType>? LocalPlayerTargetDollUpdated;

    public event Action<TargetDollComponent>? LocalPlayerTargetDollAdded;
    public event Action? LocalPlayerTargetDollRemoved;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetDollComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<TargetDollComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TargetDollComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TargetDollComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TargetDollComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    #region Event Handling

    private void OnHandleState(EntityUid uid, TargetDollComponent component, AfterAutoHandleStateEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        LocalPlayerTargetDollUpdated?.Invoke(component.SelectedProvider);
    }

    private void OnShutdown(EntityUid uid, TargetDollComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == uid)
            LocalPlayerTargetDollRemoved?.Invoke();
    }

    private void OnStartup(EntityUid uid, TargetDollComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalEntity == uid)
            LocalPlayerTargetDollAdded?.Invoke(component);
    }

    private void OnPlayerAttached(EntityUid uid, TargetDollComponent component, LocalPlayerAttachedEvent args)
    {
        LocalPlayerTargetDollAdded?.Invoke(component);
    }

    private void OnPlayerDetached(EntityUid uid, TargetDollComponent component, LocalPlayerDetachedEvent args)
    {
        LocalPlayerTargetDollRemoved?.Invoke();
    }

    #endregion

    #region Public API

    public override void SelectProvider(Entity<TargetDollComponent?> ent, BodyProviderType provider)
    {
        if (!TargetDollQuery.Resolve(ent, ref ent.Comp, false))
            return;

        base.SelectProvider(ent, provider);

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        LocalPlayerTargetDollUpdated?.Invoke(ent.Comp.SelectedProvider);
    }

    #endregion
}
