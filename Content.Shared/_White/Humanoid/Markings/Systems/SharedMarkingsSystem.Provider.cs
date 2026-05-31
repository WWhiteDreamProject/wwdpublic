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
        if (!args.Args.MarkingsSet.TryGetValue(ent.Comp.Data.Category, out var markingSet))
            return;

        SetMarkings(ent.AsNullable(), markingSet);
    }

    private void OnGetMarkingsData(Entity<MarkingsProviderComponent> ent, ref BodyRelayedEvent<GetMarkingsDataEvent> args)
    {
        args.Args.Data.Add(ent.Comp.Data.Category, ent.Comp.Data);

        if (args.Args.Filter is null)
        {
            args.Args.Set.TryAdd(ent.Comp.Data.Category, ent.Comp.Markings);
            return;
        }

        var markings = new List<Marking>();
        foreach (var marking in ent.Comp.Markings)
        {
            if (!args.Args.Filter.Contains(marking.Layer))
                continue;

            markings.Add(marking);
        }

        args.Args.Set.TryAdd(ent.Comp.Data.Category, markings);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Attempts to retrieve the <see cref="MarkingsData"/> associated with a given body provider prototype ID.
    /// </summary>
    /// <param name="prototype">The <see cref="EntProtoId"/> of the body provider prototype to look up.</param>
    /// <param name="data">The appearance data for the body provider if it exists.</param>
    /// <returns>True if the provided entity prototype ID corresponded to a valid provider with marking data that could be returned, false otherwise.</returns>
    public bool TryGetData(EntProtoId prototype, [NotNullWhen(true)] out MarkingsData? data)
    {
        data = null;

        if (!_prototype.TryIndex(prototype, out var provider))
            return false;

        if (!provider.TryGetComponent<MarkingsProviderComponent>(out var comp, _componentFactory))
            return false;

        data = comp.Data;
        return true;
    }

    public IEnumerable<Marking> GetMarkings(Entity<MarkingsProviderComponent?> ent)
    {
        if (!ProviderQuery.Resolve(ent, ref ent.Comp))
            yield break;

        foreach (var marking in ent.Comp.Markings)
        {
            yield return marking;
        }

        if (!CensorNudity)
            yield break;

        var group = _prototype.Index(ent.Comp.Data.Group);
        if (!group.Limits.TryGetValue(ent.Comp.Data.Category, out var limits))
            yield break;

        if (limits.NudityDefault.Count < 1)
            yield break;

        foreach (var markingId in limits.NudityDefault)
        {
            if (!Marking.TryGetMarking(markingId, out var prototype))
                continue;

            foreach (var data in prototype.Markings)
            {
                yield return new(data.Layer, markingId, data.Sprite);
            }
        }
    }

    public virtual void SetMarkings(Entity<MarkingsProviderComponent?> ent, List<Marking> markings)
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
