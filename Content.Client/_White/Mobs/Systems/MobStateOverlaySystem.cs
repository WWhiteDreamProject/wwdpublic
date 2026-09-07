using Content.Client._White.Mobs.Overlays;
using Content.Shared._White.Maths;
using Content.Shared._White.Mobs.Components;
using Content.Shared._White.Mobs.Systems;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client._White.Mobs.Systems;

public sealed class MobStateOverlaySystem : SharedMobStateOverlaySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private EntityQuery<MobStateOverlayComponent> _overlayQuery;

    private MobStateOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<MobStateOverlayComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<MobStateOverlayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MobStateOverlayComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MobStateOverlayComponent, LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<MobStateOverlayComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlayQuery = GetEntityQuery<MobStateOverlayComponent>();
    }

    #region Event Handling

    private void OnHandleState(Entity<MobStateOverlayComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not MobStateOverlayState state)
            return;

        if (ent.Comp.State == state.State)
            return;

        ent.Comp.State = state.State;
        UpdateData(ent);
    }

    private void OnInit(Entity<MobStateOverlayComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent)
            return;

        _overlayManager.AddOverlay(_overlay);
        _overlay.Clear();

        UpdateData(ent);
    }

    private void OnShutdown(Entity<MobStateOverlayComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Clear();
    }

    private void OnPlayerAttach(Entity<MobStateOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
        _overlay.Clear();

        UpdateData(ent);
    }

    private void OnPlayerDetached(Entity<MobStateOverlayComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Clear();
    }

    protected override void OnMobStateChanged(Entity<MobStateOverlayComponent> ent, ref MobStateChangedEvent args)
    {
        base.OnMobStateChanged(ent, ref args);
        UpdateData(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not {} uid)
            return;

        if (!_overlayQuery.TryComp(uid, out var comp))
            return;

        var ev = new GetMobStateOverlayLevelEvent(comp.State);
        RaiseLocalEvent(uid, ref ev);

        if (!MathHelper.CloseTo(comp.Level, ev.Level, 0.001f))
        {
            comp.Level += WhiteMath.Diff(ev.Level - comp.Level, frameTime, comp.Speed);
        }
        else
        {
            comp.Level = ev.Level;
        }

        _overlay.SetLevel(comp.Level);
    }

    #endregion

    #region Private API

    private void UpdateData(Entity<MobStateOverlayComponent> ent)
    {
        if (!ent.Comp.Data.TryGetValue(ent.Comp.State, out var data))
            return;

        _overlay.SetData(data);
    }

    #endregion
}
