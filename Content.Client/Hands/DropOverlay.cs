using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Hands;


public sealed class DropOverlay : Overlay
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly EntityManager _entMan = default!;

    private readonly HandsSystem _hands;
    private readonly SharedTransformSystem _transform;

    //private readonly Font _font;

    private IRenderTexture _renderBackbuffer;

    public DropOverlay(HandsSystem hands, SharedTransformSystem transform)
    {
        IoCManager.InjectDependencies(this);
        _hands = hands;
        _transform = transform;

        //var cache = IoCManager.Resolve<IResourceCache>();
        //_font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);

        _renderBackbuffer = _clyde.CreateRenderTarget(
            (128, 128),
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
            new TextureSampleParameters
            {
                Filter = true
            }, nameof(ShowHandItemOverlay));
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_hands.GetActiveHandEntity() is not EntityUid held ||
            !_entMan.HasComponent<SpriteComponent>(held) ||
            _player.LocalEntity is not EntityUid player) // how and why?
            return;

        var handle = args.ScreenHandle;

        var mouseScreenPos = _input.MouseScreenPosition.Position;

        var mouseMapPos = _eye.ScreenToMap(mouseScreenPos);
        // Why do i have to do so much to simply convert Vector2 from screenspace to worldspace and back?
        var finalMapPos = _hands.GetFinalDropCoordinates(player, _transform.GetMapCoordinates(player), mouseMapPos);
        var finalScreenPos = _eye.MapToScreen(new MapCoordinates(finalMapPos, mouseMapPos.MapId)).Position;
        //handle.DrawString(_font, mouseScreenPos, mouseScreenPos.ToString());
        //handle.DrawString(_font, mouseScreenPos + new System.Numerics.Vector2(0, 64), finalScreenPos.ToString());

        Angle adjustedAngle = _entMan.GetComponent<HoldingDropComponent>(player).Angle;
        handle.RenderInRenderTarget(_renderBackbuffer, () =>
        {
            handle.DrawEntity(held, _renderBackbuffer.Size / 2, new Vector2(2), adjustedAngle);
        }, Color.Transparent);

        handle.DrawTexture(_renderBackbuffer.Texture, finalScreenPos - _renderBackbuffer.Size / 2, Color.GreenYellow.WithAlpha(0.75f));
    }
}
