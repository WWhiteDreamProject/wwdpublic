using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Security;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;

namespace Content.Client._White.Glasses.UI;

public sealed class SecurityGlassesRadialMenu : RadialMenu
{
    public event Action<SecurityStatus, string?>? OnStatusSelected;

    private readonly RSIResource _rsi;

    private static SecurityGlassesRadialMenu? _currentOpenMenu;
    
    public SecurityGlassesRadialMenu()
    {
        BackButtonStyleClass = "RadialMenuBackButton";
        CloseButtonStyleClass = "RadialMenuCloseButton";
        MinSize = new Vector2(256, 256);
        MaxSize = MinSize;

        var cache = IoCManager.Resolve<IResourceCache>();
        _rsi = cache.GetResource<RSIResource>(new ResPath("/Textures/Interface/Misc/security_icons.rsi"));

        var container = new RadialContainer
        {
            Name = "StatusContainer",
            Radius = 60f
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
            SetSize = new Vector2(64, 64),
            ToolTip = Loc.GetString($"criminal-records-status-{status.ToString().ToLower()}")
        };

        var texture = new TextureRect
        {
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            Texture = GetStatusTexture(status),
            TextureScale = new Vector2(2, 2)
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

        if (_rsi.RSI.TryGetState(stateName, out var state))
        {
            return state.Frame0;
        }

        foreach (var availableState in _rsi.RSI)
        {
            return availableState.Frame0;
        }

        return Texture.Transparent;
    }

    public void Open(Vector2 screenPos)
    {
        if (_currentOpenMenu != null && _currentOpenMenu != this)
        {
            _currentOpenMenu.Close();
        }
        
        base.Open(screenPos);
        _currentOpenMenu = this;
    }
    
    public new void OpenCentered()
    {
        if (_currentOpenMenu != null && _currentOpenMenu != this)
        {
            _currentOpenMenu.Close();
        }
        
        base.OpenCentered();
        _currentOpenMenu = this;
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