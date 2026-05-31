using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Systems;

public abstract partial class SharedMarkingsSystem : EntitySystem
{
    [Dependency] protected readonly MarkingManager Marking = default!;

    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;

    protected EntityQuery<MarkingsProviderComponent> ProviderQuery;

    private bool _clientCensorNudity;
    private bool _serverCensorNudity;

    public bool CensorNudity => _clientCensorNudity || _serverCensorNudity;

    public override void Initialize()
    {
        base.Initialize();

        InitializeProvider();

        ProviderQuery = GetEntityQuery<MarkingsProviderComponent>();

        Subs.CVar(_configuration, CCVars.AccessibilityClientCensorNudity, value => _clientCensorNudity = value, true);
        Subs.CVar(_configuration, CCVars.AccessibilityServerCensorNudity, value => _serverCensorNudity = value, true);
    }

    #region Public API

    /// <summary>
    /// Gathers all the markings-relevant data from this entity.
    /// </summary>
    /// <param name="uid">The entity to sample.</param>
    /// <param name="filter">If set, only returns data concerning the given layers.</param>
    /// <param name="set">The markings that are applied to the entity.</param>
    /// <param name="data">The marking data of providers.</param>
    public bool TryGetData(
        EntityUid uid,
        HashSet<Enum>? filter,
        out Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> set,
        out Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> data
    )
    {
        var ev = new GetMarkingsDataEvent(filter);
        RaiseLocalEvent(uid, ref ev);

        set = ev.Set;
        data = ev.Data;

        return set.Count > 0 || data.Count > 0;
    }

    /// <summary>
    /// Looks up the expected set of <see cref="MarkingsData" /> for the species to have.
    /// </summary>
    /// <param name="species">The species to look up the usual markings of.</param>
    /// <returns>A dictionary of marking categories to their usual marking data within a species.</returns>
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> GetMarkingData(ProtoId<SpeciesPrototype> species)
    {
        var markingsData = new Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData>();

        var speciesPrototype = _prototype.Index(species);
        var dollPrototype = _prototype.Index(speciesPrototype.DollPrototype);

        if (!dollPrototype.TryGetComponent<BodyComponent>(out var body, _componentFactory))
            return markingsData;

        foreach (var prototype in _body.GetProviders(body))
        {
            if (!TryGetData(prototype, out var data))
                continue;

            markingsData[data.Value.Category] = data.Value;
        }

        return markingsData;
    }

    /// <summary>
    /// Applies the given set of markings to the entity.
    /// </summary>
    /// <param name="uid">The entity whose apply markings.</param>
    /// <param name="markingsSet">A dictionary of marking categories to markings.</param>
    public void ApplyMarkings(EntityUid uid, Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markingsSet)
    {
        var ev = new ApplyMarkingsEvent(markingsSet);
        RaiseLocalEvent(uid, ref ev);
    }

    #endregion
}

/// <summary>
/// Event raised on body entity when a profile is being applied to it.
/// </summary>
[ByRefEvent]
public readonly record struct ApplyMarkingsEvent(Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> MarkingsSet);

/// <summary>
/// Event raised on an entity to get the markings on its provider.
/// </summary>
[ByRefEvent]
public readonly record struct GetMarkingsDataEvent(HashSet<Enum>? Filter)
{
    /// <summary>
    /// A result contained the marking sets.
    /// </summary>
    public readonly Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Set = new();

    /// <summary>
    /// A result contained the marking data.
    /// </summary>
    public readonly Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> Data = new();
}
