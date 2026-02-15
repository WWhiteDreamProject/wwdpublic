using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Content.Shared.Ghost; // WWDP EDIT

namespace Content.Client.Interactable.Components
{
    [RegisterComponent]
    public sealed partial class InteractionOutlineComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const float DefaultWidth = 1;
        private const float DefaultAlphaCutoff = 0f; // WWDP EDIT
        private const float GhostAlphaCutoff = 0.02f; // WWDP EDIT

        [ValidatePrototypeId<ShaderPrototype>]
        private const string ShaderInRange = "SelectionOutlineInrange";

        [ValidatePrototypeId<ShaderPrototype>]
        private const string ShaderOutOfRange = "SelectionOutline";

        private bool _inRange;
        private ShaderInstance? _shader;
        private ShaderInstance? _previousPostShader; // WWDP EDIT
        private int _lastRenderScale;

        public void OnMouseEnter(EntityUid uid, bool inInteractionRange, int renderScale)
        {
            _lastRenderScale = renderScale;
            _inRange = inInteractionRange;

            // WWDP EDIT START
            if (!_entMan.TryGetComponent(uid, out SpriteComponent? sprite))
                return;

            if (_shader != null && sprite.PostShader == _shader)
                return;

            _previousPostShader = sprite.PostShader;
            _shader?.Dispose();
            _shader = MakeNewShader(inInteractionRange, renderScale);
            sprite.PostShader = _shader;
            // WWDP EDIT END
        }

        public void OnMouseLeave(EntityUid uid)
        {
            if (_entMan.TryGetComponent(uid, out SpriteComponent? sprite))
            {
                if (sprite.PostShader == _shader)
                    sprite.PostShader = _previousPostShader; // WWDP EDIT
            }

            _shader?.Dispose();
            _shader = null;
            _previousPostShader = null; // WWDP EDIT
        }

        public void UpdateInRange(EntityUid uid, bool inInteractionRange, int renderScale)
        {
            if (_entMan.TryGetComponent(uid, out SpriteComponent? sprite)
                && sprite.PostShader == _shader
                && (inInteractionRange != _inRange || _lastRenderScale != renderScale))
            {
                _inRange = inInteractionRange;
                _lastRenderScale = renderScale;

                _shader?.Dispose(); // WWDP EDIT
                _shader = MakeNewShader(_inRange, _lastRenderScale);
                sprite.PostShader = _shader;
            }
        }

        private ShaderInstance MakeNewShader(bool inRange, int renderScale)
        {
            var shaderName = inRange ? ShaderInRange : ShaderOutOfRange;

            var instance = _prototypeManager.Index<ShaderPrototype>(shaderName).InstanceUnique();
            instance.SetParameter("outline_width", DefaultWidth * renderScale);
            instance.SetParameter("alpha_cutoff", _entMan.HasComponent<GhostComponent>(Owner) ? GhostAlphaCutoff : DefaultAlphaCutoff); // WWDP EDIT
            return instance;
        }
    }
}
