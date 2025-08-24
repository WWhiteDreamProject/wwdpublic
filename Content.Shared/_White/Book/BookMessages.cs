using Robust.Shared.Serialization;

namespace Content.Shared._White.Book;

[Serializable, NetSerializable]
public sealed class BookBoundUserInterfaceState(
    List<string> pages,
    int currentPage,
    int maxCharactersPerPage,
    int maxPages,
    Dictionary<int, string> bookmarks,
    int maxBookmarks,
    bool isEditing)
    : BoundUserInterfaceState
{
    public readonly List<string> Pages = pages;
    public readonly int CurrentPage = Math.Max(0, currentPage);
    public readonly int MaxCharactersPerPage = Math.Max(0, maxCharactersPerPage);
    public readonly int MaxPages = Math.Max(0, maxPages);
    public readonly bool IsEditing = isEditing;
    public readonly Dictionary<int, string> Bookmarks = bookmarks;
    public readonly int MaxBookmarks = Math.Max(0, maxBookmarks);
}

[Serializable, NetSerializable]
public sealed class BookPageChangedMessage(int newPage) : BoundUserInterfaceMessage
{
    public readonly int NewPage = Math.Max(0, newPage);
}

[Serializable, NetSerializable]
public sealed class BookAddTextMessage(string text) : BoundUserInterfaceMessage
{
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class BookAddBookmarkMessage(int pageIndex, string bookmarkName) : BoundUserInterfaceMessage
{
    public readonly int PageIndex = Math.Max(0, pageIndex);
    public readonly string BookmarkName = bookmarkName;
}

[Serializable, NetSerializable]
public sealed class BookRemoveBookmarkMessage(int pageIndex) : BoundUserInterfaceMessage
{
    public readonly int PageIndex = Math.Max(0, pageIndex);
}

[Serializable, NetSerializable]
public enum BookUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BookDeletePageMessage(int pageIndex) : BoundUserInterfaceMessage
{
    public readonly int PageIndex = Math.Max(0, pageIndex);
}

[Serializable, NetSerializable]

public sealed class BookAddPageMessage() : BoundUserInterfaceMessage
{
    //TODO: any else, but except DELETING THAT METHOD
}
