using Content.Shared._White.Pain.Components;
using Content.Shared._White.Pain.Systems;
using Content.Shared._White.Threshold;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;

namespace Content.Client._White.Pain.Overlays;

[UsedImplicitly]
public sealed class PainOverlayUiController : UIController
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<PainfulComponent> _painfulQuery;
    private EntityQuery<PainThresholdsComponent> _thresholdsQuery;

    private PainOverlay _painOverlay = default!;

    private FixedPoint2 _pain = FixedPoint2.Zero;
    private MobState _mobState = MobState.Dead;
    private SortedDictionary<FixedPoint2,MobState> _mobStateThresholds = new();

    public override void Initialize()
    {
        _painOverlay = new PainOverlay();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PainChangedEvent>(OnPainChange);

        _mobStateQuery = EntityManager.GetEntityQuery<MobStateComponent>();
        _painfulQuery = EntityManager.GetEntityQuery<PainfulComponent>();
        _thresholdsQuery = EntityManager.GetEntityQuery<PainThresholdsComponent>();
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        ClearOverlay();

        if (!_mobStateQuery.TryComp(args.Entity, out var mobStateComp))
            return;

        if (!_painfulQuery.TryComp(args.Entity, out var painfulComp))
            return;

        if (!_thresholdsQuery.TryComp(args.Entity, out var painThresholdsComp))
            return;

        _pain = painfulComp.CurrentPain;
        _mobState = mobStateComp.CurrentState;
        _mobStateThresholds = painThresholdsComp.MobStateThresholds;

        UpdateOverlays();
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

        _mobState = args.NewMobState;
    }

    private void OnPainChange(PainChangedEvent args)
    {
        if (args.Painful != _player.LocalEntity)
            return;

        _painOverlay.PainBlink = args.Pain.Float();
        UpdateOverlays();
    }

    private void ClearOverlay()
    {
        _painOverlay.DeadLevel = 0f;
        _painOverlay.CritLevel = 0f;
        _painOverlay.PainLevel = 0f;
        _painOverlay.PainBlink = 0f;
    }

    private void UpdateOverlays()
    {
        _painOverlay.State = _mobState;

        if (_mobState == MobState.Dead)
        {
            _painOverlay.PainLevel = 0;
            _painOverlay.CritLevel = 0;
            return;
        }

        if (!_mobStateThresholds.TryGetNextValue(_mobState, out var nextState)
            || !_mobStateThresholds.TryGetKey(nextState, out var nextThreshold))
            return;

        if (_mobState == MobState.Critical)
        {
            _painOverlay.CritLevel = FixedPoint2.Min(1f, _pain / nextThreshold.Value).Float();
            _painOverlay.PainLevel = 0;
            _painOverlay.DeadLevel = 0;
            return;
        }

        _painOverlay.PainLevel = FixedPoint2.Min(1f, _pain / nextThreshold.Value).Float();

        if (_painOverlay.PainLevel < 0.05f)
            _painOverlay.PainLevel = 0;

        _painOverlay.CritLevel = 0;
        _painOverlay.DeadLevel = 0;
    }
}
