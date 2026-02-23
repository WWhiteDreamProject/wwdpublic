using Content.Shared._White.AltExamine;
using Content.Shared.Input;
using Content.Shared.Wall;
using Content.Shared._White.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared._White.RenderOrderSystem;
using System.Numerics;
using Direction = Robust.Shared.Maths.Direction;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._White.AltExamine
{
    public sealed class AltExamineSystem : EntitySystem
    {
        [Dependency] private readonly InputSystem _inputSystem = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly SharedRenderOrderSystem _renderOrder = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private ISawmill _sawmill = default!;
        private float _holdTimer = 0f;
        private float _holdThreshold = 0.3f;
        private bool _effectsApplied = false;

        private readonly Dictionary<EntityUid, (
            Vector2 scale,
            Direction dir,
            bool enableOverride,
            Vector2 offset,
            ShaderInstance? postShader,
            Color color,
            float alpha,
            int drawDepth
        )> _originalValues = new();

        public override void Initialize()
        {
            _sawmill = Logger.GetSawmill("alt_examine");
            SubscribeLocalEvent<AltExamineComponent, ComponentShutdown>(OnShutdown);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.AltExamine, InputCmdHandler.FromDelegate(session => { }, handle: false, outsidePrediction: true))
                .Register<AltExamineSystem>();

            InitializeCVars();
        }

        private void InitializeCVars()
        {
            Subs.CVar(_cfg, WhiteCVars.AltExamineHoldThreshold, value => _holdThreshold = value, true);
        }

        private void OnShutdown(EntityUid uid, AltExamineComponent comp, ComponentShutdown args)
        {
            if (_originalValues.ContainsKey(uid))
            {
                RestoreEffects();
            }
        }

        public override void Update(float frameTime)
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            var altDown = _inputSystem.CmdStates.GetState(ContentKeyFunctions.AltExamine) == BoundKeyState.Down;

            if (altDown)
            {
                if (!_effectsApplied)
                {
                    _holdTimer += frameTime;

                    if (_holdTimer >= _holdThreshold)
                    {
                        _effectsApplied = true;
                        var query = EntityManager.AllEntityQueryEnumerator<AltExamineComponent, SpriteComponent, TransformComponent>();
                        while (query.MoveNext(out var uid, out var comp, out var sprite, out var xform))
                        {
                            ApplyEffects(uid, comp, sprite, xform);
                        }
                    }
                }
            }
            else
            {
                _holdTimer = 0f;
                if (_effectsApplied)
                {
                    _effectsApplied = false;
                    RestoreEffects();
                }
            }
        }

        private void ApplyEffects(EntityUid uid, AltExamineComponent comp, SpriteComponent sprite, TransformComponent xform)
        {
            if (!_originalValues.ContainsKey(uid))
            {
                _originalValues[uid] = (
                    sprite.Scale,
                    sprite.DirectionOverride,
                    sprite.EnableDirectionOverride,
                    sprite.Offset,
                    sprite.PostShader,
                    sprite.Color,
                    sprite.Color.A,
                    sprite.DrawDepth
                );
            }

            if (comp.EnableOverride)
            {
                sprite.EnableDirectionOverride = true;
                sprite.DirectionOverride = comp.OverrideDirection;
            }

            if (comp.Scale.HasValue)
                sprite.Scale = comp.Scale.Value;

            if (comp.Color.HasValue)
                sprite.Color = comp.Color.Value;

            if (comp.Alpha.HasValue)
                sprite.Color = sprite.Color.WithAlpha(comp.Alpha.Value);

            Vector2 offsetVec;
            if (comp.UseAltCalc && HasComp<WallMountComponent>(uid))
            {
                if (TryComp<WallMountComponent>(uid, out var wallMount))
                {
                    offsetVec = wallMount.Direction.ToWorldVec() * comp.OffsetDistance;
                }
                else
                {
                    offsetVec = Vector2.Zero;
                }
            }
            else
            {
                var worldDir = xform.LocalRotation.GetCardinalDir();
                offsetVec = DirectionToVector(worldDir) * comp.OffsetDistance;
            }

            sprite.Offset = _originalValues[uid].offset + offsetVec;

            if (comp.OutlineColor.HasValue)
            {
                if (_protoMan.TryIndex<ShaderPrototype>(comp.OutlineShader, out var outlineProto))
                {
                    var outline = outlineProto.InstanceUnique();
                    outline.SetParameter("outline_color", new Robust.Shared.Maths.Vector4(
                        comp.OutlineColor.Value.R,
                        comp.OutlineColor.Value.G,
                        comp.OutlineColor.Value.B,
                        comp.OutlineColor.Value.A));
                    sprite.PostShader = outline;
                }
            }

            sprite.DrawDepth = (int)DrawDepth.Overlays;
            _renderOrder.MoveToTop(uid, nameof(AltExamineSystem));
        }

        private void RestoreEffects()
        {
            foreach (var (uid, orig) in _originalValues)
            {
                if (!TryComp(uid, out SpriteComponent? sprite))
                    continue;

                sprite.Scale = orig.scale;
                sprite.DirectionOverride = orig.dir;
                sprite.EnableDirectionOverride = orig.enableOverride;
                sprite.Offset = orig.offset;
                sprite.Color = orig.color;
                sprite.Color = sprite.Color.WithAlpha(orig.alpha);
                sprite.PostShader = null;
                sprite.DrawDepth = orig.drawDepth;
                _renderOrder.UnsetRenderOrder(uid, nameof(AltExamineSystem));
            }
            _originalValues.Clear();
        }

        private static Vector2 DirectionToVector(Direction dir) => dir switch
        {
            Direction.North => new Vector2(0, 1),
            Direction.South => new Vector2(0, -1),
            Direction.East => new Vector2(1, 0),
            Direction.West => new Vector2(-1, 0),
            _ => Vector2.Zero
        };

        public override void Shutdown()
        {
            CommandBinds.Unregister<AltExamineSystem>();
            RestoreEffects();
            base.Shutdown();
        }
    }
}
