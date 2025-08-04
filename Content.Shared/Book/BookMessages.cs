using Robust.Shared.Serialization;

namespace Content.Shared.Book;

[Serializable, NetSerializable]
public sealed class BookBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<string> Pages;
    public readonly int CurrentPage;
    public readonly int MaxCharactersPerPage;
    public readonly int MaxPages;
    public readonly bool IsEditing;
    public readonly Dictionary<int, string> Bookmarks;
    public readonly int MaxBookmarks;

    public BookBoundUserInterfaceState(List<string> pages, int currentPage,
        int maxCharactersPerPage, int maxPages, Dictionary<int, string> bookmarks,
        int maxBookmarks, bool isEditing)
    {
        Pages = pages;
        CurrentPage = currentPage;
        MaxCharactersPerPage = maxCharactersPerPage;
        MaxPages = maxPages;
        IsEditing = isEditing;
        Bookmarks = bookmarks;
        MaxBookmarks = maxBookmarks;
    }
}

[Serializable, NetSerializable]
public sealed class BookPageChangedMessage : BoundUserInterfaceMessage
{
    public readonly int NewPage;

    public BookPageChangedMessage(int newPage)
    {
        NewPage = newPage;
    }
}

[Serializable, NetSerializable]
public sealed class BookAddTextMessage : BoundUserInterfaceMessage
{
    public readonly string Text;

    public BookAddTextMessage(string text)
    {
        Text = text;
    }
}

[Serializable, NetSerializable]
public sealed class BookAddBookmarkMessage : BoundUserInterfaceMessage
{
    public readonly int PageIndex;
    public readonly string BookmarkName;

    public BookAddBookmarkMessage(int pageIndex, string bookmarkName)
    {
        PageIndex = pageIndex;
        BookmarkName = bookmarkName;
    }
}

[Serializable, NetSerializable]
public sealed class BookRemoveBookmarkMessage : BoundUserInterfaceMessage
{
    public readonly int PageIndex;

    public BookRemoveBookmarkMessage(int pageIndex)
    {
        PageIndex = pageIndex;
    }
}

[Serializable, NetSerializable]
public enum BookUiKey : byte
{
    Key
}
