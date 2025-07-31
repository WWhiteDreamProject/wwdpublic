using Content.Server.Body.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._White.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class AddOrgan : EntityEffect
{
    [DataField(required: true)]
    public string SlotId;

    [DataField(required: true)]
    public EntProtoId Prototype;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var bodySystem = args.EntityManager.System<BodySystem>();

        foreach (var (partUid, partComponent) in bodySystem.GetBodyChildren(args.TargetEntity))
        {
            foreach (var slotId in partComponent.Organs.Keys)
            {
                if (slotId != SlotId)
                    continue;

                var organ = args.EntityManager.Spawn(Prototype);
                if (!bodySystem.InsertOrgan(partUid, organ, slotId, partComponent))
                    args.EntityManager.QueueDeleteEntity(organ);
            }
        }
    }
}
