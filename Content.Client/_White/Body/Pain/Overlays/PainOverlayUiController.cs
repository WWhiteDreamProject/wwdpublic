using Content.Client._White.Body.Pain.Systems;
using Content.Shared._White.Body.Pain.Components;
using Content.Shared._White.Body.Pain.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;

namespace Content.Client._White.Body.Pain.Overlays;

[UsedImplicitly]
public sealed class PainOverlayUiController : UIController
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly PainSystem _pain = default!;

    private PainOverlay _painOverlay = default!;

    public override void Initialize()
    {
        _painOverlay = new PainOverlay();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AfterPainChangedEvent>(OnPainChange);
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        ClearOverlay();

        if (!EntityManager.TryGetComponent<MobStateComponent>(args.Entity, out var mobState))
            return;

        if (mobState.CurrentState != MobState.Dead)
            UpdateOverlays((args.Entity, mobState));

        _overlay.AddOverlay(_painOverlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_painOverlay);
        ClearOverlay();
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _player.LocalEntity)
            return;

        UpdateOverlays((args.Target, args.Component));
    }

    private void OnPainChange(AfterPainChangedEvent args)
    {
        if (args.Target != _player.LocalEntity
            || !EntityManager.TryGetComponent<PainfulComponent>(args.Target, out var painful))
            return;

        UpdateOverlays(args.Target);

        if (args.CurrentPain - args.OldPain < 5)
            return;

        _painOverlay.PainBlink = FixedPoint2.Min(1f, args.CurrentPain - args.OldPain / painful.MaxPainIncreasePerSecond).Float();
    }

    private void ClearOverlay()
    {
        _painOverlay.DeadLevel = 0f;
        _painOverlay.CritLevel = 0f;
        _painOverlay.PainLevel = 0f;
        _painOverlay.PainBlink = 0f;
    }

    private void UpdateOverlays(Entity<MobStateComponent?, PainfulComponent?, PainThresholdsComponent?> entity)
    {
        if (entity.Comp1 == null && !EntityManager.TryGetComponent(entity, out entity.Comp1)
            || entity.Comp2 == null && !EntityManager.TryGetComponent(entity, out entity.Comp2)
            || entity.Comp3 == null && !EntityManager.TryGetComponent(entity, out entity.Comp3))
            return;

        _painOverlay.State = entity.Comp1.CurrentState;

        switch (entity.Comp1.CurrentState)
        {
            case MobState.Alive:
            {
                if (!_pain.TryGetNextState(entity.Comp3.MobStateThresholds, MobState.Alive, out var nextState)
                    || !_pain.TryGetThresholdFromState(entity.Comp3.MobStateThresholds, nextState.Value, out var critLevel))
                    return;

                _painOverlay.PainLevel = FixedPoint2.Min(1f, entity.Comp2.CurrentPain / critLevel.Value).Float();

                if (_painOverlay.PainLevel < 0.05f)
                    _painOverlay.PainLevel = 0;

                _painOverlay.CritLevel = 0;
                _painOverlay.DeadLevel = 0;

                break;
            }
            case MobState.Critical:
            {
                if (!_pain.TryGetThresholdFromState(entity.Comp3.MobStateThresholds, MobState.Dead, out var deadLevel))
                    return;

                _painOverlay.CritLevel = FixedPoint2.Min(1f, entity.Comp2.CurrentPain / deadLevel.Value).Float();
                _painOverlay.PainLevel = 0;
                _painOverlay.DeadLevel = 0;

                break;
            }
            case MobState.Dead:
            {
                _painOverlay.PainLevel = 0;
                _painOverlay.CritLevel = 0;

                break;
            }
        }
    }
}
