using Content.Client.Decals;
using Content.Client.Markers;
using Content.Client.SubFloor;
using Content.Shared.Atmos.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Client.UserInterface;

namespace Content.Client.Mapping;

public sealed partial class MappingVisibilityUIController : UIController
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private MappingVisibilityWindow? _window;
    private MappingScreen? _mappingScreen;

    [ValidatePrototypeId<TagPrototype>]
    private const string WallTag = "Wall";

    [ValidatePrototypeId<TagPrototype>]
    private const string CableTag = "Cable";

    [ValidatePrototypeId<TagPrototype>]
    private const string DisposalTag = "Disposal";

    private readonly MappingVisibilityState _state = new();

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<MappingVisibilityWindow>();
        _mappingScreen = UIManager.ActiveScreen as MappingScreen;
        
        if (_mappingScreen == null)
        {
            Logger.Warning("Active screen is not a MappingScreen, panel visibility controls will not function.");
            DisableAllControls();
            return;
        }

        InitializeWindowControls();
    }

    private void DisableAllControls()
    {
        if (_window == null) return;
        
        _window.EntitiesPanel.Disabled = true;
        _window.TilesPanel.Disabled = true;
        _window.DecalsPanel.Disabled = true;
        _window.Light.Disabled = true;
        _window.Fov.Disabled = true;
        _window.Shadows.Disabled = true;
        _window.Entities.Disabled = true;
        _window.Markers.Disabled = true;
        _window.Walls.Disabled = true;
        _window.Airlocks.Disabled = true;
        _window.Decals.Disabled = true;
        _window.SubFloor.Disabled = true;
        _window.Cables.Disabled = true;
        _window.Disposal.Disabled = true;
        _window.Atmos.Disabled = true;
    }

    private void InitializeWindowControls()
    {
        if (_window == null || _mappingScreen == null) return;

        InitializePanelControls();
        InitializeLayerControls();
    }

    private void InitializePanelControls()
    {
        if (_window == null || _mappingScreen == null) return;

        _window.EntitiesPanel.Pressed = _state.EntitiesVisible;
        _window.EntitiesPanel.OnPressed += OnToggleEntitiesPanelPressed;

        _window.TilesPanel.Pressed = _state.TilesVisible;
        _window.TilesPanel.OnPressed += OnToggleTilesPanelPressed;

        _window.DecalsPanel.Pressed = _state.DecalsVisible;
        _window.DecalsPanel.OnPressed += OnToggleDecalsPanelPressed;
    }

    private void InitializeLayerControls()
    {
        if (_window == null) return;

        _window.Light.Pressed = _lightManager.Enabled;
        _window.Light.OnPressed += args => _lightManager.Enabled = args.Button.Pressed;

        _window.Fov.Pressed = _eyeManager.CurrentEye.DrawFov;
        _window.Fov.OnPressed += args => _eyeManager.CurrentEye.DrawFov = args.Button.Pressed;

        _window.Shadows.Pressed = _lightManager.DrawShadows;
        _window.Shadows.OnPressed += args => _lightManager.DrawShadows = args.Button.Pressed;

        _window.Entities.Pressed = _state.EntitiesVisible;
        _window.Entities.OnPressed += OnToggleEntitiesLayerPressed;

        _window.Markers.Pressed = _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible;
        _window.Markers.OnPressed += args =>
        {
            _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible = args.Button.Pressed;
        };

        _window.Walls.Pressed = true;
        _window.Walls.OnPressed += args => ToggleWithTag(args, WallTag);

        _window.Airlocks.Pressed = true;
        _window.Airlocks.OnPressed += ToggleWithComp<AirlockComponent>;

        _window.Decals.Pressed = _state.DecalsVisible;
        _window.Decals.OnPressed += OnToggleDecalsLayerPressed;

        _window.SubFloor.Pressed = _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll;
        _window.SubFloor.OnPressed += OnToggleSubfloorPressed;

        _window.Cables.Pressed = true;
        _window.Cables.OnPressed += args => ToggleWithTag(args, CableTag);

        _window.Disposal.Pressed = true;
        _window.Disposal.OnPressed += args => ToggleWithTag(args, DisposalTag);

        _window.Atmos.Pressed = true;
        _window.Atmos.OnPressed += ToggleWithComp<PipeAppearanceComponent>;
    }

    private void OnToggleEntitiesPanelPressed(BaseButton.ButtonEventArgs args)
    {
        _state.EntitiesVisible = args.Button.Pressed;
        if (_mappingScreen?.SpawnContainer != null)
        {
            _mappingScreen.SpawnContainer.Visible = args.Button.Pressed;
        }
    }

    private void OnToggleTilesPanelPressed(BaseButton.ButtonEventArgs args)
    {
        _state.TilesVisible = args.Button.Pressed;
        UpdatePanelsVisibility();
    }

    private void OnToggleDecalsPanelPressed(BaseButton.ButtonEventArgs args)
    {
        _state.DecalsVisible = args.Button.Pressed;
        UpdatePanelsVisibility();
    }

    private void UpdatePanelsVisibility()
    {
        if (_mappingScreen?.RightContainer == null) return;

        var rightContainer = _mappingScreen.RightContainer;
        var panelsContainer = rightContainer.GetChild(1) as BoxContainer;
        if (panelsContainer == null) return;

        bool anyVisible = _state.TilesVisible || _state.DecalsVisible;
        panelsContainer.Visible = anyVisible;

        if (!anyVisible)
        {
            return;
        }

        PanelContainer? tilesPanel = null;

        for (int i = 0; i < panelsContainer.ChildCount; i++)
        {
            var child = panelsContainer.GetChild(i);
            if (child == null) continue;
            
            switch (child)
            {
                case PanelContainer panel when panel.Name == "TilesPanel":
                    panel.Visible = _state.TilesVisible;
                    panel.VerticalExpand = _state.TilesVisible && !_state.DecalsVisible;
                    tilesPanel = panel;
                    break;

                case PanelContainer panel when panel.Name == "DecalsPanel":
                    panel.Visible = _state.DecalsVisible;
                    if (_state.TilesVisible && tilesPanel != null)
                    {
                        panel.VerticalExpand = true;
                        tilesPanel.VerticalExpand = true;
                    }
                    else
                    {
                        panel.VerticalExpand = true;
                    }
                    break;

                case BoxContainer box when box.Name == "DecalSettings":
                    box.Visible = _state.DecalsVisible;
                    for (int j = 0; j < box.ChildCount; j++)
                    {
                        var settingChild = box.GetChild(j);
                        if (settingChild == null) continue;
                        
                        settingChild.Visible = _state.DecalsVisible;
                        if (settingChild is PanelContainer settingPanel)
                        {
                            settingPanel.VerticalExpand = _state.DecalsVisible;
                        }
                    }
                    break;

                case Control line when line.GetType().Name.Contains("HLine"):
                    if (i == 1)
                    {
                        line.Visible = _state.TilesVisible && _state.DecalsVisible;
                    }
                    else if (i == 3)
                    {
                        line.Visible = _state.DecalsVisible;
                    }
                    else if (i == 5)
                    {
                        line.Visible = _state.DecalsVisible;
                    }
                    break;

                case BoxContainer box when box.Name == "BottomButtons":
                    box.Visible = anyVisible;
                    var eraseTileButton = box.GetChild(0);
                    var eraseDecalButton = box.GetChild(1);
                    
                    if (eraseTileButton is Button tileButton)
                    {
                        tileButton.Visible = _state.TilesVisible;
                    }
                    
                    if (eraseDecalButton is Button decalButton)
                    {
                        decalButton.Visible = _state.DecalsVisible;
                    }
                    break;
            }
        }

        if (_state.DecalsVisible)
        {
            for (int i = 0; i < panelsContainer.ChildCount; i++)
            {
                var child = panelsContainer.GetChild(i);
                if (child == null) continue;
                
                if (child is BoxContainer decalSettings && decalSettings.Name == "DecalSettings")
                {
                    for (int j = 0; j < decalSettings.ChildCount; j++)
                    {
                        var settingPanel = decalSettings.GetChild(j);
                        if (settingPanel == null) continue;
                        
                        if (settingPanel is PanelContainer panel)
                        {
                            panel.HorizontalExpand = true;
                            panel.VerticalExpand = true;
                        }
                    }
                    break;
                }
            }
        }
    }

    private void OnToggleEntitiesLayerPressed(BaseButton.ButtonEventArgs args)
    {
        var query = _entityManager.AllEntityQueryEnumerator<SpriteComponent>();

        if (args.Button.Pressed && _window != null)
        {
            _window.Markers.Pressed = true;
            _window.Walls.Pressed = true;
            _window.Airlocks.Pressed = true;
        }
        else if (_window != null)
        {
            _window.Markers.Pressed = false;
            _window.Walls.Pressed = false;
            _window.Airlocks.Pressed = false;
        }

        while (query.MoveNext(out _, out var sprite))
        {
            sprite.Visible = args.Button.Pressed;
        }
    }

    private void OnToggleDecalsLayerPressed(BaseButton.ButtonEventArgs args)
    {
        _entitySystemManager.GetEntitySystem<DecalSystem>().ToggleOverlay();
    }

    private void OnToggleSubfloorPressed(BaseButton.ButtonEventArgs args)
    {
        _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll = args.Button.Pressed;

        if (args.Button.Pressed && _window != null)
        {
            _window.Cables.Pressed = true;
            _window.Atmos.Pressed = true;
            _window.Disposal.Pressed = true;
        }
    }

    private void ToggleWithComp<TComp>(BaseButton.ButtonEventArgs args) where TComp : IComponent
    {
        var query = _entityManager.AllEntityQueryEnumerator<TComp, SpriteComponent>();

        while (query.MoveNext(out _, out _, out var sprite))
        {
            sprite.Visible = args.Button.Pressed;
        }
    }

    private void ToggleWithTag(BaseButton.ButtonEventArgs args, ProtoId<TagPrototype> tag)
    {
        var query = _entityManager.AllEntityQueryEnumerator<TagComponent, SpriteComponent>();
        var tagSystem = _entityManager.EntitySysManager.GetEntitySystem<TagSystem>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (tagSystem.HasTag(uid, tag))
                sprite.Visible = args.Button.Pressed;
        }
    }
}
