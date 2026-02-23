using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Body.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Utility;

namespace Content.Shared._White.Body.Systems;

public abstract partial class SharedBodySystem
{
    private void InitializeAppearance()
    {
        SubscribeLocalEvent<BodyAppearanceComponent, BodyPartAddedEvent>(OnBodyPartAdded);
        SubscribeLocalEvent<BodyAppearanceComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<BodyAppearanceComponent, OrganAddedEvent>(OnOrganAdded);
        SubscribeLocalEvent<BodyAppearanceComponent, OrganRemovedEvent>(OnOrganRemoved);
    }

    #region Event Handling

    private void OnBodyPartAdded(Entity<BodyAppearanceComponent> bodyAppearance, ref BodyPartAddedEvent args)
    {
        if (!TryComp<BodyPartAppearanceComponent>(args.Part, out var bodyPartAppearance))
            return;

        bodyAppearance.Comp.Layers[bodyPartAppearance.Layer] = bodyPartAppearance.LayerInfo;
        Dirty(bodyAppearance);
    }

    protected virtual void OnBodyPartRemoved(Entity<BodyAppearanceComponent> bodyAppearance, ref BodyPartRemovedEvent args)
    {
        if (!TryComp<BodyPartAppearanceComponent>(args.Part, out var bodyPartAppearance))
            return;

        bodyAppearance.Comp.Layers[bodyPartAppearance.Layer] = null;
        Dirty(bodyAppearance);
    }

    private void OnOrganAdded(Entity<BodyAppearanceComponent> bodyAppearance, ref OrganAddedEvent args)
    {
        if (!TryComp<OrganAppearanceComponent>(args.Organ, out var organAppearance))
            return;

        bodyAppearance.Comp.Layers[organAppearance.Layer] = organAppearance.LayerInfo;
        Dirty(bodyAppearance);
    }

    protected virtual void OnOrganRemoved(Entity<BodyAppearanceComponent> bodyAppearance, ref OrganRemovedEvent args)
    {
        if (!TryComp<OrganAppearanceComponent>(args.Organ, out var organAppearance))
            return;

        bodyAppearance.Comp.Layers[organAppearance.Layer] = null;
        Dirty(bodyAppearance);
    }

    #endregion

    #region Private API

    private void SetupBodyPartAppearance(Entity<HumanoidAppearanceComponent> humanoid, Entity<BodyPartComponent?, BodyPartAppearanceComponent?> bodyPart, bool sync = true)
    {
        if (!Resolve(bodyPart, ref bodyPart.Comp1) || !Resolve(bodyPart, ref bodyPart.Comp2, false))
            return;

        bodyPart.Comp2.BodyType = humanoid.Comp.BodyType;
        bodyPart.Comp2.Color = humanoid.Comp.SkinColor;
        bodyPart.Comp2.Sex = humanoid.Comp.Sex;

        if (sync)
            Dirty(bodyPart, bodyPart.Comp2);
    }

    private void SetupOrganAppearance(Entity<HumanoidAppearanceComponent> humanoid, Entity<OrganComponent?, OrganAppearanceComponent?> organ, bool sync = true)
    {
        if (!Resolve(organ, ref organ.Comp1) || !Resolve(organ, ref organ.Comp2, false))
            return;

        switch (organ.Comp1.Type)
        {
            case OrganType.Eyes:
                organ.Comp2.Color = humanoid.Comp.EyeColor;
                break;
            default:
                return;
        }

        if (sync)
            Dirty(organ, organ.Comp2);
    }

    protected bool TryGetMarkingLayer(List<MarkingLayerInfo> markingLayers, string markingId, string state, [NotNullWhen(true)] out MarkingLayerInfo? info)
    {
        info = null;

        foreach (var markingLayer in markingLayers)
        {
            if (markingLayer.MarkingId != markingId || markingLayer.State != state)
                continue;

            info = markingLayer;
            return true;
        }

        return false;
    }

    #endregion

    #region Public API

