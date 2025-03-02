using System.Threading;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;


namespace Content.Server._White.Hearing;

public sealed class HearingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeafComponent, ComponentShutdown>(OnCompShutdown);
        SubscribeLocalEvent<HearingComponent, HearingChangedEvent>(OnHearingChanged);
        SubscribeLocalEvent<HearingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HearingComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    private void OnCompShutdown(EntityUid uid, DeafComponent component, ComponentShutdown args)
    {
        component.TokenSource?.Cancel();
    }

    private void OnMobStateChanged(EntityUid uid, HearingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
        {
            var eventParameters = new HearingChangedEvent(uid, true);
            RaiseLocalEvent(uid, eventParameters);
            return;
        }

        var eventParams = new HearingChangedEvent(uid, false, true, 0f, "deaf-chat-message");
        RaiseLocalEvent(uid, eventParams);

        Dirty(uid, component);
    }

    private void OnSleepStateChanged(EntityUid uid, HearingComponent component, SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
        {
            var eventParams = new HearingChangedEvent(uid, false, true, 0f, "deaf-chat-message");
            RaiseLocalEvent(uid, eventParams);
            return;
        }

        var eventParameters = new HearingChangedEvent(uid, true);
        RaiseLocalEvent(uid, eventParameters);

        Dirty(uid, component);
    }

    private void OnHearingChanged(EntityUid uid, HearingComponent component, HearingChangedEvent args)
    {
        if (args.Undeafen)
        {
            RemComp<DeafComponent>(uid);
            return;
        }

        if (args.Permanent)
        {
            EnsureComp<DeafComponent>(uid, out var deaf);
            deaf.DeafChatMessage = args.DeafChatMessage;
            deaf.Permanent = true;
            return;
        }

        // Dont apply temporary deafening if we are already perma deaf
        if (TryComp<DeafComponent>(uid, out var deafC) && deafC.Permanent)
        {
            return;
        }

        EnsureComp<DeafComponent>(uid, out var deafComponent);
        deafComponent.DeafChatMessage = args.DeafChatMessage;

        deafComponent.TokenSource?.Cancel();
        deafComponent.TokenSource = new CancellationTokenSource();

        uid.SpawnTimer(TimeSpan.FromSeconds(args.Duration), () => OnTimerFired(uid, deafComponent), deafComponent.TokenSource.Token);

        Dirty(uid, component);
    }

    private void OnTimerFired(EntityUid uid, DeafComponent deafComponent)
    {
        if (!deafComponent.Permanent)
            RemComp<DeafComponent>(uid);

        Dirty(uid, deafComponent);
    }
}
