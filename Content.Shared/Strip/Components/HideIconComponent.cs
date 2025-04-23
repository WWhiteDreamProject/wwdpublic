using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HideIconComponent : Component
{
    [DataField("clickable")]
    public bool Clickable = true;

    [ViewVariables]
    public bool RequiresStripHiddenComponent = false; // Дополнительная настройка
}
