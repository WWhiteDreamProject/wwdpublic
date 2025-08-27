using Robust.Shared.GameStates;

namespace Content.Shared._White.Book.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BookRandomStoryComponent : Component
{
    [DataField("template")]
    public string Template { get; set; } = "GenericStory";
}
