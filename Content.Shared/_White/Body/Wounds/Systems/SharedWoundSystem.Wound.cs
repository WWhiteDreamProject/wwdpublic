using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Wounds.Components;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    #region Public API

    public FixedPoint2 ChangeWoundDamage(
        Entity<WoundComponent?> wound,
        FixedPoint2 damageAmount,
        EntityUid? origin = null
        )
    {
        if (!Resolve(wound, ref wound.Comp))
            return FixedPoint2.Zero;

        var oldDamage = wound.Comp.Damage;
        wound.Comp.Damage = FixedPoint2.Max(oldDamage + damageAmount, FixedPoint2.Zero);
        wound.Comp.WoundSeverity = wound.Comp.Thresholds.HighestMatch(wound.Comp.Damage) ?? WoundSeverity.Healthy;

        if (wound.Comp.Damage > oldDamage)
            wound.Comp.WoundedAt = _gameTiming.CurTime;

        Dirty(wound);

        var ev = new WoundDamageChangedEvent((wound, wound.Comp), oldDamage, origin);
        RaiseLocalEvent(wound, ev);
        RaiseLocalEvent(wound.Comp.Parent, ev);

        if (wound.Comp.Body.HasValue)
            RaiseLocalEvent(wound.Comp.Body.Value, ev);

        if (wound.Comp.IsScar && wound.Comp.Damage == FixedPoint2.Zero)
            PredictedDel(wound.Owner);

        return wound.Comp.Damage - oldDamage;
    }

    public bool TryCreateWound(
        Entity<BodyPartComponent?, WoundableBodyPartComponent?> bodyPart,
        EntProtoId woundToSpawn,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound
    )
    {
        wound = null;

        if (!Resolve(bodyPart, ref bodyPart.Comp1, ref bodyPart.Comp2)
            || !PredictedTrySpawnInContainer(woundToSpawn, bodyPart, WoundsContainerId, out var woundUid))
            return false;

        if (!TryComp<WoundComponent>(woundUid, out var woundComponent))
        {
            PredictedQueueDel(woundUid);
            return false;
        }

        wound = (woundUid.Value, woundComponent);
        woundComponent.Parent = bodyPart;
        woundComponent.WoundedAt = _gameTiming.CurTime;

        if (!TryComp<WoundableComponent>(bodyPart.Comp2.Parent, out var woundableComponent))
        {
            PredictedQueueDel(woundUid);
            return false;
        }

        woundableComponent.Wounds[bodyPart.Comp1.Type] = bodyPart.Comp2.Wounds;
        woundComponent.Body = bodyPart.Comp2.Parent;

        return true;
    }

    public List<Entity<WoundComponent>> GetWounds(Entity<WoundableComponent?> woundable, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All)
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
                    || !string.IsNullOrEmpty(damageType) && woundComponent.DamageType != damageType
                    || woundComponent.IsScar && !scar)
                    continue;

                wounds.Add((wound, woundComponent));
            }
        }

        return wounds;
    }

    public List<Entity<WoundComponent>> GetWounds(Entity<WoundableBodyPartComponent?> woundableBodyPart, string? damageType = null, bool scar = false)
    {
        if (!Resolve(woundableBodyPart, ref woundableBodyPart.Comp))
            return new List<Entity<WoundComponent>>();

        var wounds = new List<Entity<WoundComponent>>();
        foreach (var wound in woundableBodyPart.Comp.Wounds)
        {
            if (!TryComp<WoundComponent>(wound, out var woundComponent)
                || !string.IsNullOrEmpty(damageType) && woundComponent.DamageType != damageType
                || woundComponent.IsScar && !scar)
                continue;

            wounds.Add((wound, woundComponent));
        }

        return wounds;
    }

    public List<Entity<WoundComponent>> GetWounds(EntityUid parent, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (TryComp<WoundableComponent>(parent, out var woundableComponent))
            return GetWounds((parent, woundableComponent), damageType, scar, bodyPartType);

        if (TryComp<WoundableBodyPartComponent>(parent, out var woundableBodyPartComponent))
            return GetWounds((parent, woundableBodyPartComponent), damageType, scar);

        return new List<Entity<WoundComponent>>();
    }

    public List<Entity<WoundComponent, T>> GetWounds<T>(Entity<WoundableComponent?> woundable, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All) where T : IComponent
    {
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in GetWounds(woundable, damageType, scar, bodyPartType))
        {
            if (!TryComp<T>(wound, out var component))
                continue;

            wounds.Add((wound.Owner, wound.Comp, component));
        }

        return wounds;
    }

    public List<Entity<WoundComponent, T>> GetWounds<T>(Entity<WoundableBodyPartComponent?> woundableBodyPart, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All) where T : IComponent
    {
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in GetWounds(woundableBodyPart, damageType, scar, bodyPartType))
        {
            if (!TryComp<T>(wound, out var component))
                continue;

            wounds.Add((wound.Owner, wound.Comp, component));
        }

        return wounds;
    }

    public List<Entity<WoundComponent, T>> GetWounds<T>(EntityUid parent, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All) where T : IComponent
    {
        var wounds = new List<Entity<WoundComponent, T>>();
        foreach (var wound in GetWounds(parent, damageType, scar, bodyPartType))
        {
            if (!TryComp<T>(wound, out var component))
                continue;

            wounds.Add((wound.Owner, wound.Comp, component));
        }

        return wounds;
    }

    public bool HasWounds(Entity<WoundableComponent?> woundable, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All) =>
        GetWounds(woundable, damageType, scar, bodyPartType).Count > 0;

    public bool HasWounds(Entity<WoundableBodyPartComponent?> woundableBodyPart, string? damageType = null, bool scar = false) =>
        GetWounds(woundableBodyPart, damageType, scar).Count > 0;

    public bool HasWounds(EntityUid parent, string? damageType = null, bool scar = false, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (TryComp<WoundableComponent>(parent, out var woundableComponent))
            return HasWounds((parent, woundableComponent), damageType, scar, bodyPartType);

        if (TryComp<WoundableBodyPartComponent>(parent, out var woundableBodyPartComponent))
            return HasWounds((parent, woundableBodyPartComponent), damageType, scar);

        return false;
    }

    public bool HasWounds(Entity<WoundableComponent?> woundable, DamageSpecifier damage, bool scar = false, BodyPartType bodyPartType = BodyPartType.All)
    {
        foreach (var damageType in damage.DamageDict.Keys)
        {
            if (HasWounds(woundable, damageType, scar, bodyPartType))
                return true;
        }

        return false;
    }

    public bool HasWounds(Entity<WoundableBodyPartComponent?> woundableBodyPart, DamageSpecifier damage, bool scar = false)
    {
        foreach (var damageType in damage.DamageDict.Keys)
        {
            if (HasWounds(woundableBodyPart, damageType, scar))
                return true;
        }

        return false;
    }

    public bool HasWounds(EntityUid parent, DamageSpecifier damage, bool scar = false, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (TryComp<WoundableComponent>(parent, out var woundableComponent))
            return HasWounds((parent, woundableComponent), damage, scar, bodyPartType);

        if (TryComp<WoundableBodyPartComponent>(parent, out var woundableBodyPartComponent))
            return HasWounds((parent, woundableBodyPartComponent), damage, scar);

        return false;
    }

    #endregion
}

