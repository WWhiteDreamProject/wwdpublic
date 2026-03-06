using Content.Shared._White.Body.Bloodstream.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._White.Traits;

[UsedImplicitly]
public sealed partial class TraitReplaceBloodReagent : TraitFunction
{
    [DataField(required: true), AlwaysPushInheritance]
    public ProtoId<ReagentPrototype> Reagent;

    public override void OnPlayerSpawn(
        EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager
        )
    {
        var bloodstreamSystem = entityManager.System<SharedBloodstreamSystem>();
        bloodstreamSystem.ChangeBloodReagent(uid, Reagent);
    }
}
