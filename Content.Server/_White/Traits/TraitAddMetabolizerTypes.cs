using Content.Server._White.Body.Organs.Metabolizer;
using Content.Server._White.Body.Systems;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Organs.Metabolizer;
using Content.Shared._White.Body.Prototypes;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._White.Traits;

[UsedImplicitly]
public sealed partial class TraitAddMetabolizerTypes : TraitFunction
{
    [DataField(required: true), AlwaysPushInheritance]
    public HashSet<ProtoId<MetabolizerTypePrototype>> MetabolizerTypes;

    [DataField, AlwaysPushInheritance]
    public OrganType OrganType = OrganType.None;

    public override void OnPlayerSpawn(
        EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager
    )
    {
        var bodySystem = entityManager.System<BodySystem>();
        var metabolizerSystem = entityManager.System<MetabolizerSystem>();

        foreach (var metabolizer in bodySystem.GetOrgans<MetabolizerComponent>(uid, OrganType))
            metabolizerSystem.AddMetabolizerTypes((metabolizer, metabolizer), MetabolizerTypes);
    }
}
