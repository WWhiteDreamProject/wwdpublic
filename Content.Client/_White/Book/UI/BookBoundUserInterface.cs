using Content.Shared._White.Book;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._White.Book.UI;

[UsedImplicitly]
public sealed class BookBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private BookWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BookWindow>();
        _window.OnPageChanged += OnPageChanged;
        _window.OnTextSaved += OnTextSaved;
        _window.OnAddPage += OnAddPage;
        _window.OnBookmarkAdded += OnBookmarkAdded;
        _window.OnBookmarkRemoved += OnBookmarkRemoved;
        _window.OnPageDeleted += OnPageDeleted;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BookBoundUserInterfaceState bookState)
            _window?.ShowBook(bookState, bookState.IsEditing);
    }

    private void OnAddPage()
    {
        SendMessage(new BookAddPageMessage());
    }

    private void OnPageChanged(int newPage)
    {
        SendMessage(new BookPageChangedMessage(newPage));
    }

    private void OnTextSaved(string text)
    {
        SendMessage(new BookAddTextMessage(text));
    }

    private void OnBookmarkAdded(int pageIndex, string bookmarkName)
    {
        SendMessage(new BookAddBookmarkMessage(pageIndex, bookmarkName));
    }

    private void OnBookmarkRemoved(int pageIndex)
    {
        SendMessage(new BookRemoveBookmarkMessage(pageIndex));
    }

    private void OnPageDeleted(int pageIndex)
    {
        SendMessage(new BookDeletePageMessage(pageIndex));
    }
}
