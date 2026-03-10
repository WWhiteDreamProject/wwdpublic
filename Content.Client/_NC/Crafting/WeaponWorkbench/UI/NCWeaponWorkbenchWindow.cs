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

/// <summary>
/// Окно верстака оружейника.
/// </summary>
public sealed class NCWeaponWorkbenchWindow : DefaultWindow
{
    private readonly WorkbenchSensorControl _heatSensor;
    private readonly WorkbenchSensorControl _integritySensor;
    private readonly WorkbenchSensorControl _alignmentSensor;

    // Ссылки на UI элементы
    private readonly Label _predictiveLogLabel;
    private readonly Label _materialLabel;
    private readonly TextureRect _materialSprite;
    private readonly EntityPrototypeView _materialSpriteView;
    private readonly Label _resultLabel;
    private readonly TextureRect _resultSprite;
    private readonly EntityPrototypeView _resultSpriteView;

    private readonly Label _heatStatusLabel;
    private readonly Label _heatPercentLabel;
    private readonly Label _alignmentStatusLabel;
    private readonly Label _alignmentPercentLabel;
    private readonly Label _integrityStatusLabel;
    private readonly Label _integrityPercentLabel;

    private readonly Button _btnStart;
    private readonly Button _btnCoolant;
    private readonly Button _btnSpotWeld;
    private readonly Button _btnAlignLeft;
    private readonly Button _btnAlignRight;

    private readonly Label _globalProgressLabel;
    private readonly PanelContainer _flashOverlay;
    private readonly PanelContainer _lockPanel;
    private readonly LineEdit _lockInput;
    private readonly Button _lockSubmit;

    public event Action<OperatorCommandType>? OnOperatorCommand;
    public event Action<string>? OnLockCodeSubmit;

