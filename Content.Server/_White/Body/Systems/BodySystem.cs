using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Server._White.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
        InitializeRelay();
        InitializeBody();
        InitializeBodyPart();
        InitializeBone();
    }

    private void OnRelayMoveInput(Entity<BodyComponent> ent, ref MoveInputEvent args)
    {
        // If they haven't actually moved then ignore it.
        if ((args.Entity.Comp.HeldMoveButtons & (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) == 0x0)
            return;

        if (_mobState.IsDead(ent) && _mindSystem.TryGetMind(ent, out var mindId, out var mind))
        {
            mind.TimeOfDeath ??= _gameTiming.RealTime;
            _ghostSystem.OnGhostAttempt(mindId, canReturnGlobal: true, mind: mind);
        }
    }
}
