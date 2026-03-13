using Content.Shared._Shitmed.Body.Events;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Server.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.Popups;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects;

[UsedImplicitly, DataDefinition]
public sealed partial class PrimeTimeEffect : EntityEffect
{
    [DataField("headGibChance")]
    public float HeadGibChance = 0.03f;

    [DataField("legDropChance")]
    public float LegDropChance = 0.5f;

    [DataField("damageMultiplier")]
    public float DamageMultiplier = 0.3f;

    private const string ProtectionKey = "PrimeTimeProtection";

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Вызывает экстремальные эффекты Прайм Тайма, включая риск взрыва головы, отрыва ног и 70% защиту от урона.";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;
        var random = IoCManager.Resolve<IRobustRandom>();
        var bodySystem = entityManager.System<BodySystem>();
        var statusEffectsSystem = entityManager.System<StatusEffectsSystem>();
        var popupSystem = entityManager.System<SharedPopupSystem>();

        statusEffectsSystem.TryAddStatusEffect(target, "StaminaModifier", TimeSpan.FromSeconds(1.5), false, "StaminaModifier");
        statusEffectsSystem.TryAddStatusEffect(target, "Stutter", TimeSpan.FromSeconds(1.5), false, "StutteringAccent");

        ApplyDamageProtection(entityManager, target);

        if (!entityManager.TryGetComponent<BodyComponent>(target, out var body))
            return;

        // Шанс взрыва головы
        if (random.Prob(HeadGibChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Head)
                {
                    popupSystem.PopupEntity(Loc.GetString("prime-time-head-explode"), target, PopupType.LargeCaution);
                    bodySystem.GibPart(part.Id, part.Component);
                    break;
                }
            }
        }

        // Шанс отрыва ноги
        if (random.Prob(LegDropChance))
        {
            foreach (var part in bodySystem.GetBodyChildren(target, body))
            {
                if (part.Component.PartType == BodyPartType.Leg)
                {
                    popupSystem.PopupEntity(Loc.GetString("prime-time-leg-drop"), target, PopupType.LargeCaution);
                    // Используем событие, которое вызовет DropPart внутри системы
                    var ev = new AmputateAttemptEvent(part.Id);
                    entityManager.EventBus.RaiseLocalEvent(part.Id, ref ev);
                    break;
                }
            }
        }
    }

    private void ApplyDamageProtection(IEntityManager entityManager, EntityUid target)
    {
        var buffComp = entityManager.EnsureComponent<DamageProtectionBuffComponent>(target);
        if (buffComp.Modifiers.ContainsKey(ProtectionKey))
            return;

        var modifierSet = new DamageModifierSetPrototype();
        modifierSet.Coefficients["Blunt"] = DamageMultiplier;
        modifierSet.Coefficients["Slash"] = DamageMultiplier;
        modifierSet.Coefficients["Piercing"] = DamageMultiplier;
        modifierSet.Coefficients["Heat"] = DamageMultiplier;
        modifierSet.Coefficients["Shock"] = DamageMultiplier;
        modifierSet.Coefficients["Cold"] = DamageMultiplier;
        modifierSet.Coefficients["Caustic"] = DamageMultiplier;
        modifierSet.Coefficients["Poison"] = DamageMultiplier;
        modifierSet.Coefficients["Radiation"] = DamageMultiplier;
        modifierSet.Coefficients["Asphyxiation"] = DamageMultiplier;
        modifierSet.Coefficients["Bloodloss"] = DamageMultiplier;
        modifierSet.Coefficients["Cellular"] = DamageMultiplier;

        buffComp.Modifiers[ProtectionKey] = modifierSet;
        entityManager.Dirty(target, buffComp);
    }
}
