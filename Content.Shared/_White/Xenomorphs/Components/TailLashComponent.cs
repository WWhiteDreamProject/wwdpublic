using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TailLashComponent : Component
{
    [DataField]
    public EntProtoId? TailLashAction = "ActionTailLash";

    [DataField]
    public EntityUid? TailLashActionEntity;

    [DataField]
    public float LashRange = 2f;

    [DataField]
    public int StunTime = 1;

    [DataField]
    public int Cooldown = 15;

    [DataField]
    public SoundSpecifier LashSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");
}

public sealed partial class TailLashActionEvent : InstantActionEvent { }
