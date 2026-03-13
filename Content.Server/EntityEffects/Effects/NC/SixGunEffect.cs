using Content.Shared._Shitmed.Body.Events;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Server.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.StatusEffect;
using Content.Shared.Popups;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Content.Server.Chat.Systems;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects;

[UsedImplicitly, DataDefinition]
public sealed partial class SixGunEffect : EntityEffect
{
    [DataField("armDropChance")]
    public float ArmDropChance = 0.5f;

    [DataField("legDropChance")]
    public float LegDropChance = 0.5f;

    [DataField("blindChance")]
    public float BlindChance = 0.15f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Вызывает безумную скорость, но с высоким шансом отрывает конечности, ослепляет и вызывает приступы смеха.";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var random = IoCManager.Resolve<IRobustRandom>();
        var bodySystem = entityManager.System<BodySystem>();
        var statusEffectsSystem = entityManager.System<StatusEffectsSystem>();
        var chatSystem = entityManager.System<ChatSystem>();
        var popupSystem = entityManager.System<SharedPopupSystem>();

        if (!entityManager.TryGetComponent<BodyComponent>(target, out var body))
            return;

        // Шанс отрыва рук
        if (random.Prob(ArmDropChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Arm)
                {
                    popupSystem.PopupEntity(Loc.GetString("six-gun-arm-drop"), target, PopupType.LargeCaution);
                    var ev = new AmputateAttemptEvent(part.Id);
                    entityManager.EventBus.RaiseLocalEvent(part.Id, ref ev);
                    break;
                }
            }
        }

        // Шанс отрыва ног
        if (random.Prob(LegDropChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Leg)
                {
                    popupSystem.PopupEntity(Loc.GetString("six-gun-leg-drop"), target, PopupType.LargeCaution);
                    var ev = new AmputateAttemptEvent(part.Id);
                    entityManager.EventBus.RaiseLocalEvent(part.Id, ref ev);
                    break;
                }
            }
        }

        if (random.Prob(BlindChance))
        {
            popupSystem.PopupEntity(Loc.GetString("six-gun-blindness"), target, PopupType.LargeCaution);
            statusEffectsSystem.TryAddStatusEffect(target, "TemporaryBlindness", TimeSpan.FromSeconds(5), true, "TemporaryBlindness");
        }

        if (random.Prob(0.2f))
        {
            chatSystem.TryEmoteWithoutChat(target, "Laugh");
        }
    }
}
