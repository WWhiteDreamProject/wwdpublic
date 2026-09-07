using Content.Client._White.Pain.Overlays;
using Content.Shared._White.Pain.Components;
using Content.Shared._White.Pain.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._White.Pain.Systems;

public sealed partial class PainfulSystem : SharedPainfulSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [Dependency] private readonly SpriteSystem _system = default!;

    private PainOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        InitializeOverlay();
        InitializeStatus();
    }
}
