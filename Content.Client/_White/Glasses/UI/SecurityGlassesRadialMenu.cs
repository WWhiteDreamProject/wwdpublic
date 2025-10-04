using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Security;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Client.ResourceManagement;

namespace Content.Client._White.Glasses.UI;

public sealed class SecurityGlassesRadialMenu : RadialMenu
{
    private const float MenuSize = 256f;
    private const float ButtonSize = 64f;
    private const float TextureScale = 2f;

    public event Action<SecurityStatus, string?>? OnStatusSelected;

    private readonly RSIResource _rsi;

    private static SecurityGlassesRadialMenu? _currentOpenMenu;

    public static SecurityGlassesRadialMenu? GetCurrentMenu() => _currentOpenMenu;

    public SecurityGlassesRadialMenu()
    {
        BackButtonStyleClass = "RadialMenuBackButton";
        CloseButtonStyleClass = "RadialMenuCloseButton";
        MinSize = new Vector2(MenuSize, MenuSize);
        MaxSize = MinSize;

        var cache = IoCManager.Resolve<IResourceCache>();
        _rsi = cache.GetResource<RSIResource>(new ResPath("/Textures/Interface/Misc/security_icons.rsi"));

        InitializeStatusButtons();
    }

    private void InitializeStatusButtons()
    {
        var container = new RadialContainer
        {
            Name = "StatusContainer",
        };

        AddChild(container);

        var statuses = new[]
        {
            SecurityStatus.None,
            SecurityStatus.Wanted,
            SecurityStatus.Detained,
            SecurityStatus.Suspected,
            SecurityStatus.Paroled,
            SecurityStatus.Discharged
        };

        foreach (var status in statuses)
        {
            var button = CreateStatusButton(status);
            container.AddChild(button);
        }
    }

    private RadialMenuTextureButton CreateStatusButton(SecurityStatus status)
    {
        var button = new RadialMenuTextureButton
        {
            StyleClasses = { "RadialMenuButton" },
            SetSize = new Vector2(ButtonSize, ButtonSize),
            ToolTip = Loc.GetString($"criminal-records-status-{status.ToString().ToLower()}")
        };

        var texture = new TextureRect
        {
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            Texture = GetStatusTexture(status),
            TextureScale = new Vector2(TextureScale, TextureScale)
        };

        button.OnPressed += _ => OnStatusSelected?.Invoke(status, null);
        button.AddChild(texture);

        return button;
    }

    private Texture GetStatusTexture(SecurityStatus status)
    {
        var stateName = status switch
        {
            SecurityStatus.None => "none",
            SecurityStatus.Wanted => "hud_wanted",
            SecurityStatus.Detained => "hud_incarcerated",
            SecurityStatus.Suspected => "hud_suspected",
            SecurityStatus.Paroled => "hud_paroled",
            SecurityStatus.Discharged => "hud_discharged",
            _ => "none"
        };

        return _rsi.RSI.TryGetState(stateName, out var state)
            ? state.Frame0
            : Texture.Transparent;
    }


    private void EnsureSingletonOpen()
    {
        if (_currentOpenMenu != null && _currentOpenMenu != this)
        {
            _currentOpenMenu.Close();
        }
        _currentOpenMenu = this;
    }

    public void Open(Vector2 screenPos)
    {
        EnsureSingletonOpen();
        base.Open(screenPos);
    }

    public new void OpenCentered()
    {
        EnsureSingletonOpen();
        base.OpenCentered();
    }

    public override void Close()
    {
        base.Close();

        if (_currentOpenMenu == this)
        {
            _currentOpenMenu = null;
        }
    }
}
