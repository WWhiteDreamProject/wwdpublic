using Content.Shared._White.TargetDoll;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._White.UserInterface.Systems.TargetDoll.Controls;

public sealed class BodyPartButton : TextureButton
{
    [ViewVariables]
    public BodyPart BodyPart { get; set; } = BodyPart.None;

    private string? _texturePathFocused;
    public string TexturePathFocused
    {
        set
        {
            _texturePathFocused = value;
            if (_texturePathFocused != null)
                TextureFocused = Theme.ResolveTexture(_texturePathFocused);
        }
    }

    private string? _texturePathHovered;
    public string TexturePathHovered
    {
        set
        {
            _texturePathHovered = value;
            if (_texturePathHovered != null)
                TextureHovered = Theme.ResolveTexture(_texturePathHovered);
        }
    }

    private Texture? _textureFocused;
    public Texture? TextureFocused
    {
        get => _textureFocused;
        set
        {
            _textureFocused = value;
            InvalidateMeasure();
        }
    }

    private Texture? _textureHovered;
    public Texture? TextureHovered
    {
        get => _textureHovered;
        set
        {
            _textureHovered = value;
            InvalidateMeasure();
        }
    }

    public BodyPartButton()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void OnThemeUpdated()
    {
        base.OnThemeUpdated();

        if (_texturePathFocused != null)
            TextureFocused = Theme.ResolveTexture(_texturePathFocused);

        if (_texturePathHovered != null)
            TextureHovered = Theme.ResolveTexture(_texturePathHovered);
    }
}
