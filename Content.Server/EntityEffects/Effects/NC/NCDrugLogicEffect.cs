using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._NC.Chemistry.Components;
using Content.Shared._Shitmed.Body.Events;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// NC: Универсальный эффект для продвинутых наркотиков.
/// Обрабатывает шансы отрыва конечностей, кровь, слепоту и смех.
/// </summary>
[UsedImplicitly, DataDefinition]
public sealed partial class NCDrugLogicEffect : EntityEffect
{
    [DataField("headGibChance")] public float HeadGibChance = 0f;
    [DataField("armDropChance")] public float ArmDropChance = 0f;
    [DataField("legDropChance")] public float LegDropChance = 0f;

    [DataField("blindChance")] public float BlindChance = 0f;
    [DataField("laughChance")] public float LaughChance = 0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Вызывает комплексные побочные эффекты препарата.";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var random = IoCManager.Resolve<IRobustRandom>();
        var timing = IoCManager.Resolve<IGameTiming>();
        var popupSystem = entityManager.System<SharedPopupSystem>();

        // 1. ОГРАНИЧЕНИЕ ЧАСТОТЫ ПРОВЕРОК (1 РАЗ В СЕКУНДУ)
        var ncComp = entityManager.EnsureComponent<NCReagentEffectsComponent>(target);
        if (timing.CurTime < ncComp.LastLimbCheckTime + TimeSpan.FromSeconds(1))
            return;
        
        ncComp.LastLimbCheckTime = timing.CurTime;
        entityManager.Dirty(target, ncComp);

        // 2. ПРОВЕРКА КОНЕЧНОСТЕЙ
        if (!entityManager.TryGetComponent<BodyComponent>(target, out var body))
            return;

        var bodySystem = entityManager.System<BodySystem>();

        if (HeadGibChance > 0 && random.Prob(HeadGibChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Head)
                {
                    SpawnBlood(target, entityManager);
                    bodySystem.GibPart(part.Id, part.Component);
                    break;
                }
            }
        }

        if (ArmDropChance > 0 && random.Prob(ArmDropChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Arm)
                {
                    DropLimb(target, part.Id, part.Component, bodySystem, entityManager);
                    break;
                }
            }
        }

        if (LegDropChance > 0 && random.Prob(LegDropChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Leg)
                {
                    DropLimb(target, part.Id, part.Component, bodySystem, entityManager);
                    break;
                }
            }
        }

        // 3. ПРОЧЕЕ
        if (BlindChance > 0 && random.Prob(BlindChance))
        {
            entityManager.System<StatusEffectsSystem>().TryAddStatusEffect(target, "TemporaryBlindness", TimeSpan.FromSeconds(5), true, "TemporaryBlindness");
        }

        if (LaughChance > 0 && random.Prob(LaughChance))
        {
            entityManager.System<ChatSystem>().TryEmoteWithoutChat(target, "Laugh");
        }
    }

    private void DropLimb(EntityUid target, EntityUid partId, BodyPartComponent partComp, BodySystem bodySystem, IEntityManager entMan)
    {
        SpawnBlood(target, entMan);
        var ev = new AmputateAttemptEvent(partId);
        entMan.EventBus.RaiseLocalEvent(partId, ref ev);
        
        if (entMan.TryGetComponent<Content.Server.Body.Components.BloodstreamComponent>(target, out var bloodstream))
        {
            entMan.System<BloodstreamSystem>().TryModifyBleedAmount(target, 10f, bloodstream);
        }
    }

    private void SpawnBlood(EntityUid target, IEntityManager entMan)
    {
        var xform = entMan.GetComponent<TransformComponent>(target);
        entMan.System<PuddleSystem>().TrySpillAt(xform.Coordinates, new Solution("Blood", FixedPoint2.New(10)), out _);
    }
}
