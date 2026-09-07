using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Systems;

public abstract partial class SharedMarkingsSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyProviderGotRemovedEvent>(OnGotRemoved);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyRelayedEvent<ApplyMarkingsEvent>>(OnApplyMarkings);
        SubscribeLocalEvent<MarkingsProviderComponent, BodyRelayedEvent<GetMarkingsDataEvent>>(OnGetMarkingsData);
    }

    #region Event Handling

    protected virtual void OnGotInserted(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotInsertedEvent args)
    {
        ent.Comp.Body = args.Body;
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Body));
    }

    protected virtual void OnGotRemoved(Entity<MarkingsProviderComponent> ent, ref BodyProviderGotRemovedEvent args)
    {
        ent.Comp.Body = null;
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Body));
    }

    private void OnApplyMarkings(Entity<MarkingsProviderComponent> ent, ref BodyRelayedEvent<ApplyMarkingsEvent> args)
    {
        var markings = new Dictionary<Enum, List<Marking>>();
        foreach (var marking in args.Args.Markings)
        {
            if (marking.OverrideAppearance)
                continue;

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

    public virtual void SetMarkings(Entity<MarkingsProviderComponent?> ent, Dictionary<Enum, List<Marking>> markings)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Markings == markings)
            return;

        ent.Comp.Markings = markings;
        DirtyField(ent, ent.Comp, nameof(MarkingsProviderComponent.Markings));
    }

    #endregion
}
