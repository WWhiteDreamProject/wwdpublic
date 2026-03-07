using Content.Shared._NC.Crafting.WeaponWorkbench.Events;
using Content.Shared._NC.Crafting.WeaponWorkbench.Components;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Client.Graphics;
using System.Numerics;
using Robust.Shared.Localization;

namespace Content.Client._NC.Crafting.WeaponWorkbench.UI;

public sealed class NCWeaponWorkbenchWindow : DefaultWindow
{
    private readonly BoxContainer _mainContainer;

    // --- Элементы UI ---
    private readonly Label _statusLabel;
    private readonly Label _predictiveLogLabel;
    private readonly ProgressBar _progressBar;

    private readonly WorkbenchSensorControl _heatSensor;
    private readonly WorkbenchSensorControl _integritySensor;
    private readonly WorkbenchSensorControl _alignmentSensor;

    private readonly Button _btnStart;
    private readonly Button _btnCoolant;
    private readonly Button _btnSpotWeld;
    private readonly Button _btnAlignLeft;
    private readonly Button _btnAlignRight;

    private readonly Label _materialLabel;

    // Красное мигание
    private readonly PanelContainer _flashOverlay;

    // Системная блокировка (Тир 3)
    private readonly PanelContainer _lockPanel;
    private readonly Label _lockLabel;
    private readonly Label _lockCodeDisplay;
    private readonly LineEdit _lockInput;
    private readonly Button _lockSubmit;

    // Визуальный кулдаун
    private readonly Label _cooldownLabel;

    public event Action<OperatorCommandType>? OnOperatorCommand;
    public event Action<string>? OnLockCodeSubmit;

    public NCWeaponWorkbenchWindow()
    {
        Title = Loc.GetString("nc-workbench-title") ?? "CNC Weapon Workbench";
        MinSize = SetSize = new Vector2(620, 500);

        _mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(10)
        };

