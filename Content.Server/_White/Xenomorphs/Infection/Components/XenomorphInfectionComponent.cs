using Content.Shared._White.StatusIcon;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Xenomorphs.Infection.Components;

[RegisterComponent]
public sealed partial class XenomorphInfectionComponent : Component
{
    [DataField]
    public int MaxGrowthStage = 1;

    [DataField]
    public EntProtoId LarvaPrototype = "MobXenomorphLarva";

    /// <summary>
    /// A set of prototype IDs for status icons representing different growth stages of the infection.
    /// </summary>
    [DataField]
    public Dictionary<int, ProtoId<InfectionIconPrototype>> InfectedIcons = new();

    /// <summary>
    /// The probability of infection growth per GrowTime.
    /// </summary>
    [DataField]
    public float GrowProb = 1f;

    /// <summary>
    /// The time required for infection to grow.
    /// </summary>
    [DataField]
    public TimeSpan GrowTime = TimeSpan.FromSeconds(25);

    [DataField]
    public Dictionary<int, List<EntityEffect>> Effects = new ();

    /// <summary>
    /// Current stage of infection development.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int GrowthStage;

    [ViewVariables]
    public TimeSpan NextPointsAt;

    [ViewVariables]
    public bool Growing;

    [ViewVariables]
    public EntityUid? Infected;

}
