using Content.StyleSheetify.Client.StyleSheet;
using Content.StyleSheetify.Client.StyleSheet.StyleBox;

namespace Content.Client.Stylesheets;

// WWDP CLASS
public sealed class DummyStylesheetManager : IStylesheetManager
{
    public StylesheetReference SheetNano { get; } = StylesheetReference.Empty;
    public StylesheetReference SheetSpace { get; } = StylesheetReference.Empty;

    public DummyStylesheetManager()
    {
        StyleBoxTextureData.IgnoreTextures = true;
    }

    public void Initialize(){}
}