        // --- ЗОНА А: СЛОТЫ ВВОДА ---
        var inputZone = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var inputPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(40, 40, 45) },
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Stretch,
            VerticalAlignment = VAlignment.Stretch,
            Margin = new Thickness(5)
        };
        var inputContent = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(5) };
        inputContent.AddChild(new Label { Text = "INPUT SYSTEM", FontColorOverride = Color.LimeGreen });
        _materialLabel = new Label { Text = "Base Material: NONE", FontColorOverride = Color.Red };
        inputContent.AddChild(_materialLabel);
        inputPanel.AddChild(inputContent);
        inputZone.AddChild(inputPanel);

        _btnStart = new Button { Text = "START CYCLE", MinSize = new Vector2(100, 30), HorizontalAlignment = HAlignment.Right, VerticalAlignment = VAlignment.Center };
        _btnStart.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.StartScraping);
        inputZone.AddChild(new Control { MinSize = new Vector2(20, 0) });
        inputZone.AddChild(_btnStart);

        _mainContainer.AddChild(inputZone);

        // --- ЗОНА Б: СТАТУС И ПРОГРЕСС ---
        _statusLabel = new Label { Text = "STATUS: IDLE", HorizontalAlignment = HAlignment.Center, FontColorOverride = Color.Cyan };
        _mainContainer.AddChild(_statusLabel);

        _predictiveLogLabel = new Label { Text = "[SYSTEM LOG: STANDBY]", HorizontalAlignment = HAlignment.Center, FontColorOverride = Color.Orange };
        _mainContainer.AddChild(_predictiveLogLabel);

        _progressBar = new ProgressBar
        {
            MinSize = new Vector2(0, 25),
            MinValue = 0f,
            MaxValue = 1.0f,
            HorizontalExpand = true,
            Margin = new Thickness(0, 5, 0, 10)
        };
        _mainContainer.AddChild(_progressBar);

        // --- ЗОНА В: ДАТЧИКИ ---
        var sensorsZone = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(0, 0, 0, 10)
        };

        _heatSensor = CreateSensorPanel("HEAT", sensorsZone, Color.OrangeRed);
        _integritySensor = CreateSensorPanel("INTEGRITY", sensorsZone, Color.DodgerBlue);
        _alignmentSensor = CreateSensorPanel("ALIGNMENT", sensorsZone, Color.Purple);

        _mainContainer.AddChild(sensorsZone);

        // --- ЗОНА Г: ПАНЕЛЬ КОМАНД ---
        var commandsZone = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center
        };

        commandsZone.AddChild(_btnCoolant = CreateOperatorButton("COOLANT"));
        _btnCoolant.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.ApplyCoolant);

        commandsZone.AddChild(_btnSpotWeld = CreateOperatorButton("SPOT WELD"));
        _btnSpotWeld.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.SpotWeld);

        commandsZone.AddChild(_btnAlignLeft = CreateOperatorButton("ALIGN ←"));
        _btnAlignLeft.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.AlignLeft);

        commandsZone.AddChild(_btnAlignRight = CreateOperatorButton("ALIGN →"));
        _btnAlignRight.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.AlignRight);

        _mainContainer.AddChild(commandsZone);

        // Индикатор кулдауна кнопок
        _cooldownLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HAlignment.Center,
            FontColorOverride = Color.Gray,
            Margin = new Thickness(0, 2, 0, 0)
        };
        _mainContainer.AddChild(_cooldownLabel);

        // --- Создаём корневой Layout с оверлеями ---
        var rootLayout = new LayoutContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true
        };

        // Основной контент
        LayoutContainer.SetAnchorPreset(_mainContainer, LayoutContainer.LayoutPreset.Wide);
        rootLayout.AddChild(_mainContainer);

        // Красное мигание (оверлей поверх всего)
        _flashOverlay = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(255, 0, 0, 60) },
            Visible = false,
            MouseFilter = MouseFilterMode.Ignore // Не перехватывает клики
        };
        LayoutContainer.SetAnchorPreset(_flashOverlay, LayoutContainer.LayoutPreset.Wide);
        rootLayout.AddChild(_flashOverlay);

        // Панель системной блокировки (Тир 3)
        _lockPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(0, 0, 0, 200) },
            Visible = false
        };
        LayoutContainer.SetAnchorPreset(_lockPanel, LayoutContainer.LayoutPreset.Wide);

        var lockContent = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(50)
        };
        _lockLabel = new Label { Text = "⚠ SYSTEM LOCK", FontColorOverride = Color.Red, HorizontalAlignment = HAlignment.Center };
        _lockCodeDisplay = new Label { Text = "ENTER ACCESS CODE:", FontColorOverride = Color.Yellow, HorizontalAlignment = HAlignment.Center };
        _lockInput = new LineEdit
        {
            PlaceHolder = "0000",
            MinSize = new Vector2(120, 30),
            HorizontalAlignment = HAlignment.Center
        };
        _lockSubmit = new Button { Text = "SUBMIT", MinSize = new Vector2(120, 35), HorizontalAlignment = HAlignment.Center };
        _lockSubmit.OnPressed += _ =>
        {
            OnLockCodeSubmit?.Invoke(_lockInput.Text);
            _lockInput.Text = string.Empty;
        };
        // Enter key submission
        _lockInput.OnTextEntered += _ =>
        {
            OnLockCodeSubmit?.Invoke(_lockInput.Text);
            _lockInput.Text = string.Empty;
        };

        lockContent.AddChild(_lockLabel);
        lockContent.AddChild(new Control { MinSize = new Vector2(0, 10) });
        lockContent.AddChild(_lockCodeDisplay);
        lockContent.AddChild(new Control { MinSize = new Vector2(0, 5) });
        lockContent.AddChild(_lockInput);
        lockContent.AddChild(new Control { MinSize = new Vector2(0, 5) });
        lockContent.AddChild(_lockSubmit);
        _lockPanel.AddChild(lockContent);
        rootLayout.AddChild(_lockPanel);

        Contents.AddChild(rootLayout);

        DisableOperatorButtons();
    }

    private Button CreateOperatorButton(string text)
    {
        return new Button
        {
            Text = text,
            MinSize = new Vector2(130, 40),
            Margin = new Thickness(3)
        };
    }

    private WorkbenchSensorControl CreateSensorPanel(string labelText, BoxContainer parent, Color themeColor)
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(30, 30, 30) },
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = HAlignment.Stretch,
            VerticalAlignment = VAlignment.Stretch,
            Margin = new Thickness(5)
        };

        var vbox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(5) };
        vbox.AddChild(new Label { Text = labelText, HorizontalAlignment = HAlignment.Center, FontColorOverride = themeColor });

        var sensor = new WorkbenchSensorControl
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MinSize = new Vector2(0, 30)
        };
        vbox.AddChild(sensor);

        panel.AddChild(vbox);
        parent.AddChild(panel);

        return sensor;
    }

    public void UpdateState(NCWeaponWorkbenchUpdateState state)
    {
        // Материал
        _materialLabel.Text = state.HasMaterial ? "Base Material: LOADED" : "Base Material: NONE";
        _materialLabel.FontColorOverride = state.HasMaterial ? Color.LimeGreen : Color.Red;

        // Статус
        _statusLabel.Text = $"STATUS: {state.WorkbenchState.ToString().ToUpper()}";
        _btnStart.Disabled = state.WorkbenchState != NCWeaponWorkbenchState.Idle || !state.HasMaterial;

        // Лог
        _predictiveLogLabel.Text = string.IsNullOrEmpty(state.WarningMessage)
            ? "[SYSTEM LOG: STANDBY]"
            : state.WarningMessage;

        // Прогресс
        _progressBar.Value = state.Progress;

        // Датчики
        _heatSensor.UpdateSensor(state.Heat, state.SafeZoneHalfWidth);
        _integritySensor.UpdateSensor(state.Integrity, state.SafeZoneHalfWidth);
        _alignmentSensor.UpdateSensor(state.Alignment, state.SafeZoneHalfWidth);

        // Кнопки оператора
        if (state.WorkbenchState == NCWeaponWorkbenchState.Processing && !state.IsSystemLocked)
            EnableOperatorButtons();
        else
            DisableOperatorButtons();

        // Визуальный кулдаун
        if (state.ButtonCooldownRemaining > 0f)
        {
            _cooldownLabel.Text = $"COOLDOWN: {state.ButtonCooldownRemaining:F1}s";
            _cooldownLabel.FontColorOverride = Color.OrangeRed;
        }
        else
        {
            _cooldownLabel.Text = "";
        }

        // Красное мигание экрана
        _flashOverlay.Visible = state.IsFlashing;

        // Системная блокировка
        _lockPanel.Visible = state.IsSystemLocked;
    }

    private void EnableOperatorButtons()
    {
        _btnCoolant.Disabled = false;
        _btnSpotWeld.Disabled = false;
        _btnAlignLeft.Disabled = false;
        _btnAlignRight.Disabled = false;
    }

    private void DisableOperatorButtons()
    {
        _btnCoolant.Disabled = true;
        _btnSpotWeld.Disabled = true;
        _btnAlignLeft.Disabled = true;
        _btnAlignRight.Disabled = true;
    }
}

