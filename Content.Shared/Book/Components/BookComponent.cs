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
    public int MaxCharactersPerPage { get; set; } = 3000;

    [DataField("maxPages")]
    public int MaxPages { get; set; } = 100;

    [DataField("saveSound")]
    public SoundSpecifier SaveSound = new SoundCollectionSpecifier("PaperScribble");

    [DataField("bookmarks")]
    public Dictionary<int, string> Bookmarks { get; set; } = new();

    [DataField("maxBookmarks")]
    public int MaxBookmarks { get; set; } = 10;

    [DataField("content")]
    public string? Content { get; set; }
}
