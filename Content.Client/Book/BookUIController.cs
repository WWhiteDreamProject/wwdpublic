using Content.Shared.Book.Components;
using Content.Shared.Book;
using Content.Client.Book.UI;
using Robust.Client.UserInterface.Controllers;
using JetBrains.Annotations;

namespace Content.Client.Book;

[UsedImplicitly]
public sealed class BookUIController : UIController
{
    private Dictionary<EntityUid, BookWindow> _openBooks = new();

    public void OpenBook(EntityUid bookEntity, BookBoundUserInterfaceState bookState)
    {
        if (_openBooks.ContainsKey(bookEntity))
        {
            _openBooks[bookEntity].MoveToFront();
            return;
        }

        var window = UIManager.CreateWindow<BookWindow>();
        window.ShowBook(bookState, false);
        window.OnClose += () => _openBooks.Remove(bookEntity);

        _openBooks[bookEntity] = window;
        window.OpenCentered();
    }
}
