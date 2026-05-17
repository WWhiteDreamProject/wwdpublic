using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Nutrition.Components;
using Content.Shared._White.Random;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
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

        args.Handled = TryIngest(ent.AsNullable(), args.User);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Checks if we can feed an edible solution from an entity to a target.
    /// </summary>
    /// <param name="ent">The entity that is trying to be ingested.</param>
    /// <param name="user">The entity who is ingesting.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <param name="utensils">The utensils needed to ingest the ingestible item.</param>
    /// <returns>Returns true if the user can feed the target with the ingested entity</returns>
    public bool CanConsume(Entity<IngestibleComponent> ent, EntityUid user, EntityUid target, out List<Entity<UtensilComponent>> utensils)
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
    /// An entity is trying to ingest another entity.
    /// </summary>
    /// <param name="ent">The entity that is trying to be ingested.</param>
    /// <param name="user">The entity who is ingesting.</param>
    /// <returns>Returns true if we are now ingesting the item.</returns>
    public bool TryIngest(Entity<IngestibleComponent?> ent, EntityUid user)
    {
        return TryIngest(ent, user, user);
    }

    /// <summary>
    /// Overload of TryIngest for if an entity is trying to make another entity ingest an entity.
    /// </summary>
    /// <param name="ent">The entity that is trying to be ingested.</param>
    /// <param name="user">The entity who is trying to make this happen.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    public bool TryIngest(Entity<IngestibleComponent?> ent, EntityUid user, EntityUid target)
    {
        if (!_ingestibleQuery.Resolve(ent, ref ent.Comp))
            return false;

        var ingestibleEv = new IngestibleEvent();
        RaiseLocalEvent(ent, ref ingestibleEv);

        if (ingestibleEv.Cancelled)
            return false;

        var ingestEv = new IngestEvent((ent, ent.Comp), user, target);
        RaiseLocalEvent(target, ref ingestEv);

        return ingestEv.Handled;
    }

    #endregion

    #region Private API

    private bool IsEmpty(Entity<IngestibleComponent> ent)
    {
        return GetVolume(ent) == FixedPoint2.Zero;
    }

    private FixedPoint2 GetVolume(Entity<IngestibleComponent> ent)
    {
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return FixedPoint2.Zero;

        return solution.Volume;
    }

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

    private void UpdateAppearance(Entity<IngestibleComponent> ent)
    {
        var volume = GetVolume(ent);
        _appearance.SetData(ent, FoodVisuals.Visual, volume.Float());
    }

    #endregion
}

[ByRefEvent]
public record struct AttemptGotIngestedEvent(EntityUid User, IngestibleComponent Ingestible, bool Cancelled = false)
{
    public string? Popup = null;
}

/// <summary>
/// Event raised on an entity that is consuming another entity to see if there is anything attached to the entity
/// that is preventing it from doing the consumption.
/// </summary>
[ByRefEvent]
public record struct AttemptIngestedEvent(bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = IngestionSystem.DefaultFlags;

    public string? Popup = null;
}

[ByRefEvent]
public record struct GetUtensilsEvent(UtensilType Type, UtensilType UtensilType = UtensilType.None)
{
    public List<Entity<UtensilComponent>> Utensils;
}

/// <summary>
/// Event raised when an entity is trying to ingest an entity.
/// </summary>
/// <param name="Ingestible">What are we trying to ingest?</param>
/// <param name="User">The entity that is trying to feed and therefore raising the event</param>
/// <param name="Target">Who is doing the ingesting?</param>
[ByRefEvent]
public record struct IngestEvent(Entity<IngestibleComponent> Ingestible, EntityUid User, EntityUid Target) : IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;

    /// <summary>
    /// Did a system successfully ingest this item?
    /// </summary>
    public bool Handled;
}

/// <summary>
/// Event raised on an entity that is trying to be ingested to see if it has universal blockers preventing it from being ingested.
/// </summary>
[ByRefEvent]
public record struct IngestibleEvent(bool Cancelled = false);

/// <summary>
/// Do After Event for trying to put an ingestible solution into stomach entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class IngestingDoAfterEvent : SimpleDoAfterEvent;
