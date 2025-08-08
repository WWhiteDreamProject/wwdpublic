using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ChangelingHivemindNameComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? HivemindName;
} 