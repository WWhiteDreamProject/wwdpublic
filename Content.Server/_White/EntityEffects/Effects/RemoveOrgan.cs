using Content.Server.Body.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._White.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class RemoveOrgan : EntityEffect
{
    [DataField(required: true)]
    public string SlotId;

    [DataField]
    public bool Delete;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var bodySystem = args.EntityManager.System<BodySystem>();

        foreach (var (organId, organComponent) in bodySystem.GetBodyOrgans(args.TargetEntity))
        {
            if (organComponent.SlotId != SlotId)
                continue;

            bodySystem.RemoveOrgan(organId, organComponent);

            if (Delete)
                args.EntityManager.QueueDeleteEntity(organId);
        }
    }
}
