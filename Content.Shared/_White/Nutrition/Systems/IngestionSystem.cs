using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Nutrition.Components;
using Content.Shared._White.Random;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tools.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._White.Nutrition.Systems;

/// <summary>
/// This handles the ingestion of solutions and entities.
/// </summary>
public sealed partial class IngestionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPredictedRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<IngestibleComponent> _ingestibleQuery;

    public const float MaxFeedDistance = 1.0f;
    public const SlotFlags DefaultFlags = SlotFlags.HEAD | SlotFlags.MASK;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IngestibleComponent, AfterInteractEvent>(OnAfterInteract, after: [typeof(ToolOpenableSystem)]);
        SubscribeLocalEvent<IngestibleComponent, AttemptShakeEvent>(OnAttemptShake);
        SubscribeLocalEvent<IngestibleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IngestibleComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<IngestibleComponent, SolutionContainerChangedEvent>(OnSolutionContainerChanged);
        SubscribeLocalEvent<IngestibleComponent, UseInHandEvent>(OnUseInHand, after: [typeof(OpenableSystem), typeof(InventorySystem), typeof(ActivatableUISystem)]);

        InitializeBlocker();
        InitializeProvider();
        InitializeUtensil();

        _ingestibleQuery = GetEntityQuery<IngestibleComponent>();
    }

    #region Event Handling

    private void OnAfterInteract(Entity<IngestibleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryIngest(ent.AsNullable(), args.User, args.Target.Value);
    }

    private void OnAttemptShake(Entity<IngestibleComponent> ent, ref AttemptShakeEvent args)
    {
        if (!IsEmpty(ent))
            return;

        args.Cancelled = true;
    }

    private void OnInit(Entity<IngestibleComponent> ent, ref ComponentInit args)
    {
        _solutionContainer.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out _);
        UpdateAppearance(ent);
    }

    private void OnGetVerbs(Entity<IngestibleComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (ent.Owner == user || !args.CanInteract || !args.CanAccess)
            return;

        if (!CanConsume(ent, user, user, out _))
            return;

        var verb = new AlternativeVerb
        {
            Act = () =>
            {
                TryIngest(ent.AsNullable(), user, user);
            },
            Icon = ent.Comp.Icon,
            Text = Loc.GetString(ent.Comp.Name),
            Priority = 2,
        };

        args.Verbs.Add(verb);
    }

    private void OnSolutionContainerChanged(Entity<IngestibleComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        UpdateAppearance(ent);
    }

    private void OnUseInHand(Entity<IngestibleComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryIngest(ent.AsNullable(), args.User, args.User);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Initiates the process of ingesting an entity.
    /// </summary>
    /// <param name="ent">The entity being ingested.</param>
    /// <param name="user">The entity performing the ingestion (e.g., a player feeding another).</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <returns>True if the ingestion process was initiated (not necessarily completed), false otherwise.</returns>
    public bool TryIngest(Entity<IngestibleComponent?> ent, EntityUid user, EntityUid target)
    {
        if (!_ingestibleQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!CanConsume((ent, ent.Comp), user, target, out _))
            return false;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return false;

        var beforeIngestedEv = new BeforeTryIngestEvent(solution);
        RaiseLocalEvent(ent, ref beforeIngestedEv);

        var ingestEv = new TryIngestEvent((ent, ent.Comp), user, target, beforeIngestedEv.Min, beforeIngestedEv.Refresh);
        RaiseLocalEvent(target, ref ingestEv);

        return ingestEv.Handled;
    }

    #endregion

    #region Private API

    /// <summary>
    /// Checks if a target entity can consume an ingestible entity.
    /// </summary>
    /// <param name="ent">The ingestible entity being checked.</param>
    /// <param name="user">The entity performing the action (e.g., attempting to feed another). May be the same as target.</param>
    /// <param name="target">The entity that will be consuming the item.</param>
    /// <param name="utensils">Outputs a list of required utensils if any are found.</param>
    /// <returns>True if consumption is possible, false otherwise.</returns>
    private bool CanConsume(Entity<IngestibleComponent> ent, EntityUid user, EntityUid target, out List<Entity<UtensilComponent>> utensils)
    {
        utensils = new List<Entity<UtensilComponent>>();
        if (!_interaction.InRangeUnobstructed(user, ent.Owner, popup: true))
            return false;

        if (!_interaction.InRangeUnobstructed(user, target, MaxFeedDistance, popup: true))
            return false;

        if (ent.Comp.RequireDead && _mobState.IsAlive(ent))
            return false;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution)
            || solution.Volume == FixedPoint2.Zero && !ent.Comp.DeleteOnEmpty)
        {
            _popup.PopupClient(Loc.GetString("ingestion-try-use-empty", ("entity", ent)), ent, user);
            return false;
        }

        var attemptEv = new AttemptIngestedEvent();
        RaiseLocalEvent(target, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            _popup.PopupClient(attemptEv.Popup, target, user);
            return false;
        }

        var attemptGotEv = new AttemptGotIngestedEvent(user, ent.Comp);
        RaiseLocalEvent(ent, ref attemptGotEv);

        if (attemptGotEv.Cancelled)
        {
            _popup.PopupClient(attemptEv.Popup, target, user);
            return false;
        }

        if (ent.Comp.Utensil == UtensilType.None)
            return true;

        var getUtensilsEv = new GetUtensilsEvent(ent.Comp.Utensil);
        RaiseLocalEvent(ent, ref getUtensilsEv);

        utensils = getUtensilsEv.Utensils;

        if (!ent.Comp.UtensilRequired || getUtensilsEv.Type.HasFlag(ent.Comp.Utensil))
            return true;

        var message = Loc.GetString("ingestion-you-need-to-hold-utensil", ("utensil", ent.Comp.Utensil), ("verb", Loc.GetString(ent.Comp.Verb)));
        _popup.PopupClient(message, user, user);

        return false;
    }

    /// <summary>
    /// Checks if an ingestible item is empty.
    /// </summary>
    /// <param name="ent">The ingestible entity.</param>
    /// <returns>True if the item is empty, false otherwise.</returns>
    private bool IsEmpty(Entity<IngestibleComponent> ent)
    {
        return GetVolume(ent) == FixedPoint2.Zero;
    }

    /// <summary>
    /// Gets the current volume of the solution within an ingestible item.
    /// </summary>
    /// <param name="ent">The ingestible entity.</param>
    /// <returns>The volume of the solution.</returns>
    private FixedPoint2 GetVolume(Entity<IngestibleComponent> ent)
    {
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return FixedPoint2.Zero;

        return solution.Volume;
    }

    /// <summary>
    /// Destroys the ingestible item and spawns any specified trash items at its location.
    /// </summary>
    /// <param name="ent">The ingestible entity.</param>
    /// <param name="user">The entity that was interacting with the ingestible item.</param>
    private void DeleteAndSpawnTrash(Entity<IngestibleComponent> ent, EntityUid? user = null)
    {
        var position = _transform.GetMapCoordinates(ent);
        var pickup = user != null && _hands.IsHolding(user.Value, ent);

        if (!_destructible.DestroyEntity(ent.Owner))
            return;

        if (ent.Comp.Trashes.Count == 0)
            return;

        foreach (var trash in ent.Comp.Trashes)
        {
            var spawnedTrash = EntityManager.PredictedSpawn(trash, position);

            if (!pickup || user == null)
                continue;

            _hands.TryPickupAnyHand(user.Value, spawnedTrash);
        }
    }

    /// <summary>
    /// Updates the visual appearance of the ingestible item.
    /// </summary>
    /// <param name="ent">The ingestible entity to update.</param>
    private void UpdateAppearance(Entity<IngestibleComponent> ent)
    {
        var volume = GetVolume(ent);
        _appearance.SetData(ent, FoodVisuals.Visual, volume.Float());
    }

    #endregion
}

