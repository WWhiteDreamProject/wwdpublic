using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets
{
    public interface IStylesheetManager
    {
        StylesheetReference SheetNano { get; } // WWDP EDIT
        StylesheetReference SheetSpace { get; } // WWDP EDIT

        void Initialize();
    }
}
