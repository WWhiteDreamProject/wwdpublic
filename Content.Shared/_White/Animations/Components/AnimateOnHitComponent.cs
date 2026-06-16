using Content.Shared._White.Animations.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Animations.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnimateOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ApplyToUser = true;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<AnimationPrototype> Animation = string.Empty;

    [DataField]
    public EntityWhitelist? Whitelist;
}
