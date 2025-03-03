using Content.Shared.DisplacementMap;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Hands.Components;


// paranoidal component networking:
// i genuinely am not sure if this should be networked
// most likely not, but i'll still make it networked
// just in the off chance client rotation gets desynced or something
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HoldingDropComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [AutoNetworkedField]
    public Angle Angle;
}
