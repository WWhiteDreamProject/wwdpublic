using Content.Shared._White.StatusIcon;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Xenomorphs.Components;

/// <summary>
/// The AlienInfectedComponent is used to manage the infection process and growth stages of alien larvae inside a host.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XenomorphInfectedComponent : Component
{
    /// <summary>
    /// The time required for the alien larva to grow.
    /// </summary>
    [DataField]
    public float GrowTime = 25f;

    /// <summary>
    /// The prototype ID for the larva entity to be spawned.
    /// </summary>
    [DataField]
    public EntProtoId Prototype = "MobAlienLarvaInside";

    /// <summary>
    /// A set of prototype IDs for status icons representing different growth stages of the infection.
    /// </summary>
    public readonly HashSet<ProtoId<InfectionIconPrototype>> InfectedIcons =
    [
        "AlienInfectedIconStageZero",
        "AlienInfectedIconStageOne",
        "AlienInfectedIconStageTwo",
        "AlienInfectedIconStageThree",
        "AlienInfectedIconStageFour",
        "AlienInfectedIconStageFive",
        "AlienInfectedIconStageSix"
    ];

    /// <summary>
    /// The current growth stage of the alien infection, starting at 0.
    /// </summary>
    [ViewVariables]
    public int GrowthStage = 0;

    /// <summary>
    /// The probability of the larva growing.
    /// </summary>
    [DataField]
    public float GrowProb = 1f;

    /// <summary>
    /// The time span until the next growth roll can occur.
    /// </summary>
    [DataField]
    public TimeSpan NextGrowRoll = TimeSpan.Zero;

    /// <summary>
    /// The container where the larva is located inside the host.
    /// </summary>
    public Container Stomach = default!;

    /// <summary>
    /// Optional reference to the entity representing the spawned larva.
    /// </summary>
    public EntityUid? SpawnedLarva;

    /// <summary>
    /// The damage specifier for the burst damage caused by the alien larva.
    /// </summary>
    [DataField]
    public DamageSpecifier BurstDamage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 100 }
        }
    };

    /// <summary>
    /// Indicator of whether the roots of the larva have been cut in surgery.
    /// </summary>
    public bool RootsCut;
}
