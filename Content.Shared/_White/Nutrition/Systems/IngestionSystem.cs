using Content.Shared._White.Body;
using Content.Shared._White.Body.Systems;
using Content.Shared._White.Nutrition.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Nutrition.Systems;

/// <summary>
/// This handles the ingestion of solutions and entities.
/// </summary>
public sealed partial class IngestionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    [Dependency] private readonly FlavorProfileSystem _flavorProfile = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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
        SubscribeLocalEvent<IngestibleComponent, BeforeIngestedEvent>(OnBeforeIngested);
        SubscribeLocalEvent<IngestibleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IngestibleComponent, FullyEatenEvent>(OnFullyEaten);
        SubscribeLocalEvent<IngestibleComponent, IngestedEvent>(OnIngested);
        SubscribeLocalEvent<IngestibleComponent, UseInHandEvent>(OnUseInHand, after: [typeof(OpenableSystem), typeof(InventorySystem), typeof(ActivatableUISystem)]);

        _ingestibleQuery = GetEntityQuery<IngestibleComponent>();
    }

    #region Event Handling

    private void OnAfterInteract(Entity<IngestibleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = TryIngest(ent.AsNullable(), args.User, args.Target.Value);
    }

    private void OnBeforeIngested(Entity<IngestibleComponent> ent, ref BeforeIngestedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Transfer = ent.Comp.TransferAmount ?? args.Solution.Volume;
    }

    private void OnInit(Entity<IngestibleComponent> ent, ref ComponentInit args)
    {
        _solutionContainer.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out _);
        UpdateAppearance(ent);
    }

    private void OnFullyEaten(Entity<IngestibleComponent> ent, ref FullyEatenEvent args)
    {
        SpawnTrash(ent, args.User);
    }

    private void OnIngested(Entity<IngestibleComponent> ent, ref IngestedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_prototype.TryIndex(ent.Comp.Edible, out var edible))
            return;

        _audio.PlayPredicted(ent.Comp.Sound ?? edible.Sound, args.Target, args.User);

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(ent.Owner, args.Target, args.Split);

        if (args.Target != args.User)
        {
            var targetMessage = Loc.GetString("edible-force-feed-success", ("user", Identity.Entity(args.User, EntityManager)), ("verb", edible.Verb), ("flavors", flavors), ("satiated", args.Satiated));
            _popup.PopupEntity(targetMessage, ent, args.Target);

            var userMessage = Loc.GetString("edible-force-feed-success-user", ("target", Identity.Entity(args.Target, EntityManager)), ("verb", edible.Verb));
            _popup.PopupClient(userMessage, args.User, args.User);

            _adminLog.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(ent):user} forced {ToPrettyString(args.User):target} to {edible.Verb} {ToPrettyString(ent):food}");
        }
        else
        {
            var message = Loc.GetString(edible.Message, ("food", ent.Owner), ("flavors", flavors), ("satiated", args.Satiated));
            _popup.PopupPredicted(message, Loc.GetString(edible.Message), args.User, args.User);

            _adminLog.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} {edible.Verb} {ToPrettyString(ent):food}");
        }

        if (TryGetUtensils(args.User, ent, out var utensils))
        {
            foreach (var utensil in utensils)
            {
                TryBreak(utensil, args.User);
            }
        }

        if (IsEmpty(ent))
        {
            args.Delete = ent.Comp.DeleteOnEmpty;
            return;
        }

        var transferDnaEv = new TransferDnaEvent
        {
            Donor = args.Target,
            Recipient = ent,
            CanDnaBeCleaned = false,
        };
        RaiseLocalEvent(args.Target, ref transferDnaEv);

        args.Repeat = args.Target == args.User;
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
    /// <param name="user">The entity who is eating.</param>
    /// <param name="target">The entity who is being made to ingest something.</param>
    /// <returns>Returns true if the user can feed the target with the ingested entity</returns>
    public bool CanConsume(Entity<IngestibleComponent> ent, EntityUid user, EntityUid target)
    {
        if (!_interaction.InRangeUnobstructed(user, ent.Owner, popup: true))
            return false;

        if (!_interaction.InRangeUnobstructed(user, target, MaxFeedDistance, popup: true))
            return false;

        if (ent.Comp.RequireDead && _mobState.IsAlive(ent))
            return false;

        var attemptEv = new AttemptIngestedEvent();
        RaiseLocalEvent(target, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            _popup.PopupClient(attemptEv.Popup, target, user);
            return false;
        }

        var attemptGotEv = new AttemptGotIngestedEvent();
        RaiseLocalEvent(ent, ref attemptGotEv);

        if (attemptGotEv.Cancelled)
        {
            _popup.PopupClient(attemptEv.Popup, target, user);
            return false;
        }

        if (!ent.Comp.UtensilRequired || ent.Comp.Utensil == UtensilType.None)
            return true;

        var getUtensilEv = new GetUtensilEvent();
        RaiseLocalEvent(ent, ref getUtensilEv);

        if (!getUtensilEv.Type.HasFlag(ent.Comp.Utensil))
            return false;

        return true;
    }

    /// <summary>
    /// An entity is trying to ingest another entity.
    /// </summary>
    /// <param name="ent">The entity that is trying to be ingested.</param>
    /// <param name="user">The entity who is eating.</param>
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

    public void SpawnTrash(Entity<IngestibleComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Trashes.Count == 0)
            return;

        var position = _transform.GetMapCoordinates(ent);
        var pickup = user != null && _hands.IsHolding(user.Value, ent);

        foreach (var trash in ent.Comp.Trashes)
        {
            var spawnedTrash = EntityManager.PredictedSpawn(trash, position);

            if (!pickup || user == null)
                continue;

            _hands.TryPickupAnyHand(user.Value, spawnedTrash);
        }
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

    private void UpdateAppearance(Entity<IngestibleComponent> ent)
    {
        var volume = GetVolume(ent);
        _appearance.SetData(ent, FoodVisuals.Visual, volume.Float());
    }

    #endregion
}

