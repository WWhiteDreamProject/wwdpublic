using Content.Client.Crayon;
using Content.Shared._White.Hands.Components;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._White.Overlays;

public sealed class CrayonPreviewOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly CrayonComponent _crayonComp;
    private readonly EntityUid _crayonUid;
    private string _currentState;

    private Texture _tex;
    private readonly SpriteSystem _sprite;

    public CrayonPreviewOverlay(SpriteSystem sprite, CrayonComponent comp)
    {
        IoCManager.InjectDependencies(this);
        _sprite = sprite;

        _crayonComp = comp;
        _crayonUid = comp.Owner;
        _currentState = comp.SelectedState;
        if (_proto.TryIndex<DecalPrototype>(_currentState, out var proto))
            _tex = _sprite.Frame0(proto.Sprite);
        else
            _tex = Texture.Transparent;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entMan.EntityExists(_crayonUid) || // failsafe
            _player.LocalEntity is not EntityUid playerUid ||
            _entMan.HasComponent<HoldingDropComponent>(playerUid))
            return;

        var handle = args.WorldHandle;

        if (_currentState != _crayonComp.SelectedState)
        {
            _currentState = _crayonComp.SelectedState;
            if (_proto.TryIndex<DecalPrototype>(_currentState, out var proto))
                _tex = _sprite.Frame0(proto.Sprite);
            else
                _tex = Texture.Transparent;
        }

        var mouseScreenPos = _input.MouseScreenPosition.Position;

        var angle = _crayonComp.Angle - _eye.CurrentEye.Rotation;
        var mouseMapPos = _eye.ScreenToMap(mouseScreenPos);
        var playerMapPos = _entMan.GetComponent<TransformComponent>(playerUid).MapPosition;

        float alpha = 0.6f;
        if ((mouseMapPos.Position - playerMapPos.Position).LengthSquared() > SharedInteractionSystem.InteractionRangeSquared)
            alpha = 0.1f;


#pragma warning disable RA0002 // ffs
        handle.DrawTexture(_tex, mouseMapPos.Position - new Vector2(0.5f, 0.5f), angle, _crayonComp.Color.WithAlpha(alpha));
#pragma warning restore RA0002
    }
}

