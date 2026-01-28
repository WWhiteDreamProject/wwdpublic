using Content.Client.Pinpointer.UI;
using Content.Shared._NC.Netrunning.UI;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Client.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Robust.Shared.Input;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Client.UserInterface;
using System;
using System.Numerics;

namespace Content.Client._NC.Netrunning.UI;

public sealed partial class NetMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;

    public event Action<NetEntity, NetMapAction>? OnInteract;

    private SpriteSystem _spriteSys;
    private Dictionary<NetEntity, NetBlipType> _blipTypes = new();

    public NetMapControl() : base()
    {
        IoCManager.InjectDependencies(this);
        _spriteSys = _entManager.System<SpriteSystem>();

        // Cyberpunk Radar Style
        WallColor = Color.FromHex("#004400"); // Dark Green
        TileColor = Color.FromHex("#001100"); // Very Dark Green
        BackgroundColor = Color.FromHex("#000500"); // Almost Black
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.UIRightClick)
            return;

        if (MapUid == null || TrackedEntities.Count == 0)
            return;

        if (!_entManager.TryGetComponent(MapUid, out TransformComponent? xform) ||
            !_entManager.TryGetComponent(MapUid, out PhysicsComponent? physics))
            return;

        // Blip Hit Test
        var offset = Offset + physics.LocalCenter;
        var localPosition = args.PointerLocation.Position - GlobalPixelPosition;
        var unscaledPosition = (localPosition - MidPointVector) / MinimapScale;

        // Use dependencies for System access
        var xformSys = _entManager.System<SharedTransformSystem>();
        var worldPosition = Vector2.Transform(new Vector2(unscaledPosition.X, -unscaledPosition.Y) + offset, xformSys.GetWorldMatrix(xform));

        var closestEntity = NetEntity.Invalid;
        var closestDistance = float.PositiveInfinity;

        foreach ((var currentEntity, var blip) in TrackedEntities)
        {
            if (!blip.Selectable) continue;

            var blipPos = _entManager.System<SharedTransformSystem>().ToMapCoordinates(blip.Coordinates).Position;
            var dist = (blipPos - worldPosition).Length();

            if (closestDistance > dist && dist * MinimapScale <= MaxSelectableDistance)
            {
                closestEntity = currentEntity;
                closestDistance = dist;
            }
        }

        if (closestEntity == NetEntity.Invalid) return;

        // Determine Action
        var type = _blipTypes.GetValueOrDefault(closestEntity, NetBlipType.Generic);
        var action = NetMapAction.Toggle; // Default

        if (args.Function == EngineKeyFunctions.UIClick) // Left Click
        {
            if (type == NetBlipType.Mob) action = NetMapAction.Attack;
            else if (type == NetBlipType.Door) action = NetMapAction.Toggle; // Open/Close logic handled by server toggle
            else if (type == NetBlipType.Camera) action = NetMapAction.ViewFeed;
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick) // Right Click
        {
            if (type == NetBlipType.Door) action = NetMapAction.Bolt; // Or Context Menu later
            else if (type == NetBlipType.Mob) action = NetMapAction.Tag;
        }

        OnInteract?.Invoke(closestEntity, action);
    }

    public void UpdateState(NetMapBoundUiState state)
    {
        if (state.TargetGrid == null) return;

        var netGrid = state.TargetGrid.Value;
        var gridUid = _entManager.GetEntity(netGrid);

        // Update MapUid for NavMapControl to render walls
        MapUid = gridUid;
        ForceNavMapUpdate();

        // Update Blips
        TrackedEntities.Clear();
        _blipTypes.Clear();

        foreach (var blip in state.Blips)
        {
            // Convert NetMapBlip to NavMapBlip
            var coords = _entManager.GetCoordinates(blip.Coordinates);
            _blipTypes[blip.Entity] = blip.BlipType;

            // Resolve Texture based on Type
            Texture? texture = null;
            if (blip.BlipType == NetBlipType.Door)
            {
                texture = _spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Doors/Airlocks/Standard/basic.rsi"), "closed"));
            }
            else if (blip.BlipType == NetBlipType.Camera)
            {
                texture = _spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Wallmounts/camera.rsi"), "camera"));
            }
            else if (blip.BlipType == NetBlipType.Mob)
            {
                // Simple bullet/dot for mobs
                texture = _spriteSys.Frame0(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png")));
            }
            else
            {
                // Server/Generic
                texture = _spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Machines/server.rsi"), "server"));
            }

            if (texture != null)
            {
                // Enable Selectable=true
                TrackedEntities[blip.Entity] = new NavMapBlip(coords, texture, blip.Color, blip.IsBlinking, true);
            }
        }
    }
}
