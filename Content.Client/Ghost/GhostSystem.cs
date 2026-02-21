using System; // WWDP EDIT
using Content.Client.Movement.Systems;
using Content.Shared.Actions;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Graphics; // WWDP EDIT
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes; // WWDP EDIT

namespace Content.Client.Ghost
{
    public sealed class GhostSystem : SharedGhostSystem
    {
        // WWDP EDIT START
        private sealed class GhostShaderData(ShaderInstance shader, string shaderName)
        {
            public ShaderInstance Shader = shader;
            public string ShaderName = shaderName;
        }
        // WWDP EDIT END

        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!; // WWDP EDIT
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
        [Dependency] private readonly ContentEyeSystem _contentEye = default!;

        private readonly Dictionary<EntityUid, GhostShaderData> _compositeGhostShaders = new(); // WWDP EDIT

        public int AvailableGhostRoleCount { get; private set; }

        private bool _ghostVisibility = true;

        private bool GhostVisibility
        {
            get => _ghostVisibility;
            set
            {
                if (_ghostVisibility == value)
                {
                    return;
                }

                _ghostVisibility = value;

                var query = AllEntityQuery<GhostComponent, SpriteComponent>();
                while (query.MoveNext(out var uid, out _, out var sprite))
                {
                    sprite.Visible = value || uid == _playerManager.LocalEntity;
                }
            }
        }

        public GhostComponent? Player => CompOrNull<GhostComponent>(_playerManager.LocalEntity);
        public bool IsGhost => Player != null;

        public event Action<GhostComponent>? PlayerRemoved;
        public event Action<GhostComponent>? PlayerUpdated;
        public event Action<GhostComponent>? PlayerAttached;
        public event Action? PlayerDetached;
        public event Action<GhostWarpsResponseEvent>? GhostWarpsResponse;
        public event Action<GhostUpdateGhostRoleCountEvent>? GhostRoleCountUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);
            SubscribeLocalEvent<GhostComponent, AfterAutoHandleStateEvent>(OnGhostState);
            SubscribeLocalEvent<VisualObserverComponent, AfterAutoHandleStateEvent>(OnVisualObserverState); // WWDP EDIT

