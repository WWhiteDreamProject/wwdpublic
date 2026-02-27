using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Threshold;
using Content.Shared._White.Wounds.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    #region Public API

    /// <summary>
    /// Creates a wound on this body provider.
    /// </summary>
    public bool TryCreateWound(
        Entity<WoundableProviderComponent?> ent,
        EntProtoId woundId,
        FixedPoint2 damage,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        EntityUid? origin = null
    )
    {
        wound = null;

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!TrySpawnInContainer(woundId, ent, WoundsContainerId, out var woundUid))
        {
            _sawmill.Error($"Couldn't insert wound '{woundId}' to {ent}");
            return false;
        }

        if (!_woundQuery.TryComp(woundUid, out var woundComponent))
        {
            _sawmill.Error($"Wound {ToPrettyString(woundUid)} does not have {typeof(WoundComponent)}");
            Del(woundUid);
            return false;
        }

        woundComponent.Body = ent.Comp.Body;
        DirtyField(woundUid.Value, woundComponent, nameof(WoundComponent.Body));

        woundComponent.Parent = ent;
        DirtyField(woundUid.Value, woundComponent, nameof(WoundComponent.Parent));

        woundComponent.WoundedAt = _gameTiming.CurTime;
        DirtyField(woundUid.Value, woundComponent, nameof(WoundComponent.WoundedAt));

        ChangeDamage((woundUid.Value, woundComponent), damage, origin);

        if (_woundableQuery.TryComp(ent.Comp.Body, out var woundableComp))
        {
            woundableComp.Wounds.Add(woundUid.Value);
            Dirty(ent.Comp.Body.Value, woundableComp);
        }

        wound = (woundUid.Value, woundComponent);

        return true;
    }

    #region TryGetWound

    /// <summary>
    /// Trying to get the wound for this body.
    /// </summary>
    public bool TryGetWound(
        Entity<WoundableComponent?> ent,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        wound = null;

        if (!_woundableQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var providerWound in ent.Comp.Wounds)
        {
            if (!_woundQuery.TryComp(providerWound, out var woundComp)
                || !string.IsNullOrEmpty(type) && woundComp.Type != type)
                continue;

            wound = (providerWound, woundComp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Trying to get the wound for this body provider.
    /// </summary>
    public bool TryGetWound(
        Entity<WoundableProviderComponent?> ent,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        wound = null;

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var providerWound in ent.Comp.Wounds)
        {
            if (!_woundQuery.TryComp(providerWound, out var woundComp)
                || !string.IsNullOrEmpty(type) && woundComp.Type != type)
                continue;

            wound = (providerWound, woundComp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Trying to get the wound for this entity.
    /// </summary>
    public bool TryGetWound(
        EntityUid parent,
        [NotNullWhen(true)] out Entity<WoundComponent>? wound,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        if (_woundableQuery.TryComp(parent, out var woundableComp))
            return TryGetWound((parent, woundableComp), out wound, type);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return TryGetWound((parent, providerComp), out wound, type);

        wound = null;
        return false;
    }

    #endregion

    #region GetWounds

    /// <summary>
    /// Gets the wounds of this body.
    /// </summary>
    public List<Entity<WoundComponent>> GetWounds(
        Entity<WoundableComponent?> ent,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        var wounds = new List<Entity<WoundComponent>>();

        if (!_woundableQuery.Resolve(ent, ref ent.Comp))
            return wounds;

        foreach (var wound in ent.Comp.Wounds)
        {
            if (!_woundQuery.TryComp(wound, out var woundComp)
                || !string.IsNullOrEmpty(type) && woundComp.Type != type)
                continue;

            wounds.Add((wound, woundComp));
        }

        return wounds;
    }

    /// <summary>
    /// Gets the wounds of this body provider.
    /// </summary>
    public List<Entity<WoundComponent>> GetWounds(
        Entity<WoundableProviderComponent?> ent,
        ProtoId<DamageTypePrototype>? type = null
    )
    {
        var wounds = new List<Entity<WoundComponent>>();

        if (!_providerQuery.Resolve(ent, ref ent.Comp))
            return wounds;

        foreach (var wound in ent.Comp.Wounds)
        {
            if (!_woundQuery.TryComp(wound, out var woundComp)
                || !string.IsNullOrEmpty(type) && woundComp.Type != type)
                continue;

            wounds.Add((wound, woundComp));
        }

        return wounds;
    }

    /// <summary>
    /// Gets the wounds of this body entity.
    /// </summary>
    public List<Entity<WoundComponent>> GetWounds(EntityUid parent, ProtoId<DamageTypePrototype>? type = null)
    {
        if (_woundableQuery.TryComp(parent, out var woundableComp))
            return GetWounds((parent, woundableComp), type);

        if (_providerQuery.TryComp(parent, out var providerComp))
            return GetWounds((parent, providerComp), type);

        return new List<Entity<WoundComponent>>();
    }

    #endregion

    /// <summary>
    /// Applies damage to the wound.
    /// </summary>
    /// <returns>
    /// Returns the damage dealt, subject to limitations.
    /// </returns>
    public FixedPoint2 ChangeDamage(Entity<WoundComponent?> ent, FixedPoint2 damage, EntityUid? origin = null)
    {
        if (!_woundQuery.Resolve(ent, ref ent.Comp))
            return FixedPoint2.Zero;

        var newDamage = FixedPoint2.Max(ent.Comp.Damage + damage, FixedPoint2.Zero);
        var damageDelta = newDamage - ent.Comp.Damage;

        if (damageDelta == FixedPoint2.Zero)
            return FixedPoint2.Zero;

        if (damageDelta > FixedPoint2.Zero)
        {
            ent.Comp.WoundedAt = _gameTiming.CurTime;
            DirtyField(ent, ent.Comp, nameof(WoundComponent.WoundedAt));
        }

        ent.Comp.Damage = newDamage;
        DirtyField(ent, ent.Comp, nameof(WoundComponent.Damage));

        UpdateWoundSeverity((ent, ent.Comp));

        var ev = new WoundDamageChangedEvent(origin, damageDelta, ent.Comp);
        RaiseLocalEvent(ent, ev);

        return damageDelta;
    }

    #endregion

    #region Private API

    private bool TryProcessWound(
        Entity<WoundableProviderComponent?> ent,
        ProtoId<DamageTypePrototype> type,
        FixedPoint2 damage,
        EntityUid? origin,
        out FixedPoint2 processedDamage
    )
    {
        processedDamage = FixedPoint2.Zero;
        if (damage == FixedPoint2.Zero)
            return false;

        if (TryGetWound(ent, out var wound, type))
        {
            processedDamage = ChangeDamage(wound.Value.AsNullable(), damage, origin);
            return true;
        }

        if (damage < FixedPoint2.Zero
            || !_prototype.TryIndex(type, out var typePrototype)
            || typePrototype.Wound is not {} woundId
            || !TryCreateWound(ent, woundId, damage, out _, origin))
            return false;

        processedDamage = damage;
        return true;
    }

    private void UpdateWoundSeverity(Entity<WoundComponent> ent)
    {
        var severity = ent.Comp.Thresholds.HighestMatch(ent.Comp.Damage) ?? WoundSeverity.Healthy;
        if (severity == ent.Comp.WoundSeverity)
            return;

        ent.Comp.WoundSeverity = severity;
        DirtyField(ent, ent.Comp, nameof(WoundComponent.WoundSeverity));

        RaiseLocalEvent(ent, new WoundSeverityChangedEvent(severity));
    }

    #endregion
}