    public NCWeaponWorkbenchWindow()
    {
        Title = Loc.GetString("nc-workbench-window-title");
        MinSize = new Vector2(480, 580);
        SetSize = new Vector2(480, 580);

        var rootPanel = new PanelContainer();
        // Background CRT Screen
        rootPanel.AddChild(new PanelContainer { PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#1a2b20") } });

        var mainLayout = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10)
        };
        rootPanel.AddChild(mainLayout);

        // TOP: Predictive Log Panel
        var logPanel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 0, 10),
            MinSize = new Vector2(0, 80),
            PanelOverride = new StyleBoxFlat() { BackgroundColor = Color.FromHex("#221100"), BorderThickness = new Thickness(2), BorderColor = Color.FromHex("#ff6f00") }
        };
        mainLayout.AddChild(logPanel);

        var logBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(8) };
        logPanel.AddChild(logBox);
        logBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-log-header"), FontColorOverride = Color.FromHex("#b87a32") });
        _predictiveLogLabel = new Label { Text = Loc.GetString("nc-workbench-window-log-standby"), FontColorOverride = Color.FromHex("#ff9a00") };
        logBox.AddChild(_predictiveLogLabel);

        // MIDDLE: Left Material Panel + Right Sensors
        var middleBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, VerticalExpand = true };
        mainLayout.AddChild(middleBox);

        // LEFT: Material Info
        var leftPanel = new PanelContainer
        {
            MinSize = new Vector2(140, 0), Margin = new Thickness(0, 0, 10, 0),
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111c15"), BorderThickness = new Thickness(2), BorderColor = Color.FromHex("#4caf50") }
        };
        middleBox.AddChild(leftPanel);

        var leftContentBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalAlignment = HAlignment.Center, VerticalAlignment = VAlignment.Center };
        leftPanel.AddChild(leftContentBox);

        _materialLabel = new Label { HorizontalAlignment = HAlignment.Center, FontColorOverride = Color.FromHex("#4caf50") };
        leftContentBox.AddChild(_materialLabel);
        _materialSprite = new TextureRect { MinSize = new Vector2(64, 64), HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        leftContentBox.AddChild(_materialSprite);
        leftContentBox.AddChild(new Control { MinSize = new Vector2(0, 20) });
        leftContentBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-recipe-header"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        _resultLabel = new Label { HorizontalAlignment = HAlignment.Center, FontColorOverride = Color.FromHex("#4caf50") };
        leftContentBox.AddChild(_resultLabel);
        _resultSprite = new TextureRect { MinSize = new Vector2(64, 64), HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        leftContentBox.AddChild(_resultSprite);

        // RIGHT: Sensors
        var rightPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111c15"), BorderThickness = new Thickness(2), BorderColor = Color.FromHex("#2e5c3e") }
        };
        middleBox.AddChild(rightPanel);

        var rightLayout = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, VerticalExpand = true, Margin = new Thickness(10) };
        rightPanel.AddChild(rightLayout);

        // TOP ROW: Heat + Integrity
        var topRow = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true, VerticalExpand = true };
        rightLayout.AddChild(topRow);

        // Heat Sensor Container
        var heatBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, HorizontalAlignment = HAlignment.Center };
        topRow.AddChild(heatBox);
        heatBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-sensor-heat"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        _heatStatusLabel = new Label { HorizontalAlignment = HAlignment.Center, FontColorOverride = Color.FromHex("#4caf50") };
        heatBox.AddChild(_heatStatusLabel);

        var heatSensorContainer = new Control { VerticalExpand = true, MinSize = new Vector2(30, 0), HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
        heatBox.AddChild(heatSensorContainer);
        _heatPercentLabel = new Label { Text = "0%", FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center };
        heatBox.AddChild(_heatPercentLabel);

        // Integrity Sensor Container
        var integrityBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, HorizontalAlignment = HAlignment.Center };
        topRow.AddChild(integrityBox);
        integrityBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-sensor-integrity"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        _integrityStatusLabel = new Label { HorizontalAlignment = HAlignment.Center, FontColorOverride = Color.FromHex("#4caf50") };
        integrityBox.AddChild(_integrityStatusLabel);

        var integritySensorContainer = new Control { VerticalExpand = true, MinSize = new Vector2(30, 0), HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
        integrityBox.AddChild(integritySensorContainer);
        _integrityPercentLabel = new Label { Text = "100%", FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center };
        integrityBox.AddChild(_integrityPercentLabel);

        // BOTTOM ROW: Alignment Sensor
        var alignBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true, Margin = new Thickness(0, 10, 0, 0) };
        rightLayout.AddChild(alignBox);

        var alignHeaderBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalAlignment = HAlignment.Center };
        alignBox.AddChild(alignHeaderBox);
        alignHeaderBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-sensor-alignment"), FontColorOverride = Color.FromHex("#4caf50"), Margin = new Thickness(0, 0, 10, 0) });
        _alignmentStatusLabel = new Label { FontColorOverride = Color.FromHex("#4caf50") };
        alignHeaderBox.AddChild(_alignmentStatusLabel);

        var alignSensorContainer = new Control { HorizontalExpand = true, MinSize = new Vector2(0, 30), Margin = new Thickness(0, 5, 0, 5) };
        alignBox.AddChild(alignSensorContainer);
        _alignmentPercentLabel = new Label { Text = "50%", FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center };
        alignBox.AddChild(_alignmentPercentLabel);

        // START BUTTON
        _btnStart = new Button
        {
            MinSize = new Vector2(0, 40),
            Margin = new Thickness(0, 0, 0, 10),
        };
        _btnStart.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-start-button"), FontColorOverride = Color.LimeGreen, HorizontalAlignment = HAlignment.Center, VerticalAlignment = VAlignment.Center });
        mainLayout.AddChild(_btnStart);

        // BOTTOM: Operator Controls
        var opPanel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 0, 0), MinSize = new Vector2(0, 80),
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111c15"), BorderThickness = new Thickness(2), BorderColor = Color.FromHex("#2e5c3e") }
        };
        mainLayout.AddChild(opPanel);

        var opBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(5), HorizontalExpand = true, VerticalExpand = true, HorizontalAlignment = HAlignment.Center };
        opPanel.AddChild(opBox);
        opBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-operator-header"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });

        var btnsBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 5, 0, 0) };
        opBox.AddChild(btnsBox);

        var coolantBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
        _btnCoolant = new Button { MinSize = new Vector2(48, 48) };
        coolantBox.AddChild(_btnCoolant);
        coolantBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-op-coolant"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        btnsBox.AddChild(coolantBox);

        var weldBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
        _btnSpotWeld = new Button { MinSize = new Vector2(48, 48) };
        weldBox.AddChild(_btnSpotWeld);
        weldBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-op-weld"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        btnsBox.AddChild(weldBox);

        var alignLeftBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
        _btnAlignLeft = new Button { MinSize = new Vector2(48, 48) };
        alignLeftBox.AddChild(_btnAlignLeft);
        alignLeftBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-op-align-left"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        btnsBox.AddChild(alignLeftBox);

        var alignRightBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
        _btnAlignRight = new Button { MinSize = new Vector2(48, 48) };
        alignRightBox.AddChild(_btnAlignRight);
        alignRightBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-op-align-right"), FontColorOverride = Color.FromHex("#4caf50"), HorizontalAlignment = HAlignment.Center });
        btnsBox.AddChild(alignRightBox);

        // VERY BOTTOM: Global Progress
        var progressPanel = new PanelContainer
        {
            Margin = new Thickness(0, 5, 0, 0), MinSize = new Vector2(0, 30),
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111c15"), BorderThickness = new Thickness(2), BorderColor = Color.FromHex("#4caf50") }
        };
        mainLayout.AddChild(progressPanel);
        _globalProgressLabel = new Label { Text = Loc.GetString("nc-workbench-window-progress-waiting"), FontColorOverride = Color.FromHex("#4caf50"), Margin = new Thickness(5) };
        progressPanel.AddChild(_globalProgressLabel);

        // OVERLAYS
        _flashOverlay = new PanelContainer
        {
            Visible = false,
            MouseFilter = MouseFilterMode.Ignore,
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#66ff0000") }
        };
        rootPanel.AddChild(_flashOverlay);

        _lockPanel = new PanelContainer
        {
            Visible = false,
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#cc000000") }
        };
        rootPanel.AddChild(_lockPanel);

        var lockBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalAlignment = HAlignment.Center, VerticalAlignment = VAlignment.Center };
        _lockPanel.AddChild(lockBox);
        lockBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-lock-header"), FontColorOverride = Color.Red, HorizontalAlignment = HAlignment.Center });
        lockBox.AddChild(new Label { Text = Loc.GetString("nc-workbench-window-lock-input-label"), FontColorOverride = Color.Yellow, HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 10, 0, 5) });
        _lockInput = new LineEdit { PlaceHolder = "0000", MinSize = new Vector2(120, 30), HorizontalAlignment = HAlignment.Center };
        lockBox.AddChild(_lockInput);
        _lockSubmit = new Button { Text = Loc.GetString("nc-workbench-window-lock-submit"), MinSize = new Vector2(120, 35), HorizontalAlignment = HAlignment.Center, Margin = new Thickness(0, 5, 0, 0) };
        lockBox.AddChild(_lockSubmit);

        Contents.AddChild(rootPanel);

        // In-line init
        _heatSensor = new WorkbenchSensorControl(Color.Red, Color.Yellow, Color.LimeGreen);
        heatSensorContainer.AddChild(_heatSensor);

        _alignmentSensor = new WorkbenchSensorControl(Color.Red, Color.Yellow, Color.LimeGreen, true);
        alignSensorContainer.AddChild(_alignmentSensor);

        _integritySensor = new WorkbenchSensorControl(Color.Brown, Color.Yellow, Color.LimeGreen);
        integritySensorContainer.AddChild(_integritySensor);

        _materialSpriteView = new EntityPrototypeView { Scale = new Vector2(2, 2) };
        _materialSprite.AddChild(_materialSpriteView);

        _resultSpriteView = new EntityPrototypeView { Scale = new Vector2(2, 2) };
        _resultSprite.AddChild(_resultSpriteView);

        _btnStart.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.StartScraping);
        _btnCoolant.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.ApplyCoolant);
        _btnSpotWeld.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.SpotWeld);
        _btnAlignLeft.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.AlignLeft);
        _btnAlignRight.OnPressed += _ => OnOperatorCommand?.Invoke(OperatorCommandType.AlignRight);

        _btnCoolant.AddChild(new TextureRect { TexturePath = "/Textures/Interface/VerbIcons/snow.svg.192dpi.png", HorizontalExpand = true, VerticalExpand = true, Stretch = TextureRect.StretchMode.KeepAspectCentered });
        _btnSpotWeld.AddChild(new TextureRect { TexturePath = "/Textures/Interface/VerbIcons/zap.svg.192dpi.png", HorizontalExpand = true, VerticalExpand = true, Stretch = TextureRect.StretchMode.KeepAspectCentered });
        _btnAlignLeft.AddChild(new TextureRect { TexturePath = "/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png", HorizontalExpand = true, VerticalExpand = true, Stretch = TextureRect.StretchMode.KeepAspectCentered });
        _btnAlignRight.AddChild(new TextureRect { TexturePath = "/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png", HorizontalExpand = true, VerticalExpand = true, Stretch = TextureRect.StretchMode.KeepAspectCentered });

        _lockSubmit.OnPressed += _ => OnLockCodeSubmit?.Invoke(_lockInput.Text);

        UpdateState(new NCWeaponWorkbenchUpdateState(NCWeaponWorkbenchState.Idle, 0.5f, 0.5f, 0.5f, 0f, 0.15f, string.Empty, false, null, null, false, 0f, false, null));
    }

    public void UpdateState(NCWeaponWorkbenchUpdateState state)
    {
        // 1. Предсказания и логи
        _predictiveLogLabel.Text = "> " + (string.IsNullOrEmpty(state.WarningMessage)
            ? Loc.GetString("nc-workbench-window-log-standby")
            : state.WarningMessage);

        // 2. Детали и иконки
        var materialStatus = state.HasMaterial ? Loc.GetString("nc-workbench-window-material-status-ok") : Loc.GetString("nc-workbench-window-material-status-none");
        _materialLabel.Text = Loc.GetString("nc-workbench-window-material-label", ("status", materialStatus));
        _materialSpriteView.SetPrototype(state.SourcePrototypeId);

        var recipeStatus = state.ResultPrototypeId != null ? Loc.GetString("nc-workbench-window-recipe-status-ok") : Loc.GetString("nc-workbench-window-recipe-status-none");
        _resultLabel.Text = Loc.GetString("nc-workbench-window-recipe-label", ("status", recipeStatus));
        _resultSpriteView.SetPrototype(state.ResultPrototypeId);

        // 3. Сенсоры
        var heatCond = GetCondition(state.Heat, state.SafeZoneHalfWidth);
        var alignCond = GetCondition(state.Alignment, state.SafeZoneHalfWidth);
        var integrCond = GetCondition(state.Integrity, state.SafeZoneHalfWidth);

        UpdateSensorStatusLabel(_heatStatusLabel, heatCond);
        _heatSensor.TargetValue = Math.Clamp(state.Heat, 0f, 1f);
        _heatPercentLabel.Text = $"{(int) (state.Heat * 100)}%";

        UpdateSensorStatusLabel(_alignmentStatusLabel, alignCond);
        _alignmentSensor.TargetValue = Math.Clamp(state.Alignment, 0f, 1f);
        _alignmentPercentLabel.Text = $"{(int) (state.Alignment * 100)}%";

        UpdateSensorStatusLabel(_integrityStatusLabel, integrCond);
        _integritySensor.TargetValue = Math.Clamp(state.Integrity, 0f, 1f);
        _integrityPercentLabel.Text = $"{(int) (state.Integrity * 100)}%";

        // 4. Глобальный прогресс
        if (state.Progress > 0)
            _globalProgressLabel.Text = Loc.GetString("nc-workbench-window-progress-label", ("value", (int) (state.Progress * 100)));
        else
            _globalProgressLabel.Text = Loc.GetString("nc-workbench-window-progress-waiting");

        // 5. Блокировка системы (Уровень 3)
        if (state.IsSystemLocked)
        {
            _lockPanel.Visible = true;
            DisableOperatorButtons();
        }
        else
        {
            _lockPanel.Visible = false;
            EnableOperatorButtons();
        }

        // Вспышка ошибки
        _flashOverlay.Visible = state.IsFlashing;
    }

    private enum SensorCondition { Optimal, Warning, Critical }

    private static SensorCondition GetCondition(float value, float safeZoneHalf)
    {
        float dist = Math.Abs(value - 0.5f);
        if (dist <= safeZoneHalf) return SensorCondition.Optimal;
        if (dist <= safeZoneHalf + 0.15f) return SensorCondition.Warning;
        return SensorCondition.Critical;
    }

    private static void UpdateSensorStatusLabel(Label label, SensorCondition status)
    {
        var statusKey = status switch
        {
            SensorCondition.Optimal => "nc-workbench-window-sensor-status-optimal",
            SensorCondition.Warning => "nc-workbench-window-sensor-status-warning",
            SensorCondition.Critical => "nc-workbench-window-sensor-status-critical",
            _ => "nc-workbench-window-sensor-status-optimal"
        };

        label.Text = Loc.GetString("nc-workbench-window-sensor-status", ("status", Loc.GetString(statusKey)));
        label.FontColorOverride = status switch
        {
            SensorCondition.Optimal => Color.LimeGreen,
            SensorCondition.Warning => Color.Yellow,
            SensorCondition.Critical => Color.Red,
            _ => Color.LimeGreen
        };
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
/// Кастомный контрол рисующий вертикальную шкалу с ползунком-стрелочкой
/// </summary>
public sealed class WorkbenchSensorControl : Control
{
    private float _currentValue = 0f;
    public float TargetValue { get; set; } = 0f;

    private readonly Color _dangerColor;
    private readonly Color _warningColor;
    private readonly Color _safeColor;
    private readonly bool _isHorizontal;

    public WorkbenchSensorControl(Color danger, Color warning, Color safe, bool isHorizontal = false)
    {
        _isHorizontal = isHorizontal;
        if (_isHorizontal)
        {
            MinSize = new Vector2(100, 30);
            HorizontalExpand = true;
        }
        else
        {
            MinSize = new Vector2(30, 100);
            VerticalExpand = true;
        }
        _dangerColor = danger;
        _warningColor = warning;
        _safeColor = safe;
    }

    protected override void FrameUpdate(Robust.Shared.Timing.FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // Плавная анимация сенсора
        if (MathHelper.CloseToPercent(_currentValue, TargetValue, 0.01f))
            return;

        float speed = 2.0f;
        _currentValue = MathHelper.Lerp(_currentValue, TargetValue, speed * args.DeltaSeconds);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var rect = PixelSizeBox;

        if (!_isHorizontal)
        {
            // Рисуем рамку и зоны
            // Опасная зона (верхние 20%, 0.8-1.0)
            var dangerBox = new UIBox2i(rect.Left, rect.Top, rect.Right - 10, rect.Top + (int) (rect.Height * 0.2f));
            handle.DrawRect(dangerBox, _dangerColor);

            // Зона предупреждения (0.6-0.8)
            var warnBox = new UIBox2i(rect.Left, dangerBox.Bottom, rect.Right - 10, dangerBox.Bottom + (int) (rect.Height * 0.2f));
            handle.DrawRect(warnBox, _warningColor);

            // Безопасная зона (низ 0.0-0.6)
            var safeBox = new UIBox2i(rect.Left, warnBox.Bottom, rect.Right - 10, rect.Bottom);
            handle.DrawRect(safeBox, _safeColor);

            handle.DrawRect(new UIBox2i(rect.Left, rect.Top, rect.Right - 10, rect.Bottom), Color.White, false);

            // Индикатор-Стрелочка
            int indicatorY = (int) (rect.Bottom - rect.Height * _currentValue);

            var indicatorBox = new UIBox2i(rect.Left, indicatorY - 3, rect.Right, indicatorY + 3);
            handle.DrawRect(indicatorBox, Color.White);

            var pt1 = new Vector2(rect.Right - 8, indicatorY);
            var pt2 = new Vector2(rect.Right, indicatorY - 6);
            var pt3 = new Vector2(rect.Right, indicatorY + 6);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new[] { pt1, pt2, pt3 }, Color.Red);
        }
        else
        {
            // Для выравнивания: Опасные зоны по краям, зеленая по центру
            int hm = (int) (rect.Bottom - 10);
            int safeStart = (int) (rect.Width * 0.35f);
            int safeEnd = (int) (rect.Width * 0.65f);

            var dangerLeftBox = new UIBox2i(rect.Left, rect.Top, rect.Left + (int) (rect.Width * 0.2f), hm);
            handle.DrawRect(dangerLeftBox, _dangerColor);

            var warnLeftBox = new UIBox2i(dangerLeftBox.Right, rect.Top, rect.Left + safeStart, hm);
            handle.DrawRect(warnLeftBox, _warningColor);

            var safeBox = new UIBox2i(warnLeftBox.Right, rect.Top, rect.Left + safeEnd, hm);
            handle.DrawRect(safeBox, _safeColor);

            var warnRightBox = new UIBox2i(safeBox.Right, rect.Top, rect.Left + (int) (rect.Width * 0.8f), hm);
            handle.DrawRect(warnRightBox, _warningColor);

            var dangerRightBox = new UIBox2i(warnRightBox.Right, rect.Top, rect.Right, hm);
            handle.DrawRect(dangerRightBox, _dangerColor);

            handle.DrawRect(new UIBox2i(rect.Left, rect.Top, rect.Right, hm), Color.White, false);

            int indicatorX = (int) (rect.Left + rect.Width * _currentValue);
            var indicatorBox = new UIBox2i(indicatorX - 3, rect.Top, indicatorX + 3, hm);
            handle.DrawRect(indicatorBox, Color.White);

            int arrowTop = hm + 8;
            var pt1 = new Vector2(indicatorX, arrowTop);
            var pt2 = new Vector2(indicatorX - 6, hm);
            var pt3 = new Vector2(indicatorX + 6, hm);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new[] { pt1, pt2, pt3 }, Color.Red);
        }
    }
}
