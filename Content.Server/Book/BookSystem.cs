using Content.Shared.Book.Components;
using Content.Shared.Book;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Server.Book;

public sealed class BookSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BookComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<BookComponent, BookPageChangedMessage>(OnPageChanged);
        SubscribeLocalEvent<BookComponent, BookAddTextMessage>(OnAddText);
        SubscribeLocalEvent<BookComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BookComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<BookComponent, IntrinsicUIOpenAttemptEvent>(OnIntrinsicUIOpenAttempt);
        SubscribeLocalEvent<BookComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<BookComponent, BoundUserInterfaceCheckRangeEvent>(OnBookUiRangeCheck);
        SubscribeLocalEvent<BookComponent, BookAddBookmarkMessage>(OnAddBookmark);
        SubscribeLocalEvent<BookComponent, BookRemoveBookmarkMessage>(OnRemoveBookmark);
        SubscribeLocalEvent<BookComponent, MapInitEvent>(OnMapInit);
    }

    private void OnBookUiRangeCheck(EntityUid uid, BookComponent component, ref BoundUserInterfaceCheckRangeEvent args)
    {
        args.Result = _interaction.InRangeUnobstructed((args.Actor, null), (uid, null), 2.0f)
            ? BoundUserInterfaceRangeResult.Pass
            : BoundUserInterfaceRangeResult.Fail;
    }

    private BookBoundUserInterfaceState CreateBookUIState(BookComponent component, bool isEditing = false)
    {
        return new BookBoundUserInterfaceState(
            component.Pages,
            component.CurrentPage,
            component.MaxCharactersPerPage,
            component.MaxPages,
            component.Bookmarks,
            component.MaxBookmarks,
            isEditing);
    }

    private void OnActivateInWorld(EntityUid uid, BookComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actorComponent))
            return;

        if (_uiSystem.IsUiOpen(uid, BookUiKey.Key, args.User))
        {
            var state = CreateBookUIState(component, false);
            _uiSystem.SetUiState(uid, BookUiKey.Key, state);
            _uiSystem.OpenUi(uid, BookUiKey.Key, actorComponent.PlayerSession);
        }
        else
        {
            var state = CreateBookUIState(component, false);
            _uiSystem.SetUiState(uid, BookUiKey.Key, state);
        }
    }

    private void OnPageChanged(EntityUid uid, BookComponent component, BookPageChangedMessage args)
    {
        component.CurrentPage = Math.Clamp(args.NewPage, 0, Math.Max(0, component.Pages.Count - 1));
        Dirty(uid, component);
    }

    public void AddTextToBook(EntityUid uid, BookComponent component, string text)
    {
        var words = text.Split(' ');
        var currentPage = component.Pages.LastOrDefault() ?? "";
        var pageIndex = Math.Max(0, component.Pages.Count - 1);

        foreach (var word in words)
        {
            if (currentPage.Length + word.Length + 1 > component.MaxCharactersPerPage)
            {
                if (pageIndex >= component.Pages.Count)
                    component.Pages.Add("");
                else
                    component.Pages[pageIndex] = currentPage;

                pageIndex++;
                currentPage = word;

                var state = CreateBookUIState(component, true);
                _uiSystem.SetUiState(uid, BookUiKey.Key, state);
            }
            else
            {
                currentPage += (currentPage.Length > 0 ? " " : "") + word;
            }
        }

        if (pageIndex >= component.Pages.Count)
            component.Pages.Add(currentPage);
        else
            component.Pages[pageIndex] = currentPage;
        Dirty(uid, component);
    }

    private void OnAddText(EntityUid uid, BookComponent component, BookAddTextMessage args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            if (component.Pages.Count >= component.MaxPages)
            {
                return;
            }

            component.Pages.Add("");
            component.CurrentPage = component.Pages.Count - 1;
        }
        else
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

            while (!string.IsNullOrEmpty(remainingText))
            {
                if (remainingText.Length <= component.MaxCharactersPerPage)
                {
                    component.Pages[currentPageIndex] = remainingText;
                    break;
                }
                else
                {
                    var pageText = remainingText.Substring(0, component.MaxCharactersPerPage);

                    var lastSpaceIndex = pageText.LastIndexOf(' ');
                    if (lastSpaceIndex > 0 && lastSpaceIndex > component.MaxCharactersPerPage * 0.8)
                    {
                        pageText = pageText.Substring(0, lastSpaceIndex);
                        remainingText = remainingText.Substring(lastSpaceIndex + 1);
                    }
                    else
                    {
                        remainingText = remainingText.Substring(component.MaxCharactersPerPage);
                    }

                    component.Pages[currentPageIndex] = pageText;
                    currentPageIndex++;

                    if (currentPageIndex >= component.Pages.Count)
                    {
                        if (component.Pages.Count >= component.MaxPages)
                        {
                            break;
                        }
                        component.Pages.Add("");
                    }
                }
            }

            component.CurrentPage = Math.Min(currentPageIndex, component.Pages.Count - 1);
            _audioSystem.PlayPvs(component.SaveSound, uid);
        }

        Dirty(uid, component);

        var state = CreateBookUIState(component, false);
        _uiSystem.SetUiState(uid, BookUiKey.Key, state);
    }

    private void OnInteractUsing(EntityUid uid, BookComponent component, InteractUsingEvent args)
    {
        if (_tagSystem.HasTag(args.Used, "Write"))
        {
            if (TryComp<ActorComponent>(args.User, out var actor))
            {
                var state = CreateBookUIState(component, true);
                _uiSystem.SetUiState(uid, BookUiKey.Key, state);
                _uiSystem.OpenUi(uid, BookUiKey.Key, actor.PlayerSession);
            }
        }
    }

    private void OnUseInHand(EntityUid uid, BookComponent component, UseInHandEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actorComponent))
            return;

        if (_uiSystem.IsUiOpen(uid, BookUiKey.Key, args.User))
        {
            var state = CreateBookUIState(component, false);
            _uiSystem.SetUiState(uid, BookUiKey.Key, state);
            _uiSystem.OpenUi(uid, BookUiKey.Key, actorComponent.PlayerSession);
        }
        else
        {
            _uiSystem.CloseUi(uid, BookUiKey.Key, args.User);
        }

        args.Handled = true;
    }

    private void OnIntrinsicUIOpenAttempt(EntityUid uid, BookComponent component, IntrinsicUIOpenAttemptEvent args)
    {
        if (args.Key?.Equals(BookUiKey.Key) == true)
        {
            var state = CreateBookUIState(component, false);
            _uiSystem.SetUiState(uid, BookUiKey.Key, state);
        }
    }

    private void BeforeUIOpen(Entity<BookComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        var state = CreateBookUIState(entity.Comp, false);
        _uiSystem.SetUiState(entity.Owner, BookUiKey.Key, state);
    }

    private void OnAddBookmark(EntityUid uid, BookComponent component, BookAddBookmarkMessage args)
    {
        if (component.Bookmarks.Count >= component.MaxBookmarks)
        {
            return;
        }

        if (component.Bookmarks.ContainsKey(args.PageIndex))
        {
            return;
        }

        component.Bookmarks[args.PageIndex] = args.BookmarkName;
        Dirty(uid, component);
        UpdateBookUI(uid, component);
    }

    private void OnRemoveBookmark(EntityUid uid, BookComponent component, BookRemoveBookmarkMessage args)
    {
        if (component.Bookmarks.Remove(args.PageIndex))
        {
            Dirty(uid, component);
            UpdateBookUI(uid, component);
        }
    }

    private void UpdateBookUI(EntityUid uid, BookComponent component)
    {
        var state = CreateBookUIState(component, false);
        _uiSystem.SetUiState(uid, BookUiKey.Key, state);
    }

    private void GenerateRandomContent(EntityUid uid, BookComponent component)
    {
        if (component.Pages.Count == 1 && string.IsNullOrEmpty(component.Pages[0]))
        {
            var story = new FormattedMessage();

            story.AddText(Loc.GetString("book-story-template-this-is"));
            story.AddText(GetRandomLocString("story-gen-book-genre"));
            story.AddText(Loc.GetString("book-story-template-about"));
            story.AddText(GetRandomLocString("story-gen-book-character-trait"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-character"));
            story.AddText(Loc.GetString("book-story-template-and"));
            story.AddText(GetRandomLocString("story-gen-book-character-trait"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-character"));
            story.AddText(Loc.GetString("book-story-template-due-to"));
            story.AddText(GetRandomLocString("story-gen-book-event"));
            story.AddText(Loc.GetString("book-story-template-comma"));
            story.AddText(Loc.GetString("book-story-template-they"));
            story.AddText(GetRandomLocString("story-gen-book-action-trait"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-action"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-character-story"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-location"));
            story.AddText(Loc.GetString("book-story-template-period"));
            story.PushNewline();
            story.PushNewline();
            story.AddText(GetRandomLocString("story-gen-book-element"));
            story.AddText(Loc.GetString("book-story-template-is"));
            story.AddText(GetRandomLocString("story-gen-book-element-trait"));
            story.AddText(Loc.GetString("book-story-template-period"));

            component.Pages[0] = story.ToMarkup();
            Dirty(uid, component);
        }
    }

    private string GetRandomLocString(string prefix)
    {
        var maxVariants = GetMaxVariantsForPrefix(prefix);
        var randomIndex = _random.Next(1, maxVariants + 1);
        return Loc.GetString($"{prefix}{randomIndex}");
    }

    private int GetMaxVariantsForPrefix(string prefix)
    {
        return prefix switch
        {
            "story-gen-book-genre" => 14,
            "story-gen-book-character" => 40,
            "story-gen-book-character-story" => 40,
            "story-gen-book-character-trait" => 24,
            "story-gen-book-event" => 24,
            "story-gen-book-action" => 12,
            "story-gen-book-action-trait" => 13,
            "story-gen-book-location" => 34,
            "story-gen-book-element" => 9,
            "story-gen-book-element-trait" => 13,
            _ => 1
        };
    }

    private void OnMapInit(EntityUid uid, BookComponent component, MapInitEvent args)
    {
        if (MetaData(uid).EntityPrototype?.ID == "BookRandomStory")
        {
            GenerateRandomContent(uid, component);
        }

        if (!string.IsNullOrEmpty(component.Content))
        {
            var localizedContent = Loc.GetString(component.Content);
            SplitContentIntoPages(component, localizedContent);
            Dirty(uid, component);
        }
    }

    public void SplitContentIntoPages(BookComponent component, string content)
    {
        component.Pages.Clear();
        var remainingText = content;
        while (!string.IsNullOrEmpty(remainingText))
        {
            if (remainingText.Length <= component.MaxCharactersPerPage)
            {
                component.Pages.Add(remainingText);
                break;
            }

            var pageText = remainingText.Substring(0, component.MaxCharactersPerPage);
            var lastSpaceIndex = pageText.LastIndexOf(' ');

            if (lastSpaceIndex > 0 && lastSpaceIndex > component.MaxCharactersPerPage * 0.8)
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

        if (component.Pages.Count == 0)
            component.Pages.Add("");
    }
}
