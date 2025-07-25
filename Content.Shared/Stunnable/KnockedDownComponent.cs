using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunSystem))]
public sealed partial class KnockedDownComponent : Component
{
    [DataField("helpInterval"), AutoNetworkedField]
    public float HelpInterval = 1f;

    [DataField("helpAttemptSound")]
    public SoundSpecifier StunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public DropHeldItemsBehavior DropHeldItemsBehavior = DropHeldItemsBehavior.AlwaysDrop;

    [DataField]
    public bool FollowUp = false;

    [ViewVariables, AutoNetworkedField]
    public float HelpTimer = 0f;

    /// <summary>
    /// WWDP - friction modifier for being unconscious, slipped etc.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionMultiplier = 1f;
}