[ByRefEvent]
public record struct AttemptGotIngestedEvent(bool Cancelled = false)
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

/// <summary>
/// We use this to determine if an entity should abort giving up its reagents at the last minute,
/// as well as specifying how much of its reagents it should give up including minimums and maximums.
/// If minimum exceeds the  maximum, the event will abort.
/// </summary>
/// <param name="Min">The minimum amount we can transfer.</param>
/// <param name="Max">The maximum amount we can transfer.</param>
/// <param name="Solution">The solution we are transferring.</param>
[ByRefEvent]
public record struct BeforeIngestedEvent(FixedPoint2 Min, FixedPoint2 Max, Solution Solution, bool Cancelled = false)
{
    /// <summary>
    /// How much we would like to transfer, gets clamped by Min and Max.
    /// </summary>
    public FixedPoint2 Transfer;

    /// <summary>
    ///  When and if we eat this solution, should we actually remove solution or should it get replaced?
    /// This bool basically only exists because of stackable system.
    /// </summary>
    public bool Refresh;

    public bool TryNewMinimum(FixedPoint2 newMin)
    {
        if (newMin > Max)
            return false;

        Min = newMin;
        return true;
    }

    public bool TryNewMaximum(FixedPoint2 newMax)
    {
        if (newMax < Min)
            return false;

        Min = newMax;
        return true;
    }
}

[ByRefEvent]
public record struct GetUtensilEvent(UtensilType Type = UtensilType.None);

/// <summary>
/// Event raised directed at the food after finishing eating it and before it's deleted.
/// </summary>
/// <param name="User">The entity that ate the food.</param>
[ByRefEvent]
public record struct FullyEatenEvent(EntityUid User);

/// <summary>
/// Event raised on an entity when it is being made to be eaten.
/// </summary>
/// <param name="User">Who is doing the action?</param>
/// <param name="Target">Who is doing the eating?</param>
/// <param name="Split">The solution we're currently eating.</param>
/// <param name="Satiated">Whether the entity will stop eating after this.</param>
[ByRefEvent]
public record struct IngestedEvent(EntityUid User, EntityUid Target, Solution Split, bool Satiated)
{
    /// <summary>
    /// Should we delete the ingested entity?
    /// </summary>
    public bool Delete;

    /// <summary>
    /// Has this eaten event been handled? Used to prevent duplicate flavor popups and sound effects.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Should we try eating again?
    /// </summary>
    public bool Repeat;
}

/// <summary>
/// Event raised when an entity is trying to ingest an entity.
/// </summary>
/// <param name="Ingestible">What are we trying to ingest?</param>
/// <param name="User">The entity that is trying to feed and therefore raising the event</param>
/// <param name="Target">Who is doing the eating?</param>
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
public sealed partial class IngestingDoAfterEvent : SimpleDoAfterEvent, IBodyRelayEvent
{
    public BodyProviderType Type { get; } = BodyProviderType.All;
}
