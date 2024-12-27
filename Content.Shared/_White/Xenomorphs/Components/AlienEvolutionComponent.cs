using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AlienEvolutionComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> DronePolymorphPrototype = "AlienEvolutionDrone";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DroneEvolutionAction = "ActionEvolveDrone";

    [DataField]
    public EntityUid? DroneEvolutionActionEntity;

    [DataField]
    public ProtoId<PolymorphPrototype> SentinelPolymorphPrototype = "AlienEvolutionSentinel";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SentinelEvolutionAction = "ActionEvolveSentinel";

    [DataField]
    public EntityUid? SentinelEvolutionActionEntity;

    [DataField]
    public ProtoId<PolymorphPrototype> HunterPolymorphPrototype = "AlienEvolutionHunter";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? HunterEvolutionAction = "ActionEvolveHunter";

    [DataField]
    public EntityUid? HunterEvolutionActionEntity;

    [DataField]
    public TimeSpan EvolutionCooldown;

}
