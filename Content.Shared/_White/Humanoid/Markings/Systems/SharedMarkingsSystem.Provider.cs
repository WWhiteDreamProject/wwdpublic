using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Appearance.Systems;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Managers;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Systems;

public abstract partial class SharedMarkingsSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<MarkingsProviderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotInsertedIntoAncestorsEvent>(OnGotInsertedIntoAncestors);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotRemovedFromAncestorsEvent>(OnGotRemovedFromAncestors);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyRelayedEvent<ApplyMarkingsEvent>>(OnApplyMarkings);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyRelayedEvent<GetMarkingsDataEvent>>(OnGetMarkingsData);
        SubscribeLocalEvent<MarkingsProviderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MarkingsProviderComponent, GetBodyAppearanceLayersEvent>(OnGetBodyAppearanceLayers);
        SubscribeLocalEvent<MarkingsProviderComponent, MapInitEvent>(OnMapInit);
    }

    #region Event Handling

    private void OnAfterAutoHandleState(Entity<MarkingsProviderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateMarkings(ent);
    }

    private void OnGotInserted(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        if (!MarkingsQuery.HasComp(args.Body))
            return;

        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Body));

        ent.Comp.Entities.Add(args.Body.Owner);
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));

        ApplyMarkings(ent, args.Body.Owner);
    }

    private void OnGotInsertedIntoAncestors(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotInsertedIntoAncestorsEvent args)
    {
        foreach (var ancestor in args.Ancestors)
        {
            if (!ProviderQuery.HasComp(ancestor))
                continue;

            ent.Comp.Entities.Add(ancestor);
            ApplyMarkings(ent, ancestor);
        }

        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));
    }

    private void OnGotInsertedIntoParent(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        if (!ProviderQuery.HasComp(args.Parent))
            return;

        ent.Comp.Entities.Add(args.Parent.Owner);
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));

        ApplyMarkings(ent, args.Parent.Owner);
    }

    private void OnGotRemoved(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (!MarkingsQuery.HasComp(args.Body))
            return;

        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Body));

        ent.Comp.Entities.Remove(args.Body.Owner);
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));

        RemoveMarkings(ent, args.Body.Owner);
    }

    private void OnGotRemovedFromParent(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        if (!ProviderQuery.HasComp(args.Parent))
            return;

        ent.Comp.Entities.Remove(args.Parent.Owner);
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));

        RemoveMarkings(ent, args.Parent.Owner);
    }

    private void OnGotRemovedFromAncestors(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotRemovedFromAncestorsEvent args)
    {
        foreach (var ancestor in args.Ancestors)
        {
            if (!ProviderQuery.HasComp(ancestor))
                continue;

            ent.Comp.Entities.Remove(ancestor);
            RemoveMarkings(ent, ancestor);
        }

        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));
    }

    private void OnApplyMarkings(Entity<MarkingsProviderComponent> ent, ref BodyRelayedEvent<ApplyMarkingsEvent> args)
    {
        var markings = new Dictionary<Enum, List<Marking>>();
        foreach (var marking in args.Args.Markings)
        {
            if (!ent.Comp.Data.Layers.Contains(marking.Layer))
                continue;

            if (markings.TryGetValue(marking.Layer, out var layerMarkings))
            {
                layerMarkings.Add(marking);
                continue;
            }

            markings.Add(marking.Layer, new() {marking});
        }

        SetMarkings(ent.AsNullable(), markings);
    }

    private void OnGetMarkingsData(Entity<MarkingsProviderComponent> ent, ref BodyRelayedEvent<GetMarkingsDataEvent> args)
    {
        foreach (var layer in ent.Comp.Data.Layers)
        {
            args.Args.Data.Add(layer, ent.Comp.Data);

            if (!ent.Comp.Markings.TryGetValue(layer, out var layerMarkings))
                continue;

            args.Args.Markings.Add(layer, layerMarkings);
        }
    }

    private void OnInit(Entity<MarkingsProviderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Entities.Add(ent);
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Entities));
    }

    private void OnGetBodyAppearanceLayers(Entity<MarkingsProviderComponent> ent, ref GetBodyAppearanceLayersEvent args)
    {
        foreach (var layer in ent.Comp.Data.Layers)
        {
            args.Layers.Add(layer);
        }
    }

    private void OnMapInit(Entity<MarkingsProviderComponent> ent, ref MapInitEvent args)
    {
        if (!_bodyAppearance.TryGetProviderAppearance(ent.Owner, out var appearance))
            return;

        foreach (var layer in ent.Comp.Data.Layers)
        {
            var markings = new List<Marking>();

            Marking.EnsureValidColors(markings, ent.Comp.Data.Group, appearance.Value.Colors);
            Marking.EnsureValidDefault(markings, layer, ent.Comp.Data.Group, appearance.Value.Colors);
            Marking.EnsureValidGroupAndSex(markings, ent.Comp.Data.Group, appearance.Value.Sex);
            Marking.EnsureValidLimits(markings, ent.Comp.Data.Group, appearance.Value.Colors);

            ent.Comp.Markings[layer] = markings;
        }

        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Markings));

        UpdateMarkings(ent);
        ApplyMarkings(ent, ent);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to retrieve the <see cref="MarkingData"/> and layers associated with a given body provider prototype ID.
    /// </summary>
    /// <param name="prototype">The <see cref="EntProtoId"/> of the body provider prototype to look up.</param>
    /// <param name="data">The appearance data for the body provider if it exists.</param>
    /// <returns>True if the provided entity prototype ID corresponded to a valid provider with marking data and layers that could be returned, false otherwise.</returns>
    public bool TryGetData(EntProtoId prototype, [NotNullWhen(true)] out MarkingData? data)
    {
        data = null;

        if (!Prototype.TryIndex(prototype, out var provider))
            return false;

        if (!provider.TryGetComponent<MarkingsProviderComponent>(out var comp, _componentFactory))
            return false;

        data = comp.Data;
        return true;
    }

    /// <summary>
    /// Sets the markings for a markings provider and updates the visual appearance accordingly.
    /// </summary>
    /// <param name="ent">The entity whose markings should be updated.</param>
    /// <param name="markings">A dictionary mapping layers to their associated marking lists.</param>
    public void SetMarkings(Entity<MarkingsProviderComponent?> ent, Dictionary<Enum, List<Marking>> markings)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (MarkingManager.MarkingsEqual(ent.Comp.Markings, markings))
            return;

        ent.Comp.Markings = markings;
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Markings));

        UpdateMarkings((ent, ent.Comp));
    }

    #endregion

    #region Private API

    protected virtual void ApplyMarkings(Entity<MarkingsProviderComponent> ent, EntityUid target) { }

    protected virtual void RemoveMarkings(Entity<MarkingsProviderComponent> ent, EntityUid target) { }

    protected virtual void UpdateMarkings(Entity<MarkingsProviderComponent> ent) { }

    #endregion
}