/// <summary>
/// Кастомный контрол датчика: Зелёная/Жёлтая/Красная зоны + ползунок.
/// </summary>
public sealed class WorkbenchSensorControl : Control
{
    private float _currentValue = 0.5f;
    private float _safeHalfWidth = 0.15f;

    public void UpdateSensor(float value, float safeHalfWidth)
    {
        _currentValue = Math.Clamp(value, 0f, 1f);
        _safeHalfWidth = Math.Clamp(safeHalfWidth, 0.05f, 0.5f);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var rect = PixelSizeBox;

        // Фон
        handle.DrawRect(rect, new Color(40, 40, 40));

        // Зоны
        float center = rect.Width / 2f;
        float safePixelWidth = rect.Width * _safeHalfWidth;

        // Красные зоны (по краям)
        var redLeft = new UIBox2i(rect.Left, rect.Top, rect.Left + (int) (rect.Width * 0.1f), rect.Bottom);
        var redRight = new UIBox2i(rect.Right - (int) (rect.Width * 0.1f), rect.Top, rect.Right, rect.Bottom);

        // Зелёная зона (в центре)
        var greenZone = new UIBox2i(
            (int) (center - safePixelWidth), rect.Top,
            (int) (center + safePixelWidth), rect.Bottom
        );

        handle.DrawRect(redLeft, new Color(150, 0, 0, 100));
        handle.DrawRect(redRight, new Color(150, 0, 0, 100));
        handle.DrawRect(greenZone, new Color(0, 150, 0, 80));

        // Ползунок
        int indicatorX = (int) (rect.Left + rect.Width * _currentValue);
        var indicatorBox = new UIBox2i(indicatorX - 3, rect.Top, indicatorX + 3, rect.Bottom);

        Color indicatorColor = Color.Yellow;
        if (_currentValue < 0.1f || _currentValue > 0.9f)
            indicatorColor = Color.Red;
        else if (Math.Abs(_currentValue - 0.5f) <= _safeHalfWidth)
            indicatorColor = Color.LimeGreen;

        handle.DrawRect(indicatorBox, indicatorColor);
    }
}
