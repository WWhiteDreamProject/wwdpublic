using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Mapping;

public sealed class MappingDecalController : UIController
{
    private MappingScreen? _mappingScreen;
    private FloatSpinBox? _rotationSpinBox;

    private float _decalRotation;
    private bool _decalAuto;
    private bool _decalEnableColor;
    private bool _decalSnap;
    private bool _decalCleanable;
    private int _decalZIndex;
    private string? _id;
    private Color _decalColor = Color.White;

    public void Initialize(MappingScreen mappingScreen)
    {
        _mappingScreen = mappingScreen;
        InitializeControls();
    }

    private void InitializeControls()
    {
        if (_mappingScreen?.DecalSpinBoxContainer == null) return;

        _rotationSpinBox = new FloatSpinBox(90.0f, 0)
        {
            HorizontalExpand = true
        };
        _mappingScreen.DecalSpinBoxContainer.AddChild(_rotationSpinBox);

        if (_mappingScreen.DecalColorPicker != null)
            _mappingScreen.DecalColorPicker.OnColorChanged += OnDecalColorPicked;

        if (_mappingScreen.DecalPickerOpen != null)
            _mappingScreen.DecalPickerOpen.OnPressed += OnDecalPickerOpenPressed;

        _rotationSpinBox.OnValueChanged += args =>
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

    private void OnDecalColorPicked(Color color)
    {
        _decalColor = color;
        UpdateDecal();
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
        _id = id;
        // Implement decal selection logic
    }

    public void Cleanup()
    {
        if (_mappingScreen?.DecalColorPicker != null)
            _mappingScreen.DecalColorPicker.OnColorChanged -= OnDecalColorPicked;

        if (_mappingScreen?.DecalPickerOpen != null)
            _mappingScreen.DecalPickerOpen.OnPressed -= OnDecalPickerOpenPressed;

        if (_rotationSpinBox != null)
        {
            _rotationSpinBox.Dispose();
            _rotationSpinBox = null;
        }

        _mappingScreen = null;
    }
} 