using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._White.AdminObserver;

public sealed class SharedAdminObserverHandsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public event Action? LocalPlayerHandsUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ToggleAdminObserverHandsEvent>(OnToggleHands);

        SubscribeLocalEvent<AdminObserverHandsComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<AdminObserverHandsComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);

        SubscribeLocalEvent<AdminObserverHandsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<AdminObserverHandsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<AdminObserverHandsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AdminObserverHandsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AdminObserverHandsComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAttackAttempt(EntityUid uid, AdminObserverHandsComponent component, AttackAttemptEvent args)
    {
        if (!component.HandsEnabled)
            args.Cancel();
    }

    private void OnBeforeInteractHand(EntityUid uid, AdminObserverHandsComponent component, BeforeInteractHandEvent args)
    {
        if (component.HandsEnabled || args.Handled)
            return;

        args.Handled = true;

        if (TryComp<StrippableComponent>(args.Target, out var strippable))
        {
            if (args.Target != uid)
                _strippable.TryOpenStrippingUi(uid, (args.Target, strippable), openInCombat: true);
            return;
        }

        if (TryComp<StorageComponent>(args.Target, out var storage))
        {
            _storage.OpenStorageUI(args.Target, uid, storage);
        }
    }

    private void OnToggleHands(ToggleAdminObserverHandsEvent msg, EntitySessionEventArgs args)
    {
        if (_timing is { IsFirstTimePredicted: false, InPrediction: true })
            return;

        if (args.SenderSession.AttachedEntity is not { Valid: true } attached
            || !TryComp<AdminObserverHandsComponent>(attached, out var comp))
        {
            return;
        }

        SetHandsEnabled(attached, !comp.HandsEnabled, comp);
    }

    public void SetHandsEnabled(EntityUid uid, bool enabled, AdminObserverHandsComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (comp.HandsEnabled == enabled)
            return;

        comp.HandsEnabled = enabled;
        Dirty(uid, comp);

        if (_playerManager.LocalEntity == uid)
            LocalPlayerHandsUpdated?.Invoke();
    }

    private void OnPlayerAttached(Entity<AdminObserverHandsComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        LocalPlayerHandsUpdated?.Invoke();
    }

    private void OnPlayerDetached(Entity<AdminObserverHandsComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        LocalPlayerHandsUpdated?.Invoke();
    }

    private void OnStartup(Entity<AdminObserverHandsComponent> ent, ref ComponentStartup args)
    {
        if (_playerManager.LocalEntity == ent.Owner)
            LocalPlayerHandsUpdated?.Invoke();
    }

    private void OnShutdown(Entity<AdminObserverHandsComponent> ent, ref ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == ent.Owner)
            LocalPlayerHandsUpdated?.Invoke();
    }

    private void OnAfterHandleState(Entity<AdminObserverHandsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_playerManager.LocalEntity == ent.Owner)
            LocalPlayerHandsUpdated?.Invoke();
    }
}
