using Content.Client.Book.UI;
using Content.Shared.Book;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Book;

[UsedImplicitly]
public sealed class BookUIController : UIController
{
    private readonly Dictionary<EntityUid, BookWindow> _openBooks = new();

    public void OpenBook(EntityUid bookEntity, BookBoundUserInterfaceState bookState)
    {
        if (_openBooks.TryGetValue(bookEntity, out var book))
        {
            book.MoveToFront();
            return;
        }

        var window = UIManager.CreateWindow<BookWindow>();
        window.ShowBook(bookState, false);
        window.OnClose += () => _openBooks.Remove(bookEntity);

        _openBooks[bookEntity] = window;
        window.OpenCentered();
    }
}
