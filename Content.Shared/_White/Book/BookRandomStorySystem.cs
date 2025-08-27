using Content.Shared._White.Book.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._White.Book;

public sealed class BookRandomStorySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BookRandomStoryComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BookRandomStoryComponent component, ref MapInitEvent args)
    {
        if (!TryComp<BookComponent>(uid, out var bookComponent))
            return;

        GenerateRandomContent(uid, bookComponent);
    }

    private void GenerateRandomContent(EntityUid uid, BookComponent component)
    {
        if (component.Pages.Count == 1 && string.IsNullOrEmpty(component.Pages[0]))
        {
            var story = new FormattedMessage();

            story.AddText(Loc.GetString("book-story-template-this-is"));
            story.AddText(GetRandomLocString("story-gen-book-genre"));
            story.AddText(Loc.GetString("book-story-template-about"));
            story.AddText(GetRandomLocString("story-gen-book-character-trait"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-character"));
            story.AddText(Loc.GetString("book-story-template-and"));
            story.AddText(GetRandomLocString("story-gen-book-character-trait"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-character"));
            story.AddText(Loc.GetString("book-story-template-due-to"));
            story.AddText(GetRandomLocString("story-gen-book-event"));
            story.AddText(Loc.GetString("book-story-template-comma"));
            story.AddText(Loc.GetString("book-story-template-they"));
            story.AddText(GetRandomLocString("story-gen-book-action-trait"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-action"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-character-story"));
            story.AddText(Loc.GetString("book-story-template-space"));
            story.AddText(GetRandomLocString("story-gen-book-location"));
            story.AddText(Loc.GetString("book-story-template-period"));
            story.PushNewline();
            story.PushNewline();
            story.AddText(GetRandomLocString("story-gen-book-element"));
            story.AddText(Loc.GetString("book-story-template-is"));
            story.AddText(GetRandomLocString("story-gen-book-element-trait"));
            story.AddText(Loc.GetString("book-story-template-period"));

            component.Pages[0] = story.ToMarkup();
            Dirty(uid, component);
        }
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
