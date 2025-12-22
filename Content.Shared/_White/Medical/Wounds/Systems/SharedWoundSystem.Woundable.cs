using Content.Shared._White.Body.Systems;
using Content.Shared._White.Medical.Wounds.Components.Woundable;
using Content.Shared.Damage;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeWoundable()
    {
        SubscribeLocalEvent<WoundableComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<WoundableComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<WoundableComponent, BeforeDamageCommitEvent>(OnBeforeDamageCommit);
    }

    #region Event Handling

    private void OnBodyPartAdded(Entity<WoundableComponent> woundable, ref BodyPartAddedEvent args)
    {
        if (!TryComp<DamageableComponent>(woundable, out var damageableBodyComponent)
            || !TryComp<DamageableComponent>(args.Part, out var damageableBodyPartComponent)
            || !TryComp<WoundableBodyPartComponent>(args.Part, out var woundableBodyPartComponent))
            return;

        woundableBodyPartComponent.Parent = woundable;

        damageableBodyComponent.Damage.ExclusiveAdd(damageableBodyPartComponent.Damage);
        Damageable.DamageChanged(woundable, damageableBodyComponent, damageableBodyPartComponent.Damage);

        woundable.Comp.Wounds[args.Part.Comp.Type] = woundableBodyPartComponent.Wounds;
    }

    private void OnBodyPartRemoved(Entity<WoundableComponent> woundable, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<DamageableComponent>(woundable, out var damageableBodyComponent)
            || !TryComp<DamageableComponent>(args.Part, out var damageableBodyPartComponent)
            || !TryComp<WoundableBodyPartComponent>(args.Part, out var woundableBodyPartComponent))
            return;

        woundableBodyPartComponent.Parent = null;

        var damageDelta = -damageableBodyPartComponent.Damage;

        damageableBodyComponent.Damage.ExclusiveAdd(damageDelta);
        Damageable.DamageChanged(woundable, damageableBodyComponent, damageDelta);

        woundable.Comp.Wounds.Remove(args.Part.Comp.Type);
    }

    private void OnBeforeDamageCommit(Entity<WoundableComponent> woundable, ref BeforeDamageCommitEvent args)
    {
        if (!TryComp<DamageableComponent>(woundable, out var damageableBodyComponent))
            return;

        var bodyParts = _body.GetBodyParts<WoundableBodyPartComponent>(woundable.Owner, args.BodyPartType);
        var damage = args.Damage / bodyParts.Count;
        var bodyDamage = new DamageSpecifier();

        foreach (var bodyPart in bodyParts)
        {
            var bodyPartDamage = ApplyBodyPartDamage(bodyPart.AsNullable(), damage, woundable.AsNullable(), args.IgnoreResistances);

            foreach (var (damageType, damageValue) in bodyPartDamage.DamageDict)
            {
                if (!bodyDamage.DamageDict.TryAdd(damageType, damageValue))
                    bodyDamage.DamageDict[damageType] += damageValue;
            }
        }

        if (bodyDamage.Empty)
            return;

        damageableBodyComponent.Damage.ApplyDamage(bodyDamage);
        Damageable.DamageChanged(woundable, damageableBodyComponent, bodyDamage);

        RaiseLocalEvent(woundable, new WoundableDamageChangedEvent(bodyDamage));

        args.Damage = bodyDamage;
        args.Handled = true;
    }

    #endregion
}
