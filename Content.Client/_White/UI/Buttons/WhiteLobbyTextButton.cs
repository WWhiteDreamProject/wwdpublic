using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._White.UI.Buttons;

[Virtual]
public class WhiteLobbyTextButton : TextureButton
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly Font _font;

    private string? _buttonText;

    public string ButtonText
    {
        set => _buttonText = value;
    }

    public WhiteLobbyTextButton()
    {
        IoCManager.InjectDependencies(this);

        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/_White/Bedstead/Bedstead.otf"), 15);
    }

    private void RebuildTexture()
    {
        if (_buttonText == null)
            return;

        var fontMeasure = MeasureText(_font, _buttonText);

        var blank = new Image<Rgba32>((int)fontMeasure.X, (int)fontMeasure.Y);
        blank[0, 0] = new Rgba32(0, 0, 0, 0);

        var texture = Texture.LoadFromImage(blank);
        TextureNormal = texture;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        switch (DrawMode)
        {
            case DrawModeEnum.Normal:
                DrawNormal(handle);
                break;
            case DrawModeEnum.Pressed:
                DrawPressed(handle);
                break;
            case DrawModeEnum.Hover:
                DrawHover(handle);
                break;
            case DrawModeEnum.Disabled:
                DrawDisabled(handle);
                break;
        }
    }

    private void DrawNormal(DrawingHandleScreen handle)
    {
        if (string.IsNullOrEmpty(_buttonText))
            return;

        var color = Color.White;

        if (ToggleMode)
            color = Color.Red;

        DrawText(handle, color);
    }

    private void DrawHover(DrawingHandleScreen handle)
    {
        if (string.IsNullOrEmpty(_buttonText))
            return;

        DrawText(handle, Color.Yellow);
    }

    private void DrawDisabled(DrawingHandleScreen handle)
    {
        if (string.IsNullOrEmpty(_buttonText))
            return;

        DrawText(handle, Color.Gray);
    }

    private void DrawPressed(DrawingHandleScreen handle)
    {
        if (string.IsNullOrEmpty(_buttonText))
            return;

        var color = Pressed
            ? Color.Green
            : Color.Red;

        DrawText(handle, color);
    }

    private void DrawText(DrawingHandleScreen handle, Color color)
    {
        if (string.IsNullOrEmpty(_buttonText))
            return;

        RebuildTexture();

        handle.DrawString(
            _font,
            Vector2.Zero,
            _buttonText!,
            color
        );
    }

    private Vector2 MeasureText(Font font, string text)
    {
        var textSize = font.GetHeight(0.9f);
        var width = textSize * text.Length / 1.5f;

        return new Vector2(width, textSize);
    }
}