/// <summary>
/// An event raised on the target entity when an attempt is made to ingest it.
/// </summary>
/// <param name="User">The entity performing the ingestion action.</param>
/// <param name="Ingestible">The component of the entity being ingested.</param>
/// <param name="Cancelled">A flag indicating if the ingestion has been cancelled.</param>
[ByRefEvent]
public record struct AttemptGotIngestedEvent(EntityUid User, IngestibleComponent Ingestible, bool Cancelled = false)
{
    /// <summary>
    /// A string to be displayed as a popup if the ingestion is cancelled.
    /// </summary>
    public string? Popup = null;
}

/// <summary>
/// An event raised on the entity that is about to be ingested.
/// </summary>
/// <param name="Cancelled">A flag indicating if the ingestion has been cancelled.</param>
[ByRefEvent]
public record struct AttemptIngestedEvent(bool Cancelled = false) : IInventoryRelayEvent
{
    /// <summary>
    /// The inventory slots to check for blocking conditions.
    /// Defaults to HEAD and MASK, which are commonly associated with consumption blocking.
    /// </summary>
    public SlotFlags TargetSlots { get; } = IngestionSystem.DefaultFlags;

    /// <summary>
    /// A string to be displayed as a popup if the ingestion is cancelled.
    /// </summary>
    public string? Popup = null;
}

