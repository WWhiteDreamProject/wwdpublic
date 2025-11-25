using Content.Shared._White.Body.Components;
using Content.Shared._White.Medical.Wounds.Components.Woundable;
using Content.Shared.Damage;
using Robust.Shared.Containers;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem
{
    private void InitializeBodyPart()
    {
        SubscribeLocalEvent<WoundableBodyPartComponent, ComponentInit>(OnBodyPartInit);
    }

    #region Event Handling

    private void OnBodyPartInit(Entity<WoundableBodyPartComponent> woundableBodyPart, ref ComponentInit args) =>
        woundableBodyPart.Comp.Container = _container.EnsureContainer<Container>(woundableBodyPart.Owner, WoundsContainerId);

    #endregion

    #region Public API

    public List<Entity<WoundableBodyPartComponent>> GetWoundableBodyParts(Entity<WoundableComponent?> woundable, string? damageType = null, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (!Resolve(woundable, ref woundable.Comp))
            return new List<Entity<WoundableBodyPartComponent>>();

        var woundableBodyParts = new List<Entity<WoundableBodyPartComponent>>();
        foreach (var bodyPart in _body.GetBodyParts<WoundableBodyPartComponent>(woundable.Owner, bodyPartType))
        {
            if (!HasWounds((bodyPart, bodyPart.Comp2), damageType))
                continue;

            woundableBodyParts.Add((bodyPart, bodyPart.Comp2));
        }

        return woundableBodyParts;
    }

    public List<Entity<BodyPartComponent, WoundableBodyPartComponent>> GetWoundableBodyParts(Entity<WoundableComponent?> woundable, DamageSpecifier damage, BodyPartType bodyPartType = BodyPartType.All)
    {
        if (!Resolve(woundable, ref woundable.Comp))
            return new List<Entity<BodyPartComponent, WoundableBodyPartComponent>>();

        var bodyParts = new List<Entity<BodyPartComponent, WoundableBodyPartComponent>>();
        foreach (var bodyPart in _body.GetBodyParts<WoundableBodyPartComponent>(woundable.Owner, bodyPartType))
        {
            if (!HasWounds((bodyPart, bodyPart.Comp2), damage))
                continue;

            bodyParts.Add(bodyPart);
        }

        return bodyParts;
    }

    #endregion
}
