using Content.Shared._White.Appearance.Systems;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Humanoid.Markings.Components;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Markings.UserInterface;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared._White.Humanoid.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings.Systems;

public abstract partial class SharedMarkingsSystem : EntitySystem
{
    [Dependency] protected new readonly IPrototypeManager Prototype = default!;
    [Dependency] protected readonly MarkingManager Marking = default!;

    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    [Dependency] private readonly SharedBodyAppearanceSystem _bodyAppearance = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    protected EntityQuery<MarkingsComponent> MarkingsQuery;
    protected EntityQuery<MarkingsProviderComponent> ProviderQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarkingsComponent, ApplyProfileEvent>(OnApplyProfile, after: [typeof(SharedBodyAppearanceSystem)]);
        SubscribeLocalEvent<MarkingsComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        InitializeProvider();

        MarkingsQuery = GetEntityQuery<MarkingsComponent>();
        ProviderQuery = GetEntityQuery<MarkingsProviderComponent>();

        Subs.BuiEvents<MarkingsComponent>(
            MarkingModifierKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUIOpened);
                subs.Event<MarkingModifierMarkingSetMessage>(OnMarkingSet);
            });
    }

    #region Event Handling

    private void OnApplyProfile(Entity<MarkingsComponent> ent, ref ApplyProfileEvent args)
    {
        var markings = new List<Marking>();
        foreach (var categoryMarkings in args.Profile.Markings.Values)
        {
            markings.AddRange(categoryMarkings);
        }

        ApplyMarkings(ent, markings);
    }

    private void OnGetVerbs(Entity<MarkingsComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Fun))
            return;

        var user = args.User;
        args.Verbs.Add(
            new Verb
            {
                Text = Loc.GetString("markings-verb"),
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/_White/Interface/palette.svg.png")),
                Act = () =>
                {
                    _userInterface.OpenUi(ent.Owner, MarkingModifierKey.Key, user);
                },
            });
    }

    private void OnUIOpened(Entity<MarkingsComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_bodyAppearance.TryGetAppearance(ent, out var appearanceData))
            return;

        if (!TryGetData(ent, out var markingsSet, out var  markingsData))
            return;

        var markings = Marking.GetMarkingsByCategory(markingsSet);
        _userInterface.SetUiState(ent.Owner, MarkingModifierKey.Key, new MarkingModifierState(appearanceData, markingsData, markings));
    }

    private void OnMarkingSet(Entity<MarkingsComponent> ent, ref MarkingModifierMarkingSetMessage args)
    {
        var markings = new List<Marking>();
        foreach (var categoryMarkings in args.Markings.Values)
        {
            markings.AddRange(categoryMarkings);
        }

        ApplyMarkings(ent, markings);
    }

    #endregion

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
public readonly record struct ApplyMarkingsEvent(List<Marking> Markings) : IBodyRelayEvent
{
    /// <summary>
    /// The body provider to which we apply markings.
    /// </summary>
    public BodyProviderType ProviderType { get; } = BodyProviderType.All;
}

/// <summary>
/// Event raised on an entity to get the markings on its provider.
/// </summary>
[ByRefEvent]
public readonly record struct GetMarkingsDataEvent() : IBodyRelayEvent
{
    /// <summary>
    /// The body provider whose markings data we wont get.
    /// </summary>
    public BodyProviderType ProviderType { get; } = BodyProviderType.All;

    /// <summary>
    /// A result contained the markings.
    /// </summary>
    public readonly Dictionary<Enum, List<Marking>> Markings = new();

    /// <summary>
    /// A result contained the marking data.
    /// </summary>
    public readonly Dictionary<Enum, MarkingData> Data = new();
}
