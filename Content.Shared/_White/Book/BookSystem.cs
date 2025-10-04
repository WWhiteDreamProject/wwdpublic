using Content.Shared._White.Book.Components;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._White.Book;

public sealed class BookSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BookComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<BookComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BookComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<BookComponent, BookAddBookmarkMessage>(OnAddBookmark);
        SubscribeLocalEvent<BookComponent, BookAddPageMessage>(OnAddPage);
        SubscribeLocalEvent<BookComponent, BookAddTextMessage>(OnAddText);
        SubscribeLocalEvent<BookComponent, BookDeletePageMessage>(OnDeletePage);
        SubscribeLocalEvent<BookComponent, BookPageChangedMessage>(OnPageChanged);
        SubscribeLocalEvent<BookComponent, BookRemoveBookmarkMessage>(OnRemoveBookmark);
    }

    private void BeforeUIOpen(EntityUid uid, BookComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, BookComponent component, InteractUsingEvent args)
    {
        if (!_tagSystem.HasTag(args.Used, "Write"))
            return;

        UpdateUserInterface(uid, component, true);
        _uiSystem.OpenUi(uid, BookUiKey.Key, args.User);
        args.Handled = true;
    }

    private void OnMapInit(EntityUid uid, BookComponent component, MapInitEvent args)
    {
        if (string.IsNullOrEmpty(component.Content))
            return;

        SplitContentIntoPages(component, Loc.GetString(component.Content));
        UpdateUserInterface(uid, component);
    }

    private void OnAddBookmark(EntityUid uid, BookComponent component, BookAddBookmarkMessage args)
    {
        if (component.Bookmarks.Count >= component.MaxBookmarks || component.Bookmarks.ContainsKey(args.PageIndex))
            return;

        component.Bookmarks[args.PageIndex] = args.BookmarkName;
        UpdateUserInterface(uid, component);
    }

    private void OnAddPage(EntityUid uid, BookComponent component, BookAddPageMessage args)
    {
        if (component.Pages.Count >= component.MaxPages)
            return;

        component.Pages.Add("");
        component.CurrentPage = component.Pages.Count - 1;
        _audioSystem.PlayPvs(component.PageFlipSound, uid);
        UpdateUserInterface(uid, component);
    }

    private void OnAddText(EntityUid uid, BookComponent component, BookAddTextMessage args)
    {
        if (component.CurrentPage < 0 || component.CurrentPage >= component.Pages.Count)
        {
            component.CurrentPage = Math.Max(0, Math.Min(component.CurrentPage, component.Pages.Count - 1));
            if (component.Pages.Count == 0)
            {
                component.Pages.Add("");
                component.CurrentPage = 0;
            }
        }

        var remainingText = args.Text;
        var currentPageIndex = component.CurrentPage;
        if (remainingText.Length <= component.MaxCharactersPerPage)
        {
            component.Pages[currentPageIndex] = remainingText;
        }
        else
        {
            while (!string.IsNullOrEmpty(remainingText) && component.Pages.Count < component.MaxPages)
            {
                if (remainingText.Length <= component.MaxCharactersPerPage)
                {
                    component.Pages.Insert(currentPageIndex, remainingText);
                    break;
                }

                var pageText = remainingText[..component.MaxCharactersPerPage];
                var lastSpaceIndex = pageText.LastIndexOf(' ');
                if (lastSpaceIndex > component.MaxCharactersPerPage * 0.8)
                {
                    pageText = pageText[..lastSpaceIndex];
                    remainingText = remainingText[(lastSpaceIndex + 1)..];
                }
                else
                {
                    remainingText = remainingText[component.MaxCharactersPerPage..];
                }

                component.Pages.Insert(currentPageIndex, pageText);
                currentPageIndex++;
            }
        }

        component.CurrentPage = Math.Min(currentPageIndex, component.Pages.Count - 1);
        _audioSystem.PlayPvs(component.SaveSound, uid);
        UpdateUserInterface(uid, component);
    }

    private void OnDeletePage(EntityUid uid, BookComponent component, BookDeletePageMessage args)
    {
        if (component.Pages.Count <= 1 || args.PageIndex < 0 || args.PageIndex >= component.Pages.Count)
            return;

        component.Pages.RemoveAt(args.PageIndex);
        component.Bookmarks.Remove(args.PageIndex);
        if (component.Bookmarks.Count > 0)
        {
            var keys = new List<int>(component.Bookmarks.Keys);
            foreach (var key in keys)
            {
                if (key > args.PageIndex)
                {
                    var name = component.Bookmarks[key];
                    component.Bookmarks.Remove(key);
                    if (!component.Bookmarks.ContainsKey(key - 1))
                        component.Bookmarks[key - 1] = name;
                }
            }
        }
        component.CurrentPage = Math.Min(component.CurrentPage, component.Pages.Count - 1);

        _audioSystem.PlayPvs(component.PageTearSound, uid);

        UpdateUserInterface(uid, component);
    }

    private void OnPageChanged(EntityUid uid, BookComponent component, BookPageChangedMessage args)
    {
        component.CurrentPage = Math.Clamp(args.NewPage, 0, Math.Max(0, component.Pages.Count - 1));
        _audioSystem.PlayPvs(component.PageFlipSound, uid);
    }

    private void OnRemoveBookmark(EntityUid uid, BookComponent component, BookRemoveBookmarkMessage args)
    {
        if (!component.Bookmarks.Remove(args.PageIndex))
            return;

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, BookComponent component, bool isEditing = false)
    {
        _uiSystem.SetUiState(
            uid,
            BookUiKey.Key,
            new BookBoundUserInterfaceState(
                new List<string>(component.Pages),
                component.CurrentPage,
                component.MaxCharactersPerPage,
                component.MaxPages,
                new Dictionary<int, string>(component.Bookmarks),
                component.MaxBookmarks,
                isEditing));
    }

    public void SplitContentIntoPages(BookComponent component, string content)
    {
        component.Pages.Clear();

        var manualPages = content.Split(new[] { "[/p]", }, StringSplitOptions.None);
        foreach (var manualPage in manualPages)
        {
            if (component.Pages.Count >= component.MaxPages)
                break;

            var remainingText = manualPage.Trim();
            if (string.IsNullOrEmpty(remainingText))
            {
                component.Pages.Add("");
                continue;
            }

            while (!string.IsNullOrEmpty(remainingText) && component.Pages.Count < component.MaxPages)
            {
                if (remainingText.Length <= component.MaxCharactersPerPage)
                {
                    component.Pages.Add(remainingText);
                    break;
                }

                var pageText = remainingText[..component.MaxCharactersPerPage];
                var lastSpaceIndex = pageText.LastIndexOf(' ');
                if (lastSpaceIndex > component.MaxCharactersPerPage * 0.8)
                {
                    pageText = pageText.Substring(0, lastSpaceIndex);
                    remainingText = remainingText.Substring(lastSpaceIndex + 1);
                }
                else
                {
                    remainingText = remainingText.Substring(component.MaxCharactersPerPage);
                }

                component.Pages.Add(pageText);
            }
        }

        if (component.Pages.Count == 0)
            component.Pages.Add("");
    }
}
