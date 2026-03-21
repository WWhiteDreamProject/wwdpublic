using System.Numerics;
using Content.Shared._NC.Trail;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._NC.Trail;

public sealed class TrailOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _protoMan;
    private readonly IGameTiming _timing;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    public TrailOverlay(IEntityManager entManager, IPrototypeManager protoMan, IGameTiming timing)
    {
        ZIndex = (int) DrawDepth.Effects;

        _entManager = entManager;
        _protoMan = protoMan;
        _timing = timing;
        _sprite = _entManager.System<SpriteSystem>();
        _transform = _entManager.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye;

        if (eye == null)
            return;

        var eyeRot = eye.Rotation;
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();

        var query = _entManager.EntityQueryEnumerator<TrailComponent, TransformComponent>();
        while (query.MoveNext(out _, out var trail, out var xform))
        {
            if (trail.TrailData.Count == 0)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform, xformQuery);

            if (trail.Shader != null && _protoMan.TryIndex<ShaderPrototype>(trail.Shader, out var shaderProto))
            {
                var shader = shaderProto.InstanceUnique();
                handle.UseShader(shader);
            }
            else
                handle.UseShader(null);

            if (trail.RenderedEntity != null)
            {
                if (spriteQuery.TryComp(trail.RenderedEntity.Value, out var sprite))
                {
                    handle.SetTransform(Matrix3x2.Identity);
                    foreach (var data in trail.TrailData)
                    {
                        if (data.Color.A <= 0.01f || data.Scale <= 0.01f || data.MapId != args.MapId)
                            continue;

                        var worldPosition = data.Position;
                        if (!bounds.Contains(worldPosition))
                            continue;

                        // Сохраняем оригинальные параметры для восстановления
                        var originalColor = sprite.Color;
                        var originalScale = sprite.Scale;
                        
                        sprite.Color = data.Color;
                        sprite.Scale *= data.Scale;
                        
                        // Рендерим спрайт в позиции снэпшота с оригинальным поворотом снэпшота
                        sprite.Render(handle, eyeRot, data.Angle, null, worldPosition);
                        
                        sprite.Color = originalColor;
                        sprite.Scale = originalScale;
                    }
                }
                continue;
            }

            // Sprite-based trail
            if (trail.Sprite != null)
            {
                var textureSize = _sprite.Frame0(trail.Sprite).Size;
                var pos = -(Vector2) textureSize / 2f / 32f;
                foreach (var data in trail.TrailData)
                {
                    if (data.Color.A <= 0.01f || data.Scale <= 0.01f || data.MapId != args.MapId)
                        continue;

                    if (!bounds.Contains(data.Position))
                        continue;

                    var scaleMatrix = Matrix3x2.CreateScale(new Vector2(data.Scale, data.Scale));
                    var worldMatrix = Matrix3Helpers.CreateTranslation(data.Position);

                    var time = _timing.CurTime > data.SpawnTime ? _timing.CurTime - data.SpawnTime : TimeSpan.Zero;
                    var texture = _sprite.GetFrame(trail.Sprite, time);

                    handle.SetTransform(Matrix3x2.Multiply(scaleMatrix, worldMatrix));
                    handle.DrawTexture(texture, pos, data.Angle, data.Color);
                }
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
