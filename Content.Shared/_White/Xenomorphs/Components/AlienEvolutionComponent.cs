using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

/// <summary>
/// The AlienEvolutionComponent is used for managing the evolution of alien entities into different forms.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AlienEvolutionComponent : Component
{
    /// <summary>
    /// Prototype ID for the drone polymorph effect.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype> DronePolymorphPrototype = "AlienEvolutionDrone";

    /// <summary>
    /// Optional action prototype ID for evolving into a drone.
    /// </summary>
    [DataField]
    public EntProtoId? DroneEvolutionAction = "ActionEvolveDrone";

    /// <summary>
    /// Optional reference to the entity that performs the drone evolution action.
    /// </summary>
    [DataField]
    public EntityUid? DroneEvolutionActionEntity;

    /// <summary>
    /// Prototype ID for the sentinel polymorph effect.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype> SentinelPolymorphPrototype = "AlienEvolutionSentinel";

    /// <summary>
    /// Optional action prototype ID for evolving into a sentinel.
    /// </summary>
    [DataField]
    public EntProtoId? SentinelEvolutionAction = "ActionEvolveSentinel";

    /// <summary>
    /// Optional reference to the entity that performs the sentinel evolution action.
    /// </summary>
    [DataField]
    public EntityUid? SentinelEvolutionActionEntity;

    /// <summary>
    /// Prototype ID for the hunter polymorph effect.
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype> HunterPolymorphPrototype = "AlienEvolutionHunter";

    /// <summary>
    /// Optional action prototype ID for evolving into a hunter.
    /// </summary>
    [DataField]
    public EntProtoId? HunterEvolutionAction = "ActionEvolveHunter";

    /// <summary>
    /// Optional reference to the entity that performs the hunter evolution action.
    /// </summary>
    [DataField]
    public EntityUid? HunterEvolutionActionEntity;

    /// <summary>
    /// The cooldown period for evolution actions.
    /// </summary>
    [DataField]
    public TimeSpan EvolutionCooldown;
}
