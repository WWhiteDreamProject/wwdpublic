using Content.Client.Crayon;
using Content.Client.Hands.Systems;
using Content.Shared._White.Hands.Components;
using Content.Shared.Crayon;
using Content.Shared.Decals;
using Content.Shared.Hands.Components;
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

    private readonly HandsSystem _hands;
    private readonly SpriteSystem _sprite;

    public CrayonPreviewOverlay(SpriteSystem sprite, HandsSystem hands)
    {
        IoCManager.InjectDependencies(this);
        _sprite = sprite;
        _hands = hands;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not EntityUid playerUid ||
            !_entMan.HasComponent<HandsComponent>(playerUid) ||
            !_entMan.TryGetComponent<CrayonComponent>(_hands.GetActiveItem(playerUid), out var crayon) ||
            _entMan.HasComponent<HoldingDropComponent>(playerUid))
            return;

        var handle = args.WorldHandle;

        Texture tex;
        if (_proto.TryIndex<DecalPrototype>(crayon.SelectedState, out var proto))
            tex = _sprite.Frame0(proto.Sprite);
        else
            tex = Texture.Transparent;

        var mouseScreenPos = _input.MouseScreenPosition.Position;

        var angle = crayon.Angle - _eye.CurrentEye.Rotation;
        var mouseMapPos = _eye.ScreenToMap(mouseScreenPos);
        var playerMapPos = _entMan.GetComponent<TransformComponent>(playerUid).MapPosition;

        float alpha = 0.6f;
        if ((mouseMapPos.Position - playerMapPos.Position).LengthSquared() > SharedInteractionSystem.InteractionRangeSquared)
            alpha = 0.15f;


#pragma warning disable RA0002 // ffs
        handle.DrawTexture(tex, mouseMapPos.Position - new Vector2(0.5f, 0.5f), angle, crayon.Color.WithAlpha(alpha));
#pragma warning restore RA0002
    }
}

