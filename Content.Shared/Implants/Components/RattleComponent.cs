using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

[RegisterComponent, NetworkedComponent]
public sealed partial class RattleComponent : Component
{
    // The radio channels the message will be sent to
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<RadioChannelPrototype>))] //WWDP edit
    public List<string> RadioChannel = new() { "Syndicate" }; //WWDP edit

    [DataField]
    public LocId ReviveMessage = "deathrattle-implant-revive-message";

    // The message that the implant will send when crit
    [DataField]
    public LocId CritMessage = "deathrattle-implant-critical-message";

    // The message that the implant will send when dead
    [DataField("deathMessage")]
    public LocId DeathMessage = "deathrattle-implant-dead-message";

    # WD EDIT START
    [DataField]
    public LocId ReviveMessage = "deathrattle-implant-revive-message";
    # WD EDIT END
}
