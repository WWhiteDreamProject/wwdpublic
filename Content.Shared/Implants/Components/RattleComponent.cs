using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

[RegisterComponent, NetworkedComponent]
public sealed partial class RattleComponent : Component
{
    // The radio channels the message will be sent to
    [DataField]
    public List<ProtoId<RadioChannelPrototype>> RadioChannel = new() { "Syndicate" }; //WWDP edit

    // WWDP edit start
    [DataField]
    public LocId ReviveMessage = "deathrattle-implant-revive-message";
    // WWDP edit end

    // The message that the implant will send when crit
    [DataField]
    public LocId CritMessage = "deathrattle-implant-critical-message";

    // The message that the implant will send when dead
    [DataField("deathMessage")]
    public LocId DeathMessage = "deathrattle-implant-dead-message";
}
