using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Wounds.Components.Woundable;
using Content.Shared._White.Threshold;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeBone()
    {
        SubscribeLocalEvent<WoundableBoneComponent, RejuvenateEvent>(OnBoneRejuvenate);
    }

    #region Event Handling

    private void OnBoneRejuvenate(Entity<WoundableBoneComponent> woundableBone, ref RejuvenateEvent args)
    {
        woundableBone.Comp.Health = woundableBone.Comp.MaximumHealth;
        CheckBoneStatusThreshold((woundableBone, null, woundableBone));
    }

    #endregion

    #region Private API

    private void ApplyBoneDamage(Entity<BoneComponent?, WoundableBoneComponent?> bone, DamageSpecifier damage)
    {
        if (!Resolve(bone, ref bone.Comp1, ref bone.Comp2))
            return;

        var totalDamageDelta = FixedPoint2.Zero;
        foreach (var (damageType, damageValue) in damage.DamageDict)
        {
            if (damageValue <= 0 || !bone.Comp2.SupportedDamageType.Contains(damageType))
                continue;

            var damageDelta = FixedPoint2.Max(damageValue - bone.Comp2.CurrentStrength, FixedPoint2.Zero);
            if (damageDelta == FixedPoint2.Zero)
                continue;

            totalDamageDelta += damageDelta;
        }

        if (totalDamageDelta  == FixedPoint2.Zero)
            return;

        bone.Comp2.Health = FixedPoint2.Max(bone.Comp2.Health - totalDamageDelta, FixedPoint2.Zero);
        DirtyField(bone, bone.Comp2, nameof(WoundableBoneComponent.Health));

        RaiseLocalEvent(bone, new BoneHealthChangedEvent(totalDamageDelta));

        CheckBoneStatusThreshold(bone);
    }

    private void CheckBoneStatusThreshold(Entity<BoneComponent?, WoundableBoneComponent?> bone)
    {
        if (!Resolve(bone, ref bone.Comp1, ref bone.Comp2))
            return;

        var boneStatus = bone.Comp2.BoneStatusThresholds.LowestMatch(bone.Comp2.Health) ?? BoneStatus.Whole;
        if (bone.Comp2.CurrentBoneStatusThreshold == boneStatus)
            return;

        bone.Comp2.CurrentBoneStatusThreshold = boneStatus;
        DirtyField(bone, bone.Comp2, nameof(WoundableBoneComponent.CurrentBoneStatusThreshold));

        var ev = new BoneStatusChangedEvent((bone, bone.Comp2), boneStatus);
        RaiseLocalEvent(bone, ev);

        if (bone.Comp1.Body.HasValue)
            RaiseLocalEvent(bone.Comp1.Body.Value, ev);

        if (bone.Comp1.Parent.HasValue)
            RaiseLocalEvent(bone.Comp1.Parent.Value, ev);
    }

    #endregion
}
