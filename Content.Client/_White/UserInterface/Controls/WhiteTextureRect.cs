using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._White.UserInterface.Controls;

/// <summary>
/// A control that displays a texture with optional scaling support.
/// The texture can be loaded from a theme path or set directly.
/// </summary>
[Virtual]
public class WhiteTextureRect : Control
{
    /// <summary>
    /// Path to the texture resource.
    /// </summary>
    private string? _path;

    /// <summary>
    /// Texture to display.
    /// </summary>
    private Texture? _texture;

    /// <summary>
    /// Scaling factor applied to the texture.
    /// </summary>
    private Vector2 _scale = Vector2.One;

    /// <inheritdoc cref="_path"/>
    public string? Path
    {
        get => _path;
        set
        {
            _path = value;
            if (_path == null)
                return;

            Texture = Theme.ResolveTexture(_path);
        }
    }

    /// <inheritdoc cref="_texture"/>
    public Texture? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            InvalidateMeasure();
        }
    }

    /// <inheritdoc cref="_scale"/>
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            InvalidateMeasure();
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (Texture is null)
            return;

        handle.DrawTextureRect(Texture, UIBox2.FromDimensions(Vector2.Zero, Scale * Texture.Size * UIScale));
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        if (Texture == null)
            return Vector2.Zero;

        return Scale * Texture.Size;
    }

    protected override void OnThemeUpdated()
    {
        base.OnThemeUpdated();

        if (_path == null)
            return;

        Texture = Theme.ResolveTexture(_path);
    }
}
