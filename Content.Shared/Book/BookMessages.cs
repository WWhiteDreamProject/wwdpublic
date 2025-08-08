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
        Pages = pages ?? new List<string>();
        CurrentPage = Math.Max(0, currentPage);
        MaxCharactersPerPage = Math.Max(0, maxCharactersPerPage);
        MaxPages = Math.Max(0, maxPages);
        IsEditing = isEditing;
        Bookmarks = bookmarks ?? new Dictionary<int, string>();
        MaxBookmarks = Math.Max(0, maxBookmarks);
    }
}

[Serializable, NetSerializable]
public sealed class BookPageChangedMessage : BoundUserInterfaceMessage
{
    public readonly int NewPage;

    public BookPageChangedMessage(int newPage)
    {
        NewPage = Math.Max(0, newPage);
    }
}

[Serializable, NetSerializable]
public sealed class BookAddTextMessage : BoundUserInterfaceMessage
{
    public readonly string Text;

    public BookAddTextMessage(string text)
    {
        Text = text ?? string.Empty;
    }
}

[Serializable, NetSerializable]
public sealed class BookAddBookmarkMessage : BoundUserInterfaceMessage
{
    public readonly int PageIndex;
    public readonly string BookmarkName;

    public BookAddBookmarkMessage(int pageIndex, string bookmarkName)
    {
        PageIndex = Math.Max(0, pageIndex);
        BookmarkName = bookmarkName ?? string.Empty;
    }
}

[Serializable, NetSerializable]
public sealed class BookRemoveBookmarkMessage : BoundUserInterfaceMessage
{
    public readonly int PageIndex;

    public BookRemoveBookmarkMessage(int pageIndex)
    {
        PageIndex = Math.Max(0, pageIndex);
    }
}

[Serializable, NetSerializable]
public enum BookUiKey : byte
{
    Key
}
