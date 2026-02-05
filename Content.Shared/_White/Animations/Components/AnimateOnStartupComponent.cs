using Content.Shared._White.Animations.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Animations.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnimateOnStartupComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<AnimationPrototype> Animation = string.Empty;
}
