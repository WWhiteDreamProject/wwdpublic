using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._White.VisionLimit.Overlays

{
    public sealed class VisionLimitOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public float VisionLimitRadius;

        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _circleMaskShader;

        public VisionLimitOverlay()
        {
            IoCManager.InjectDependencies(this);
            _circleMaskShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            var playerEntity = _playerManager.LocalSession?.AttachedEntity;

            if (playerEntity == null)
                return;

            if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var content))
            {
                _circleMaskShader.SetParameter("Zoom", content.Zoom.X);

                _circleMaskShader.SetParameter("CircleRadius", 6.5f); // It's relative, close enough to
                _circleMaskShader.SetParameter("CircleMinDist", VisionLimitRadius * 10); // 1 unit = 1 tile
            }

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;

            worldHandle.UseShader(_circleMaskShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }
}