            SubscribeLocalEvent<GhostComponent, LocalPlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, LocalPlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);

            SubscribeLocalEvent<EyeComponent, ToggleLightingActionEvent>(OnToggleLighting);
            SubscribeLocalEvent<EyeComponent, ToggleFoVActionEvent>(OnToggleFoV);
            SubscribeLocalEvent<GhostComponent, ToggleGhostsActionEvent>(OnToggleGhosts);
        }

        private void OnStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // WWDP EDIT START
            if (TryComp(uid, out SpriteComponent? sprite))
            {
                sprite.Visible = GhostVisibility || uid == _playerManager.LocalEntity;
                ApplyGhostVisuals(uid, component, sprite);
            }
            // WWDP EDIT END
        }

        private void OnToggleLighting(EntityUid uid, EyeComponent component, ToggleLightingActionEvent args)
        {
            if (args.Handled)
                return;

            TryComp<PointLightComponent>(uid, out var light);

            if (!component.DrawLight)
            {
                // normal lighting
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-normal"), args.Performer);
                _contentEye.RequestEye(component.DrawFov, true);
            }
            else if (!light?.Enabled ?? false) // skip this option if we have no PointLightComponent
            {
                // enable personal light
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-personal-light"), args.Performer);
                _pointLightSystem.SetEnabled(uid, true, light);
            }
            else
            {
                // fullbright mode
                Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup-fullbright"), args.Performer);
                _contentEye.RequestEye(component.DrawFov, false);
                _pointLightSystem.SetEnabled(uid, false, light);
            }
            args.Handled = true;
        }

        private void OnToggleFoV(EntityUid uid, EyeComponent component, ToggleFoVActionEvent args)
        {
            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-fov-popup"), args.Performer);
            _contentEye.RequestToggleFov(uid, component);
            args.Handled = true;
        }

        private void OnToggleGhosts(EntityUid uid, GhostComponent component, ToggleGhostsActionEvent args)
        {
            if (args.Handled)
                return;

            var locId = GhostVisibility ? "ghost-gui-toggle-ghost-visibility-popup-off" : "ghost-gui-toggle-ghost-visibility-popup-on";
            Popup.PopupEntity(Loc.GetString(locId), args.Performer);
            if (uid == _playerManager.LocalEntity)
                ToggleGhostVisibility();

            args.Handled = true;
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            RemoveGhostCompositeShader(uid); // WWDP EDIT

            _actions.RemoveAction(uid, component.ToggleLightingActionEntity);
            _actions.RemoveAction(uid, component.ToggleFoVActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostsActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostHearingActionEntity);

            if (uid != _playerManager.LocalEntity)
                return;

            GhostVisibility = false;
            PlayerRemoved?.Invoke(component);
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, LocalPlayerAttachedEvent localPlayerAttachedEvent)
        {
            GhostVisibility = true;
            PlayerAttached?.Invoke(component);
        }

        private void OnGhostState(EntityUid uid, GhostComponent component, ref AfterAutoHandleStateEvent args)
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
                ApplyGhostVisuals(uid, component, sprite); // WWDP EDIT

            if (uid != _playerManager.LocalEntity)
                return;

            PlayerUpdated?.Invoke(component);
        }

        // WWDP EDIT START
        private void OnVisualObserverState(EntityUid uid, VisualObserverComponent component, ref AfterAutoHandleStateEvent args)
        {
            if (!TryComp<GhostComponent>(uid, out var ghost) ||
                !TryComp<SpriteComponent>(uid, out var sprite))
            {
                return;
            }

            ApplyGhostVisuals(uid, ghost, sprite);
        }
        // WWDP EDIT END

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, LocalPlayerDetachedEvent args)
        {
            GhostVisibility = false;
            PlayerDetached?.Invoke();
        }

        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            if (!IsGhost)
            {
                return;
            }

            GhostWarpsResponse?.Invoke(msg);
        }

        private void OnUpdateGhostRoleCount(GhostUpdateGhostRoleCountEvent msg)
        {
            AvailableGhostRoleCount = msg.AvailableGhostRoles;
            GhostRoleCountUpdated?.Invoke(msg);
        }

        public void RequestWarps()
        {
            RaiseNetworkEvent(new GhostWarpsRequestEvent());
        }

        public void ReturnToBody()
        {
            var msg = new GhostReturnToBodyRequest();
            RaiseNetworkEvent(msg);
        }

        public void OpenGhostRoles()
        {
            _console.RemoteExecuteCommand(null, "ghostroles");
        }

        public void GhostBarSpawn() // Goobstation - Ghost Bar
        {
            RaiseNetworkEvent(new GhostBarSpawnEvent());
        }

        public void ToggleGhostVisibility(bool? visibility = null)
        {
            GhostVisibility = visibility ?? !GhostVisibility;
        }

        public void ReturnToRound()
        {
            var msg = new GhostReturnToRoundRequest();
            RaiseNetworkEvent(msg);
        }

        // WWDP EDIT START
        private void ApplyGhostVisuals(EntityUid uid, GhostComponent component, SpriteComponent sprite)
        {
            if (TryComp(uid, out VisualObserverComponent? visualObserver))
            {
                var shader = EnsureGhostCompositeShader(uid, sprite, visualObserver.ShaderName);
                if (shader == null)
                {
                    sprite.Color = component.Color;
                    return;
                }

                shader.SetParameter("ghost_tint", new Robust.Shared.Maths.Vector3(component.Color.R, component.Color.G, component.Color.B));
                shader.SetParameter("ghost_alpha", Math.Clamp(component.Color.A * visualObserver.AlphaMultiplier, 0f, 1f));
                sprite.Color = Color.White;
                return;
            }

            RemoveGhostCompositeShader(uid, sprite);
            sprite.Color = component.Color;
        }

        private ShaderInstance? EnsureGhostCompositeShader(EntityUid uid, SpriteComponent sprite, string shaderName)
        {
            if (!_prototype.TryIndex<ShaderPrototype>(shaderName, out var shaderPrototype))
            {
                RemoveGhostCompositeShader(uid, sprite);
                return null;
            }

            if (!_compositeGhostShaders.TryGetValue(uid, out var shaderData))
            {
                var shader = shaderPrototype.InstanceUnique();
                shaderData = new GhostShaderData(shader, shaderName);
                _compositeGhostShaders[uid] = shaderData;
            }
            else if (shaderData.ShaderName != shaderName)
            {
                if (sprite.PostShader == shaderData.Shader)
                    sprite.PostShader = null;

                shaderData.Shader.Dispose();
                shaderData.Shader = shaderPrototype.InstanceUnique();
                shaderData.ShaderName = shaderName;
            }

            if (sprite.PostShader != shaderData.Shader)
                sprite.PostShader = shaderData.Shader;

            return shaderData.Shader;
        }

        private void RemoveGhostCompositeShader(EntityUid uid, SpriteComponent? sprite = null)
        {
            if (!_compositeGhostShaders.Remove(uid, out var shaderData))
                return;

            if (sprite == null)
                TryComp(uid, out sprite);

            if (sprite != null && sprite.PostShader == shaderData.Shader)
                sprite.PostShader = null;

            shaderData.Shader.Dispose();
        }
        // WWDP EDIT END
    }
}
