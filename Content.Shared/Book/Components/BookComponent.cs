using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Book.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BookComponent : Component
{
    [DataField("pages")]
    public List<string> Pages { get; set; } = new() { "" };

    [DataField("currentPage")]
    public int CurrentPage { get; set; } = 0;

    [DataField("maxCharactersPerPage")]
    public int MaxCharactersPerPage { get; set; } = 2000;

    [DataField("maxPages")]
    public int MaxPages { get; set; } = 150;

    [DataField("saveSound")]
    public SoundSpecifier SaveSound = new SoundCollectionSpecifier("PaperScribble");

    [DataField("bookmarks")]
    public Dictionary<int, string> Bookmarks { get; set; } = new();

    [DataField("maxBookmarks")]
    public int MaxBookmarks { get; set; } = 10;

    [DataField("content")]
    public string? Content { get; set; }

    [DataField("pageFlipSound")]
    public SoundSpecifier PageFlipSound = new SoundPathSpecifier("/Audio/Items/book_flip.ogg");

    [DataField("pageTearSound")]
    public SoundSpecifier PageTearSound = new SoundPathSpecifier("/Audio/Items/book_tear.ogg");
}
