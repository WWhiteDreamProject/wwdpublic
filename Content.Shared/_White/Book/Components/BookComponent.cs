using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._White.Book.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BookComponent : Component
{
    [DataField]
    public int CurrentPage { get; set; }

    [DataField]
    public int MaxBookmarks { get; set; } = 10;

    [DataField]
    public int MaxCharactersPerPage { get; set; } = 2000;

    [DataField]
    public int MaxPages { get; set; } = 150;

    [DataField]
    public SoundSpecifier PageFlipSound = new SoundPathSpecifier("/Audio/_White/Items/book_flip.ogg");

    [DataField]
    public SoundSpecifier PageTearSound = new SoundPathSpecifier("/Audio/_White/Items/book_tear.ogg");

    [DataField]
    public SoundSpecifier SaveSound = new SoundCollectionSpecifier("PaperScribble");

    [DataField]
    public string? Content { get; set; }

    [ViewVariables]
    public Dictionary<int, string> Bookmarks { get; set; } = new();

    [ViewVariables]
    public List<string> Pages { get; set; } = new() { string.Empty, };
}
