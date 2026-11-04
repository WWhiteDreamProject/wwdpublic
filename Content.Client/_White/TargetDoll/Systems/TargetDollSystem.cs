using Content.Shared._White.Body;
using Content.Shared._White.TargetDoll.Components;
using Content.Shared._White.TargetDoll.Systems;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._White.TargetDoll.Systems;

public sealed class TargetDollSystem : SharedTargetDollSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    /// Raised whenever selected provider changes.
    /// </summary>
    public event Action<BodyProviderType>? LocalPlayerTargetDollUpdated;

    /// <summary>
    /// Raised when the target doll component is added to the local player entity.
    /// </summary>
    public event Action<TargetDollComponent>? LocalPlayerTargetDollAdded;

    /// <summary>
    /// Raised when the target doll component is removed from the local player entity.
    /// </summary>
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

    private void OnHandleState(Entity<TargetDollComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        LocalPlayerTargetDollUpdated?.Invoke(ent.Comp.Selected);
    }

    private void OnShutdown(Entity<TargetDollComponent> ent, ref ComponentShutdown args)
    {
        if (_playerManager.LocalEntity != ent)
            return;

        LocalPlayerTargetDollRemoved?.Invoke();
    }

    private void OnStartup(Entity<TargetDollComponent> ent, ref ComponentStartup args)
    {
        if (_playerManager.LocalEntity != ent)
            return;

        LocalPlayerTargetDollAdded?.Invoke(ent.Comp);
    }

    private void OnPlayerAttached(Entity<TargetDollComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        LocalPlayerTargetDollAdded?.Invoke(ent.Comp);
    }

    private void OnPlayerDetached(Entity<TargetDollComponent> ent, ref LocalPlayerDetachedEvent args)
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

        LocalPlayerTargetDollUpdated?.Invoke(ent.Comp.Selected);
    }

    #endregion
}
