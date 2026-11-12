using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Appearance.Components;
using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Coloring;
using Content.Shared._White.Humanoid.Markings.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Appearance.Systems;

public abstract partial class SharedBodyAppearanceSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotInsertedIntoAncestorsEvent>(OnGotInsertedIntoAncestors);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotInsertedIntoParentEvent>(OnGotInsertedIntoParent);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotRemovedFromAncestorsEvent>(OnGotRemovedFromAncestors);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyProviderGotRemovedFromParentEvent>(OnGotRemovedFromParent);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyRelayedEvent<ApplyBodyAppearanceDataEvent>>(OnApplyBodyAppearanceData);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyRelayedEvent<ApplyMarkingsEvent>>(OnApplyMarkings);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, BodyRelayedEvent<GetBodyAppearanceDataEvent>>(OnGetBodyAppearanceData);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<BodyAppearanceProviderComponent, ComponentHandleState>(OnHandleState);
    }

    #region Event Handling

    private void OnGotInserted(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        if (!BodyQuery.HasComp(args.Body))
            return;

        ent.Comp.Entities.Add(args.Body.Owner);
        Dirty(ent);

        ApplyAppearance(ent, args.Body.Owner);
    }

    private void OnGotInsertedIntoAncestors(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotInsertedIntoAncestorsEvent args)
    {
        foreach (var ancestor in args.Ancestors)
        {
            if (!ProviderQuery.HasComp(ancestor))
                continue;

            ent.Comp.Entities.Add(ancestor);
            ApplyAppearance(ent, ancestor);
        }

        Dirty(ent);
    }

    private void OnGotInsertedIntoParent(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotInsertedIntoParentEvent args)
    {
        if (!ProviderQuery.HasComp(args.Parent))
            return;

        ent.Comp.Entities.Add(args.Parent.Owner);
        Dirty(ent);

        ApplyAppearance(ent, args.Parent.Owner);
    }

    private void OnGotRemoved(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        if (!BodyQuery.HasComp(args.Body))
            return;

        ent.Comp.Entities.Remove(args.Body.Owner);
        Dirty(ent);

        RemoveAppearance(ent, args.Body.Owner);
    }

    private void OnGotRemovedFromAncestors(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedFromAncestorsEvent args)
    {
        foreach (var ancestor in args.Ancestors)
        {
            if (!ProviderQuery.HasComp(ancestor))
                continue;

            ent.Comp.Entities.Remove(ancestor);
            RemoveAppearance(ent, ancestor);
        }

        Dirty(ent);
    }

    private void OnGotRemovedFromParent(Entity<BodyAppearanceProviderComponent> ent, ref BodyProviderGotRemovedFromParentEvent args)
    {
        if (!ProviderQuery.HasComp(args.Parent))
            return;

        ent.Comp.Entities.Remove(args.Parent.Owner);
        Dirty(ent);

        RemoveAppearance(ent, args.Parent.Owner);
    }

    private void OnApplyBodyAppearanceData(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<ApplyBodyAppearanceDataEvent> args)
    {
        var relevantData = args.Args.Data;
        if (args.Args.SpecifiedData?.TryGetValue(ent.Comp.Layer, out var specifiedData) == true)
            relevantData = specifiedData;

        if (relevantData is not { } data)
            return;

        SetAppearance(ent.AsNullable(), data);
    }

    private void OnApplyMarkings(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<ApplyMarkingsEvent> args)
    {
        var entries = new List<BodyAppearanceProviderEntry>();
        foreach (var marking in args.Args.Markings)
        {
            if (!_marking.TryGetMarking(marking, out var prototype))
                continue;

            if (!prototype.Appearance)
                continue;

            if (!Equals(marking.Layer, ent.Comp.Layer))
                continue;

            if (marking.Sprite is not SpriteSpecifier.Rsi specifier)
                continue;

            var group = new ProtoId<BodyColorGroupPrototype>("None");
            var coloring = _marking.GetMarkingColoring(prototype, marking.Index);
            if (coloring.Type is BodyColorGroupColoring groupColoring)
                group = groupColoring.Group;

            var entry = new BodyAppearanceProviderEntry
            {
                Group = group,
                Color = marking.Color,
                Index = marking.Index,
                Path = specifier.RsiPath.ToString(),
                State = specifier.RsiState
            };
            entries.Add(entry);
        }

        if (entries.Count == 0)
            return;

        SetEntries(ent.AsNullable(), entries);
    }

    private void OnGetBodyAppearanceData(Entity<BodyAppearanceProviderComponent> ent, ref BodyRelayedEvent<GetBodyAppearanceDataEvent> args)
    {
        var ev = new GetBodyAppearanceLayersEvent();
        ev.Layers.Add(ent.Comp.Layer);
        RaiseLocalEvent(ent, ref ev);

        foreach (var layer in ev.Layers)
        {
            args.Args.Data.Add(layer, ent.Comp.Appearance);
        }
    }

    private void OnGetState(Entity<BodyAppearanceProviderComponent> ent, ref ComponentGetState args)
    {
        args.State = new BodyAppearanceProviderComponentState(ent.Comp, EntityManager);
    }

    private void OnHandleState(Entity<BodyAppearanceProviderComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not BodyAppearanceProviderComponentState state)
            return;

        ent.Comp.Appearance = state.Appearance;
        ent.Comp.Entities = GetEntitySet(state.Entities);

        ClearAppearance(ent);
        ent.Comp.Entries = state.Entries;
        UpdateAppearance(ent);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gathers the body appearance data from this entity.
    /// </summary>
    /// <param name="ent">The entity to sample.</param>
    /// <param name="appearance">Returned body appearance data.</param>
    /// <returns>True if the appearance data is successfully returned, false otherwise.</returns>
    public bool TryGetProviderAppearance(Entity<BodyAppearanceProviderComponent?> ent, [NotNullWhen(true)] out BodyAppearanceData? appearance)
    {
        appearance = null;
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return false;

        appearance = ent.Comp.Appearance;
        return true;
    }

    /// <summary>
    /// Sets the appearance data for a body appearance provider and updates all entries with corresponding color groups.
    /// </summary>
    /// <param name="ent">The entity whose appearance data should be updated.</param>
    /// <param name="appearance">The appearance data containing color groups and other visual characteristics.</param>
    public void SetAppearance(Entity<BodyAppearanceProviderComponent?> ent, BodyAppearanceData appearance)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Appearance == appearance)
            return;

        for (var i = 0; i < ent.Comp.Entries.Count; i++)
        {
            var entry = ent.Comp.Entries[i];
            if (!appearance.Colors.TryGetValue(entry.Group, out var color))
                continue;

            SetColor(ent, i, color);
        }

        ent.Comp.Appearance = appearance;
        Dirty(ent);

        UpdateAppearance((ent, ent.Comp));
    }

    /// <summary>
    /// Sets the color for a specific provider entry at given index.
    /// </summary>
    /// <param name="ent">The entity whose entry color should be updated.</param>
    /// <param name="index">The index of the entry.</param>
    /// <param name="color">The new color to apply.</param>
    public void SetColor(Entity<BodyAppearanceProviderComponent?> ent, int index, Color color)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Entries.TryGetValue(index, out var entry))
            return;

        if (entry.Color == color)
            return;

        ent.Comp.Entries[index] = entry.WithColor(color);
        Dirty(ent);

        UpdateAppearance((ent, ent.Comp));
    }

    public void SetEntries(Entity<BodyAppearanceProviderComponent?> ent, List<BodyAppearanceProviderEntry> entries)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Entries == entries)
            return;

        ClearAppearance((ent, ent.Comp));

        ent.Comp.Entries = entries;
        Dirty(ent);

        UpdateAppearance((ent, ent.Comp));
    }

    /// <summary>
    /// Sets the sprite path for a specific provider entry at given index.
    /// </summary>
    /// <param name="ent">The entity whose entry sprite path should be updated.</param>
    /// <param name="index">The index of the entry.</param>
    /// <param name="path">The new sprite path to apply.</param>
    public void SetPath(Entity<BodyAppearanceProviderComponent?> ent, int index, string path)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Entries.TryGetValue(index, out var entry))
            return;

        if (entry.Path == path)
            return;

        ent.Comp.Entries[index] = entry.WithPath(path);
        Dirty(ent);

        UpdateAppearance((ent, ent.Comp));
    }

    #endregion

    #region Private API

    private void ClearAppearance(Entity<BodyAppearanceProviderComponent> ent)
    {
        foreach (var entity in ent.Comp.Entities)
        {
            RemoveAppearance(ent, entity);
        }

        RemoveAppearance(ent);
    }

    private void UpdateAppearance(Entity<BodyAppearanceProviderComponent> ent)
    {
        foreach (var entity in ent.Comp.Entities)
        {
            ApplyAppearance(ent, entity);
        }

        ApplyAppearance(ent);
    }

    protected virtual void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent) { }

    protected virtual void ApplyAppearance(Entity<BodyAppearanceProviderComponent> ent, EntityUid target) { }

    protected virtual void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent) { }

    protected virtual void RemoveAppearance(Entity<BodyAppearanceProviderComponent> ent, EntityUid target) { }

    #endregion
}
