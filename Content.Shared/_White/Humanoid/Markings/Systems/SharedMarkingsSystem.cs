using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Systems;

public abstract partial class SharedMarkingsSystem : EntitySystem
{
    [Dependency] protected new readonly IPrototypeManager Prototype = default!;
    [Dependency] protected readonly MarkingManager Marking = default!;

    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;

    protected EntityQuery<MarkingsProviderComponent> ProviderQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeProvider();

        ProviderQuery = GetEntityQuery<MarkingsProviderComponent>();
    }

    #region Public API

    /// <summary>
    /// Gathers all the markings-relevant data from this entity.
    /// </summary>
    /// <param name="uid">The entity to sample.</param>
    /// <param name="markings">The markings that are applied to the entity.</param>
    /// <param name="data">The marking data of providers.</param>
    /// <returns>True if the markings data is successfully returned, false otherwise.</returns>
    public bool TryGetData(
        EntityUid uid,
        out Dictionary<Enum, List<Marking>> markings,
        out Dictionary<Enum, MarkingData> data
    )
    {
        var ev = new GetMarkingsDataEvent();
        RaiseLocalEvent(uid, ref ev);

        markings = ev.Markings;
        data = ev.Data;

        return markings.Count > 0 || data.Count > 0;
    }

    /// <summary>
    /// Looks up the expected set of <see cref="MarkingData" /> for the species to have.
    /// </summary>
    /// <param name="species">The species to look up the usual markings of.</param>
    /// <returns>A dictionary of marking categories to their usual marking data within a species.</returns>
    public Dictionary<Enum, MarkingData> GetMarkingData(ProtoId<SpeciesPrototype> species)
    {
        var markingsData = new Dictionary<Enum, MarkingData>();

        var speciesPrototype = Prototype.Index(species);
        var dollPrototype = Prototype.Index(speciesPrototype.Doll);

        if (!dollPrototype.TryGetComponent<BodyComponent>(out var body, _componentFactory))
            return markingsData;

        foreach (var prototype in _body.GetProviders(body))
        {
            if (!TryGetData(prototype, out var data))
                continue;

            foreach (var layer in data.Value.Layers)
            {
                markingsData[layer] = data.Value;
            }
        }

        return markingsData;
    }

    /// <summary>
    /// Applies the given set of markings to the entity.
    /// </summary>
    /// <param name="uid">The entity whose apply markings.</param>
    /// <param name="markings">A list of markings.</param>
    public void ApplyMarkings(EntityUid uid, List<Marking> markings)
    {
        var ev = new ApplyMarkingsEvent(markings);
        RaiseLocalEvent(uid, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on body entity when a profile is being applied to it.
/// </summary>
[ByRefEvent]
public readonly record struct ApplyMarkingsEvent(List<Marking> Markings);

/// <summary>
/// Event raised on an entity to get the markings on its provider.
/// </summary>
[ByRefEvent]
public readonly record struct GetMarkingsDataEvent()
{
    /// <summary>
    /// A result contained the markings.
    /// </summary>
    public readonly Dictionary<Enum, List<Marking>> Markings = new();

    /// <summary>
    /// A result contained the marking data.
    /// </summary>
    public readonly Dictionary<Enum, MarkingData> Data = new();
}
