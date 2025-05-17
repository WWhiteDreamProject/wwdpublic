using Content.StyleSheetify.Client.StyleSheet.StyleBox;
using Robust.Client.UserInterface;


namespace Content.Client.Stylesheets;


public sealed class DummyStylesheetManager : IStylesheetManager
{
    public Stylesheet SheetNano { get; } = new Stylesheet([]);
    public Stylesheet SheetSpace { get; } = new Stylesheet([]);

    public DummyStylesheetManager()
    {
        StyleBoxTextureData.IgnoreTextures = true;
    }

    public void Initialize(){}

}
