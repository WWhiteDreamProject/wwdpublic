using System.Linq;
using Content.Shared._White.GameTicking.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._White.UserInterface.AnimatedBackground;

public sealed class AnimatedBackgroundControl : Control
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static readonly ResPath RSIFallback = new("/Textures/_White/LobbyScreens/native.rsi");
    private static readonly string DefaultState = "1";

    private ResPath? _rsiPath;
    private AnimatedTextureRect _animatedTextureRect = new AnimatedTextureRect();

    public ResPath RsiPath => _rsiPath ?? RSIFallback;

    public AnimatedBackgroundControl()
    {
        IoCManager.InjectDependencies(this);
        LayoutContainer.SetAnchorPreset(_animatedTextureRect, LayoutContainer.LayoutPreset.Wide);
        _animatedTextureRect.DisplayRect.Stretch = TextureRect.StretchMode.KeepAspectCovered;
        AddChild(_animatedTextureRect);

        InitializeStates();
    }

    private void InitializeStates()
    {
        var specifier = new SpriteSpecifier.Rsi(RsiPath, DefaultState);
        _animatedTextureRect.SetFromSpriteSpecifier(specifier);
    }

    public void SetRSI(RSI? rsi)
    {
        if(rsi is null)
        {
            _rsiPath = null;
            return;
        }

        _rsiPath = rsi.Path;
        InitializeStates();
    }

    protected override void Resized()
    {
        base.Resized();
        _animatedTextureRect.SetSize = Size;
    }

    public void RandomizeBackground()
    {
        var backgroundsProto = _prototypeManager.EnumeratePrototypes<AnimatedLobbyScreenPrototype>().ToList();
        var random = new Random();
        var index = random.Next(backgroundsProto.Count);
        _rsiPath = new ResPath($"/Textures/{backgroundsProto[index].Path}");
        InitializeStates();
    }
}
