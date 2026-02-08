using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IContentStyleSheetManager _contentStyleSheetManager = default!; // WWDP EDIT

        public StylesheetReference SheetNano { get; private set; } = default!; // WWDP EDIT
        public StylesheetReference SheetSpace { get; private set; } = default!; // WWDP EDIT

        public void Initialize()
        {
            SheetNano = _contentStyleSheetManager.MergeStyles(new StyleNano(_resourceCache).Stylesheet, "nano"); // WWDP EDIT
            SheetSpace = _contentStyleSheetManager.MergeStyles(new StyleSpace(_resourceCache).Stylesheet, "space"); // WWDP EDIT

            _userInterfaceManager.Stylesheet = SheetNano;
        }
    }
}
