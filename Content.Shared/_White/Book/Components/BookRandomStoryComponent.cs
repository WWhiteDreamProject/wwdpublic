using Content.Shared.StoryGen;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Book.Components;

[RegisterComponent]
public sealed partial class BookRandomStoryComponent : Component
{
    [DataField]
    public ProtoId<StoryTemplatePrototype> Template = "GenericStory";
}
