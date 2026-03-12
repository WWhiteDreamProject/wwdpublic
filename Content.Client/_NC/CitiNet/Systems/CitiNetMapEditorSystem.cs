using System.Numerics;
using Content.Shared._NC.CitiNet;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._NC.CitiNet.Systems;

/// <summary>
/// Client-side system that visualizes CitiNet Map Sectors in the world for mappers.
/// </summary>
public sealed class CitiNetMapEditorSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly Robust.Client.GameObjects.SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new MapSectorOverlay(EntityManager, _eye));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<MapSectorOverlay>();
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<MapSectorComponent, Robust.Client.GameObjects.SpriteComponent>();
        while (query.MoveNext(out var uid, out var sector, out var sprite))
        {
            if (sprite.Visible != sector.VisibleInWorld)
            {
                _sprite.SetVisible((uid, sprite), sector.VisibleInWorld);
            }
        }
    }
}

public sealed class MapSectorOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MapSectorOverlay(IEntityManager entManager, IEyeManager eye)
    {
        _entManager = entManager;
        _eye = eye;
        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var query = _entManager.EntityQueryEnumerator<MapSectorComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var sector, out var xform))
        {
            // IMPORTANT: Only draw if the mapper has set it to be visible in world
            if (!sector.VisibleInWorld)
                continue;

            if (xform.MapID != args.Viewport.Eye?.Position.MapId)
                continue;

            // Draw the sector bounds relative to the entity
            var worldMatrix = _transform.GetWorldMatrix(xform);
            handle.SetTransform(worldMatrix);
            
            // Draw filled rect (semi-transparent)
            handle.DrawRect(sector.Bounds, sector.Color.WithAlpha(0.2f));
            
            // Draw outline
            handle.DrawRect(sector.Bounds, sector.Color, false);
            
            // Draw anchor point for easier selection
            handle.DrawCircle(Vector2.Zero, 0.1f, sector.Color);
        }
        
        handle.SetTransform(Matrix3x2.Identity);
    }
}
