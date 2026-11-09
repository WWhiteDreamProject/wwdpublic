using Content.Shared._White.Pain.Components;
using Content.Shared._White.Pain.Systems;
using Robust.Shared.Player;

namespace Content.Client._White.Pain.Systems;

public sealed partial class PainfulSystem
{
    private void InitializeOverlay()
    {
        SubscribeLocalEvent<PainOverlayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PainOverlayComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PainOverlayComponent, LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<PainOverlayComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<PainOverlayComponent, PainChangedEvent>(OnPainChanged);
    }

    #region Event Handling

    private void OnInit(Entity<PainOverlayComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent)
            return;

        _overlayManager.AddOverlay(_overlay);
        _overlay.Clear();
        _overlay.SetComp(ent.Comp);
    }

    private void OnShutdown(Entity<PainOverlayComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Clear();
    }

    private void OnPlayerAttach(Entity<PainOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
        _overlay.Clear();
        _overlay.SetComp(ent.Comp);
    }

    private void OnPlayerDetached(Entity<PainOverlayComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Clear();
    }

    private void OnPainChanged(Entity<PainOverlayComponent> ent, ref PainChangedEvent args)
    {
        if (GameTiming.ApplyingState || !GameTiming.IsFirstTimePredicted)
            return;

        if (ent != _player.LocalEntity)
            return;

        if (args.Pain <= ent.Comp.MinPain)
            return;

        _overlay.AddPain(args.Pain.Float());
    }

    #endregion
}
