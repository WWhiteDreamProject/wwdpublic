using System.Numerics;
using Content.Shared._NC.CitiNet;
using Content.Client.Administration.Managers;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._NC.CitiNet.Systems;

/// <summary>
/// Client-side system that visualizes CitiNet Map Sectors and Beacons in the world for mappers.
/// Overlay is ONLY visible to admins to avoid cluttering normal gameplay.
/// </summary>
public sealed class CitiNetMapEditorSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new MapSectorOverlay(EntityManager, _eye, _admin));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<MapSectorOverlay>();
    }
}

public sealed class MapSectorOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IEyeManager _eye;
    private readonly IClientAdminManager _admin;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MapSectorOverlay(IEntityManager entManager, IEyeManager eye, IClientAdminManager admin)
    {
        _entManager = entManager;
        _eye = eye;
        _admin = admin;
        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // IMPORTANT: Only draw for Admins/Mappers
        if (!_admin.IsActive())
            return;

        var handle = args.WorldHandle;
        
        // --- DRAW SECTORS ---
        var sectorQuery = _entManager.EntityQueryEnumerator<MapSectorComponent, TransformComponent>();
        while (sectorQuery.MoveNext(out var uid, out var sector, out var xform))
        {
            if (!sector.VisibleInWorld)
                continue;

            if (xform.MapID != args.Viewport.Eye?.Position.MapId)
                continue;

            var worldMatrix = _transform.GetWorldMatrix(xform);
            handle.SetTransform(worldMatrix);
            
            handle.DrawRect(sector.Bounds, sector.Color.WithAlpha(0.2f));
            handle.DrawRect(sector.Bounds, sector.Color, false);
            handle.DrawCircle(Vector2.Zero, 0.1f, sector.Color);
        }

        // --- DRAW BEACONS ---
        var beaconQuery = _entManager.EntityQueryEnumerator<MapBeaconComponent, TransformComponent>();
        var spriteSys = _entManager.System<Robust.Client.GameObjects.SpriteSystem>();

        while (beaconQuery.MoveNext(out var uid, out var beacon, out var xform))
        {
            if (!beacon.VisibleInWorld)
                continue;

            if (xform.MapID != args.Viewport.Eye?.Position.MapId)
                continue;

            var worldPos = _transform.GetWorldPosition(xform);
            
            handle.SetTransform(Matrix3x2.Identity);
            handle.DrawCircle(worldPos, 0.2f, beacon.Color);

            if (beacon.Icon != null)
            {
                var texture = spriteSys.Frame0(beacon.Icon);
                if (texture != null)
                {
                    var rect = Box2.CenteredAround(worldPos, new Vector2(0.4f, 0.4f));
                    handle.DrawTextureRect(texture, rect, beacon.Color.WithAlpha(0.8f));
                }
            }
        }
        
        handle.SetTransform(Matrix3x2.Identity);
    }
}
