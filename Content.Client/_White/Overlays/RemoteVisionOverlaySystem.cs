using Content.Client.Eye.Blinding;
using Content.Shared._White.Overlays;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.Overlays;

public sealed class RemoteViewOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    RemoteControlOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoteControlOverlayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RemoteControlOverlayComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<RemoteControlOverlayComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RemoteControlOverlayComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);


        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, RemoteControlOverlayComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, RemoteControlOverlayComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(EntityUid uid, RemoteControlOverlayComponent comp, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, RemoteControlOverlayComponent comp, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.RemoveOverlay(_overlay);
    }
}

public sealed class RemoteControlOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    //public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public RemoteControlOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index<ShaderPrototype>("RemoteControl").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
