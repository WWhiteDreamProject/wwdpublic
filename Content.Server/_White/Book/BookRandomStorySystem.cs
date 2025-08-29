using Content.Shared._White.Book;
using Content.Shared._White.Book.Components;
using Content.Shared.StoryGen;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._White.Book;

public sealed class BookRandomStorySystem : EntitySystem
{
    [Dependency] private readonly BookSystem _book = default!;
    [Dependency] private readonly StoryGeneratorSystem _storyGen = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BookRandomStoryComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BookRandomStoryComponent component, ref MapInitEvent args)
    {
        if (!TryComp<BookComponent>(uid, out var book))
            return;

        if (!_storyGen.TryGenerateStoryFromTemplate(component.Template, out var story))
            return;

        _book.SplitContentIntoPages(book, story);
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
}
