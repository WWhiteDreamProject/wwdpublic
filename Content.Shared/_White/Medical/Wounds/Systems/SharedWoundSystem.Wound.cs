using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Wounds.Components.Wound;
using Content.Shared._White.Medical.Wounds.Components.Woundable;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    #region Private API

    private FixedPoint2 ChangeWoundDamage(Entity<WoundComponent?> wound, FixedPoint2 damageAmount)
    {
        if (!Resolve(wound, ref wound.Comp))
            return FixedPoint2.Zero;

        var oldDamage = wound.Comp.DamageAmount;
        wound.Comp.DamageAmount = oldDamage.Float() + damageAmount.Float();
        wound.Comp.WoundSeverity = wound.Comp.Thresholds.HighestMatch(wound.Comp.DamageAmount) ?? WoundSeverity.Healthy;

        if (wound.Comp.DamageAmount > oldDamage)
            wound.Comp.WoundedAt = _gameTiming.CurTime;

        RaiseLocalEvent(wound, new WoundDamageChangedEvent(wound.Comp, oldDamage));
        Dirty(wound);

        if (wound.Comp.DamageAmount == FixedPoint2.Zero)
            PredictedDel(wound.Owner);

        return wound.Comp.DamageAmount - oldDamage;
    }

    private bool TryCreateWound(
        Entity<BodyPartComponent, DamageableComponent?, WoundableBodyPartComponent> bodyPart,
        EntProtoId woundToSpawn,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        Entity<DamageableComponent?, WoundableComponent>? woundable = null
    )
    {
        wound = null;

        if (!PredictedTrySpawnInContainer(woundToSpawn, bodyPart, WoundsContainerId, out var woundUid))
            return false;

        if (!TryComp<WoundComponent>(woundUid, out var woundComponent))
        {
            PredictedQueueDel(woundUid);
            return false;
        }

        wound = (woundUid.Value, woundComponent);
        woundComponent.Parent = bodyPart;
        woundComponent.WoundedAt = _gameTiming.CurTime;

        if (woundable == null && TryComp<WoundableComponent>(bodyPart.Comp3.Parent, out var woundableComponent))
            woundable = (bodyPart.Comp3.Parent.Value,null,  woundableComponent);

        if (woundable != null)
            woundable.Value.Comp2.Wounds[bodyPart.Comp1.Type] = bodyPart.Comp3.Wounds;

        woundComponent.Body = woundable;

        return true;
    }

    #endregion

    #region Public API

    public List<Entity<WoundComponent>> GetWounds(Entity<WoundableComponent?> woundable, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (!Resolve(woundable, ref woundable.Comp))
            return new List<Entity<WoundComponent>>();

        var wounds = new List<Entity<WoundComponent>>();
        foreach (var (woundBodyPartType, woundList) in woundable.Comp.Wounds)
        {
            if (!bodyPartType.HasFlag(woundBodyPartType))
                continue;

            foreach (var wound in woundList)
            {
                if (!TryComp<WoundComponent>(wound, out var woundComponent)
                    || !string.IsNullOrEmpty(damageType) && woundComponent.DamageType != damageType)
                    continue;

                wounds.Add((wound, woundComponent));
            }
        }

        return wounds;
    }

    public List<Entity<WoundComponent>> GetWounds(Entity<WoundableBodyPartComponent?> woundableBodyPart, string? damageType = null)
    {
        if (!Resolve(woundableBodyPart, ref woundableBodyPart.Comp))
            return new List<Entity<WoundComponent>>();

        var wounds = new List<Entity<WoundComponent>>();
        foreach (var wound in woundableBodyPart.Comp.Wounds)
        {
            if (!TryComp<WoundComponent>(wound, out var woundComponent)
                || !string.IsNullOrEmpty(damageType) && woundComponent.DamageType != damageType)
                continue;

            wounds.Add((wound, woundComponent));
        }

        return wounds;
    }

    public List<Entity<WoundComponent>> GetWounds(EntityUid parent, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (TryComp<WoundableComponent>(parent, out var woundableComponent))
            return GetWounds((parent, woundableComponent), damageType, bodyPartType);

        if (TryComp<WoundableBodyPartComponent>(parent, out var woundableBodyPartComponent))
            return GetWounds((parent, woundableBodyPartComponent), damageType);

        return new List<Entity<WoundComponent>>();
    }

    public List<Entity<WoundComponent, T>> GetWounds<T>(Entity<WoundableComponent?> woundable, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All) where T : IComponent
    {
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in GetWounds(woundable, damageType, bodyPartType))
        {
            if (!TryComp<T>(wound, out var component))
                continue;

            wounds.Add((wound.Owner, wound.Comp, component));
        }

        return wounds;
    }

    public List<Entity<WoundComponent, T>> GetWounds<T>(Entity<WoundableBodyPartComponent?> woundableBodyPart, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All) where T : IComponent
    {
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in GetWounds(woundableBodyPart, damageType, bodyPartType))
        {
            if (!TryComp<T>(wound, out var component))
                continue;

            wounds.Add((wound.Owner, wound.Comp, component));
        }

        return wounds;
    }

    public List<Entity<WoundComponent, T>> GetWounds<T>(EntityUid parent, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All) where T : IComponent
    {
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in GetWounds(parent, damageType, bodyPartType))
        {
            if (!TryComp<T>(wound, out var component))
                continue;

            wounds.Add((wound.Owner, wound.Comp, component));
        }

        return wounds;
    }

    public bool HasWounds(Entity<WoundableComponent?> woundable, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All) =>
        GetWounds(woundable, damageType, bodyPartType).Count > 0;

    public bool HasWounds(Entity<WoundableBodyPartComponent?> woundableBodyPart, string? damageType = null) =>
        GetWounds(woundableBodyPart, damageType).Count > 0;

    public bool HasWounds(EntityUid parent, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (TryComp<WoundableComponent>(parent, out var woundableComponent))
            return HasWounds((parent, woundableComponent), damageType, bodyPartType);

        if (TryComp<WoundableBodyPartComponent>(parent, out var woundableBodyPartComponent))
            return HasWounds((parent, woundableBodyPartComponent), damageType);

        return false;
    }

    public bool HasWounds(Entity<WoundableComponent?> woundable, DamageSpecifier damage, BodyPartType bodyPartType = BodyPartType.All)
    {
        foreach (var damageType in damage.DamageDict.Keys)
        {
            if (HasWounds(woundable, damageType, bodyPartType))
                return true;
        }

        return false;
    }

    public bool HasWounds(Entity<WoundableBodyPartComponent?> woundableBodyPart, DamageSpecifier damage)
    {
        foreach (var damageType in damage.DamageDict.Keys)
        {
            if (HasWounds(woundableBodyPart, damageType))
                return true;
        }

        return false;
    }

    public bool HasWounds(EntityUid parent, DamageSpecifier damage, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (TryComp<WoundableComponent>(parent, out var woundableComponent))
            return HasWounds((parent, woundableComponent), damage, bodyPartType);

        if (TryComp<WoundableBodyPartComponent>(parent, out var woundableBodyPartComponent))
            return HasWounds((parent, woundableBodyPartComponent), damage);

        return false;
    }

    #endregion
}

