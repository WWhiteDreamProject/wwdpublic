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

public sealed class MappingVisibilityUIController : UIController
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

    private bool _entitiesVisible = true;
    private bool _tilesVisible = true;
    private bool _decalsVisible = true;

    private float _decalRotation;
    private bool _decalAuto;
    private bool _decalEnableColor;
    private bool _decalSnap;
    private bool _decalCleanable;
    private int _decalZIndex;
    private string? _id;

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
            return;
        }

        if (_mappingScreen.DecalSpinBoxContainer != null)
        {
            var rotationSpinBox = new FloatSpinBox(90.0f, 0)
            {
                HorizontalExpand = true
            };
            _mappingScreen.DecalSpinBoxContainer.AddChild(rotationSpinBox);

            if (_mappingScreen.DecalColorPicker != null)
                _mappingScreen.DecalColorPicker.OnColorChanged += OnDecalColorPicked;

            if (_mappingScreen.DecalPickerOpen != null)
                _mappingScreen.DecalPickerOpen.OnPressed += OnDecalPickerOpenPressed;

            rotationSpinBox.OnValueChanged += args =>
            {
                _decalRotation = args.Value;
                UpdateDecal();
            };

            if (_mappingScreen.DecalEnableAuto != null)
                _mappingScreen.DecalEnableAuto.OnToggled += args =>
                {
                    _decalAuto = args.Pressed;
                    if (_id is { } id)
                        SelectDecal(id);
                };

            if (_mappingScreen.DecalEnableColor != null)
                _mappingScreen.DecalEnableColor.OnToggled += args =>
                {
                    _decalEnableColor = args.Pressed;
                    UpdateDecal();
                    RefreshDecalList();
                };

            if (_mappingScreen.DecalEnableSnap != null)
                _mappingScreen.DecalEnableSnap.OnToggled += args =>
                {
                    _decalSnap = args.Pressed;
                    UpdateDecal();
                };

            if (_mappingScreen.DecalEnableCleanable != null)
                _mappingScreen.DecalEnableCleanable.OnToggled += args =>
                {
                    _decalCleanable = args.Pressed;
                    UpdateDecal();
                };

            if (_mappingScreen.DecalZIndexSpinBox != null)
                _mappingScreen.DecalZIndexSpinBox.ValueChanged += args =>
                {
                    _decalZIndex = args.Value;
                    UpdateDecal();
                };
        }

        _window.EntitiesPanel.Pressed = _entitiesVisible;
        _window.EntitiesPanel.OnPressed += OnToggleEntitiesPanelPressed;

        _window.TilesPanel.Pressed = _tilesVisible;
        _window.TilesPanel.OnPressed += OnToggleTilesPanelPressed;

        _window.DecalsPanel.Pressed = _decalsVisible;
        _window.DecalsPanel.OnPressed += OnToggleDecalsPanelPressed;

        _window.Light.Pressed = _lightManager.Enabled;
        _window.Light.OnPressed += args => _lightManager.Enabled = args.Button.Pressed;

        _window.Fov.Pressed = _eyeManager.CurrentEye.DrawFov;
        _window.Fov.OnPressed += args => _eyeManager.CurrentEye.DrawFov = args.Button.Pressed;

        _window.Shadows.Pressed = _lightManager.DrawShadows;
        _window.Shadows.OnPressed += args => _lightManager.DrawShadows = args.Button.Pressed;

        _window.Entities.Pressed = _entitiesVisible;
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

        _window.Decals.Pressed = _decalsVisible;
        _window.Decals.OnPressed += OnToggleDecalsLayerPressed;

        _window.SubFloor.Pressed = _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll;
        _window.SubFloor.OnPressed += OnToggleSubfloorPressed;

        _window.Cables.Pressed = true;
        _window.Cables.OnPressed += args => ToggleWithTag(args, CableTag);

        _window.Disposal.Pressed = true;
        _window.Disposal.OnPressed += args => ToggleWithTag(args, DisposalTag);

        _window.Atmos.Pressed = true;
        _window.Atmos.OnPressed += ToggleWithComp<PipeAppearanceComponent>;

        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
    }

    private void OnToggleEntitiesPanelPressed(BaseButton.ButtonEventArgs args)
    {
        _entitiesVisible = args.Button.Pressed;
        if (_mappingScreen != null)
        {
            if (_mappingScreen.SpawnContainer != null)
            {
                _mappingScreen.SpawnContainer.Visible = args.Button.Pressed;
            }
        }
    }

    private void OnToggleTilesPanelPressed(BaseButton.ButtonEventArgs args)
    {
        _tilesVisible = args.Button.Pressed;
        if (_mappingScreen != null)
        {
            UpdatePanelsVisibility();
        }
    }

    private void OnToggleDecalsPanelPressed(BaseButton.ButtonEventArgs args)
    {
        _decalsVisible = args.Button.Pressed;
        if (_mappingScreen != null)
        {
            UpdatePanelsVisibility();
        }
    }

    private void UpdatePanelsVisibility()
    {
        if (_mappingScreen?.RightContainer == null) return;

        var rightContainer = _mappingScreen.RightContainer;
        var panelsContainer = rightContainer.GetChild(1) as BoxContainer;
        if (panelsContainer == null) return;

        bool anyVisible = _tilesVisible || _decalsVisible;
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
                    panel.Visible = _tilesVisible;
                    panel.VerticalExpand = _tilesVisible && !_decalsVisible;
                    tilesPanel = panel;
                    break;

                case PanelContainer panel when panel.Name == "DecalsPanel":
                    panel.Visible = _decalsVisible;
                    if (_tilesVisible && tilesPanel != null)
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
                    box.Visible = _decalsVisible;
                    for (int j = 0; j < box.ChildCount; j++)
                    {
                        var settingChild = box.GetChild(j);
                        if (settingChild == null) continue;
                        
                        settingChild.Visible = _decalsVisible;
                        if (settingChild is PanelContainer settingPanel)
                        {
                            settingPanel.VerticalExpand = _decalsVisible;
                        }
                    }
                    break;

                case Control line when line.GetType().Name.Contains("HLine"):
                    if (i == 1)
                    {
                        line.Visible = _tilesVisible && _decalsVisible;
                    }
                    else if (i == 3)
                    {
                        line.Visible = _decalsVisible;
                    }
                    else if (i == 5)
                    {
                        line.Visible = _decalsVisible;
                    }
                    break;

                case BoxContainer box when box.Name == "BottomButtons":
                    box.Visible = anyVisible;
                    var eraseTileButton = box.GetChild(0);
                    var eraseDecalButton = box.GetChild(1);
                    
                    if (eraseTileButton is Button tileButton)
                    {
                        tileButton.Visible = _tilesVisible;
                    }
                    
                    if (eraseDecalButton is Button decalButton)
                    {
                        decalButton.Visible = _decalsVisible;
                    }
                    break;
            }
        }

        if (_decalsVisible)
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

    private void OnDecalColorPicked(Color color)
    {
        // Implement color picking logic
    }

    private void OnDecalPickerOpenPressed(BaseButton.ButtonEventArgs args)
    {
        // Implement color picker opening logic
    }

    private void UpdateDecal()
    {
        // Implement decal update logic
    }

    private void RefreshDecalList()
    {
        // Implement decal list refresh logic
    }

    private void SelectDecal(string id)
    {
        // Implement decal selection logic
    }
}
