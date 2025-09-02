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
        base.Initialize();

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
}