    public virtual void SetupBodyAppearance(Entity<BodyComponent?, BodyAppearanceComponent?, HumanoidAppearanceComponent?> body, bool sync = true)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !Resolve(body, ref body.Comp3, false))
            return;

        foreach (var bodyPart in GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1)))
        {
            SetupBodyPartAppearance((body, body.Comp3), bodyPart.AsNullable(), sync);
            body.Comp2.Layers[bodyPart.Comp2.Layer] = bodyPart.Comp2.LayerInfo;
        }

        foreach (var organ in GetOrgans<OrganAppearanceComponent>((body, body.Comp1)))
        {
            SetupOrganAppearance((body, body.Comp3), organ.AsNullable(), sync);
            body.Comp2.Layers[organ.Comp2.Layer] = organ.Comp2.LayerInfo;
        }

        foreach (var marking in body.Comp3.MarkingSet.GetForwardEnumerator().ToList())
            AddMarking((body, body.Comp1, body.Comp3), marking);

        if (sync)
            Dirty(body, body.Comp2);
    }

    public virtual void SetBodyPartColor(Entity<BodyPartAppearanceComponent?> bodyPartAppearance, Color color, bool sync = true)
    {
        if (!Resolve(bodyPartAppearance, ref bodyPartAppearance.Comp) || !bodyPartAppearance.Comp.CanChangeColor)
            return;

        bodyPartAppearance.Comp.Color = color;

        if (sync)
            Dirty(bodyPartAppearance);
    }

    public virtual void SetBodyColor(Entity<BodyComponent?, BodyAppearanceComponent?> body, Color color, bool sync = true)
    {
        if (!Resolve(body, ref body.Comp1) || !Resolve(body, ref body.Comp2))
            return;

        foreach (var bodyPart in GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1)))
            SetBodyPartColor((bodyPart, bodyPart.Comp2), color, false);

        if (sync)
            Dirty(body, body.Comp2);
    }

    public virtual void SetOrganColor(Entity<OrganAppearanceComponent?> organAppearance, Color color, bool sync = true)
    {
        if (!Resolve(organAppearance, ref organAppearance.Comp) || !organAppearance.Comp.CanChangeColor)
            return;

        organAppearance.Comp.Color = color;

        if (sync)
            Dirty(organAppearance);
    }

    public virtual void SetBodyOrganColor(Entity<BodyComponent?, BodyAppearanceComponent?> body, Color color, OrganType type = OrganType.None, bool sync = true)
    {
        if (!Resolve(body, ref body.Comp1) || !Resolve(body, ref body.Comp2))
            return;

        foreach (var organ in GetOrgans<OrganAppearanceComponent>((body, body.Comp1), type))
            SetOrganColor((organ, organ.Comp2), color, false);

        if (sync)
            Dirty(body, body.Comp2);
    }

    public virtual void AddMarking(Entity<BodyComponent?, HumanoidAppearanceComponent?> body, Marking marking)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !_marking.TryGetMarking(marking, out var markingPrototype))
            return;

        for (var i = 0; i < markingPrototype.Sprites.Count; i++)
        {
            var markingSprite = new MarkingLayerInfo(markingPrototype.Sprites[i]);

            markingSprite.Color = marking.MarkingColors[i];

            if (markingSprite.Organ != OrganType.None && GetOrgans<OrganAppearanceComponent>((body, body.Comp1), markingSprite.Organ).FirstOrNull() is { } organ)
            {
                organ.Comp2.MarkingsLayers.Add(markingSprite);
                continue;
            }

            if (markingSprite.BodyPart == BodyPartType.None)
                continue;

            if (GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1), markingSprite.BodyPart).FirstOrNull() is not { } bodyPart)
            {
                if (!markingSprite.ReplacementBodyPart.HasValue)
                    continue;

                var bodyPartUid = Spawn(markingSprite.ReplacementBodyPart);

                if (!TryComp<BodyPartComponent>(bodyPartUid, out var bodyPartComponent)
                    || !TryComp<BodyPartAppearanceComponent>(bodyPartUid, out var bodyPartAppearanceComponent)
                    || !TryAttachBodyPart((body, body.Comp1), (bodyPartUid, bodyPartComponent)))
                {
                    QueueDel(bodyPartUid);
                    continue;
                }

                bodyPartAppearanceComponent.IsMarking = true;
                bodyPart = (bodyPartUid, bodyPartComponent, bodyPartAppearanceComponent);
            }

            if (markingSprite.ReplacementBodyPart.HasValue && bodyPart.Comp2.Visible)
                bodyPart.Comp2.Visible = false;

            bodyPart.Comp2.MarkingsLayers.Add(markingSprite);
        }
    }

    public virtual void RemoveMarking(Entity<BodyComponent?, HumanoidAppearanceComponent?> body, Marking marking)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !_marking.TryGetMarking(marking, out var markingPrototype))
            return;

        foreach (var markingSprite in markingPrototype.Sprites)
        {
            if (markingSprite.Organ != OrganType.None && GetOrgans<OrganAppearanceComponent>((body, body.Comp1), markingSprite.Organ).FirstOrNull() is { } organ)
            {
                organ.Comp2.MarkingsLayers.Remove(markingSprite);
                continue;
            }

            if (markingSprite.BodyPart == BodyPartType.None || GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1), markingSprite.BodyPart).FirstOrNull() is not { } bodyPart)
                continue;

            bodyPart.Comp2.MarkingsLayers.Remove(markingSprite);

            if (markingSprite.ReplacementBodyPart.HasValue && !bodyPart.Comp2.Visible)
                bodyPart.Comp2.Visible = true;

            if (!markingSprite.ReplacementBodyPart.HasValue || !bodyPart.Comp2.IsMarking)
                continue;

            QueueDel(bodyPart);
        }
    }

    public virtual void UpdateMarking(Entity<BodyComponent?, HumanoidAppearanceComponent?> body, Marking marking)
    {
        if (!Resolve(body, ref body.Comp1, false)
            || !Resolve(body, ref body.Comp2, false)
            || !_marking.TryGetMarking(marking, out var markingPrototype))
            return;

        for (var i = 0; i < markingPrototype.Sprites.Count; i++)
        {
            var prototypeMarkingSprite = new MarkingLayerInfo(markingPrototype.Sprites[i]);

            List<MarkingLayerInfo> markingLayers;
            if (GetBodyParts<BodyPartAppearanceComponent>((body, body.Comp1), prototypeMarkingSprite.BodyPart).FirstOrNull() is { } bodyPart)
                markingLayers = bodyPart.Comp2.MarkingsLayers;
            else if (GetOrgans<OrganAppearanceComponent>((body, body.Comp1), prototypeMarkingSprite.Organ).FirstOrNull() is { } organ)
                markingLayers = organ.Comp2.MarkingsLayers;
            else
                continue;

            if (!TryGetMarkingLayer(markingLayers, prototypeMarkingSprite.MarkingId, prototypeMarkingSprite.State, out var markingSprite))
                continue;

            markingSprite.Color = marking.MarkingColors[i];
        }
    }

    #endregion
}