/// <summary>
/// Event raised just before an ingestible solution is trying transferred to a target.
/// </summary>
/// <param name="Solution">The solution being transferred.</param>
[ByRefEvent]
public record struct BeforeTryIngestEvent(Solution Solution)
{
    /// <summary>
    /// Determines if the original solution should be removed or if it should be replaced with a new one.
    /// </summary>
    public bool Refresh;

    /// <summary>
    /// The minimum amount of the solution that must be transferred.
    /// </summary>
    public FixedPoint2 Min;
}

/// <summary>
/// Event raised when an entity needs to find out what utensils are available and suitable for ingestion.
/// </summary>
/// <param name="Type">The required utensil type defined by the ingestible item.</param>
[ByRefEvent]
public record struct GetUtensilsEvent(UtensilType Type)
{
    /// <summary>
    /// The list to which found compatible utensils will be added.
    /// </summary>
    public List<Entity<UtensilComponent>> Utensils;

    /// <summary>
    /// The utensil type of <see cref="Utensils"/>>
    /// </summary>
    public UtensilType Result = UtensilType.None;
}

/// <summary>
/// Event raised when an entity is trying to ingest an entity.
/// </summary>
/// <param name="Ingestible">The ingestible item being consumed.</param>
/// <param name="User">The entity initiating the ingestion action.</param>
/// <param name="Target">The entity performing the ingestion.</param>
/// <param name="Min">The minimum amount of the solution that must be transferred.</param>
/// <param name="Refresh">Determines if the original solution should be removed or if it should be replaced with a new one.</param>
[ByRefEvent]
public record struct TryIngestEvent(Entity<IngestibleComponent> Ingestible, EntityUid User, EntityUid Target, FixedPoint2 Min, bool Refresh) : IBodyRelayEvent
{
    /// <summary>
    /// The type of body provider to relay this event to.
    /// </summary>
    public BodyProviderType Type { get; } = BodyProviderType.All;

    /// <summary>
    /// Indicates whether this ingestion event has been successfully handled.
    /// </summary>
    public bool Handled;
}

/// <summary>
/// Event raised when an entity is ingested.
/// </summary>
[ByRefEvent]
public record struct IngestedEvent;

/// <summary>
/// Do After Event raise when an entity is trying to ingest an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class IngestingDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Determines if the original solution should be removed or if it should be replaced with a new one.
    /// </summary>
    public bool Refresh;

    /// <summary>
    /// The minimum amount of the solution that must be transferred
    /// </summary>
    public FixedPoint2 Min;

    public IngestingDoAfterEvent(bool refresh, FixedPoint2 min)
    {
        Refresh = refresh;
        Min = min;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
