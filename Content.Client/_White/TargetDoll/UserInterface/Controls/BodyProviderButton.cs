using Content.Shared._White.Body;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._White.TargetDoll.UserInterface.Controls;

/// <summary>
/// A button control that represents a body provider selection with focused and hovered states.
/// </summary>
public sealed class BodyProviderButton : BaseButton
{
    /// <summary>
    /// Type of body provider this button represents.
    /// </summary>
    public BodyProviderType Provider { get; set; } = BodyProviderType.None;

    /// <summary>
    /// Path to the focused state texture resource.
    /// </summary>
    private string? _pathFocused;

    /// <summary>
    /// Path to the hovered state texture resource.
    /// </summary>
    private string? _pathHovered;

    /// <summary>
    /// Texture displayed when the button is focused.
    /// </summary>
    private Texture? _textureFocused;

    /// <summary>
    /// Texture displayed when the button is hovered.
    /// </summary>
    private Texture? _textureHovered;

    /// <inheritdoc cref="_pathFocused"/>
    public string PathFocused
    {
        set
        {
            _pathFocused = value;
            if (_pathFocused != null)
                TextureFocused = Theme.ResolveTexture(_pathFocused);
        }
    }

    /// <inheritdoc cref="_pathHovered"/>
    public string PathHovered
    {
        set
        {
            _pathHovered = value;
            if (_pathHovered != null)
                TextureHovered = Theme.ResolveTexture(_pathHovered);
        }
    }

    /// <inheritdoc cref="_textureFocused"/>
    public Texture? TextureFocused
    {
        get => _textureFocused;
        set
        {
            _textureFocused = value;
            InvalidateMeasure();
        }
    }

    /// <inheritdoc cref="_textureHovered"/>
    public Texture? TextureHovered
    {
        get => _textureHovered;
        set
        {
            _textureHovered = value;
            InvalidateMeasure();
        }
    }

    public BodyProviderButton()
    {
        IoCManager.InjectDependencies(this);
    }
}
