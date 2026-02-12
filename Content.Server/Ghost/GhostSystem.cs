using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Chat.Managers;
using Content.Server.Clothing.Systems;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared._White.CustomGhostSystem;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared._White.Roles;
using Content.Shared.Actions;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.SSDIndicator;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted.Components;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ghost
{
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly IAdminLogManager _adminLog = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly JobSystem _jobs = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly LoadoutSystem _loadout = default!;
        [Dependency] private readonly MindSystem _minds = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly IServerPreferencesManager _prefs = default!;

        private EntityQuery<GhostComponent> _ghostQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private readonly Dictionary<EntityUid, EntityUid> _mindHumanoidSnapshotSources = new();

        public static readonly Color AntagonistButtonColor = Color.FromHex("#7F4141"); // WWDP EDIT

        [ValidatePrototypeId<EntityPrototype>]
        private const string VisualObserverPrototypeName = "MobObserverVisualHumanoid";

        private enum GhostSpawnMode
        {
            Default,
            LobbyObserver
        }

        private static readonly HashSet<string> VisualSnapshotSlots = new(StringComparer.Ordinal)
        {
            "shoes",
            "jumpsuit",
            "outerClothing",
            "gloves",
            "neck",
            "mask",
            "eyes",
            "ears",
            "head",
            "id",
            "belt",
            "back",
            "suitstorage",
            "innerBelt",
            "innerNeck"
        };

        public override void Initialize()
        {
            base.Initialize();

            _ghostQuery = GetEntityQuery<GhostComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<GhostComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GhostComponent, ComponentShutdown>(OnGhostShutdown);

            SubscribeLocalEvent<GhostComponent, ExaminedEvent>(OnGhostExamine);

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<MindComponent, MindGotRemovedEvent>(OnMindGotRemoved);
            SubscribeLocalEvent<MindComponent, EntityTerminatingEvent>(OnMindTerminating);

            SubscribeLocalEvent<GhostOnMoveComponent, MoveInputEvent>(OnRelayMoveInput);

            SubscribeNetworkEvent<GhostWarpsRequestEvent>(OnGhostWarpsRequest);
            SubscribeNetworkEvent<GhostReturnToBodyRequest>(OnGhostReturnToBodyRequest);
            SubscribeNetworkEvent<GhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
            SubscribeNetworkEvent<GhostnadoRequestEvent>(OnGhostnadoRequest);

            SubscribeLocalEvent<GhostComponent, BooActionEvent>(OnActionPerform);
            SubscribeLocalEvent<GhostComponent, ToggleGhostHearingActionEvent>(OnGhostHearingAction);
            SubscribeLocalEvent<GhostComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);

            SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));
            SubscribeLocalEvent<ToggleGhostVisibilityToAllEvent>(OnToggleGhostVisibilityToAll);
        }

        private void OnGhostHearingAction(EntityUid uid, GhostComponent component, ToggleGhostHearingActionEvent args)
        {
            args.Handled = true;

            if (HasComp<GhostHearingComponent>(uid))
            {
                RemComp<GhostHearingComponent>(uid);
                _actions.SetToggled(component.ToggleGhostHearingActionEntity, true);
            }
            else
            {
                AddComp<GhostHearingComponent>(uid);
                _actions.SetToggled(component.ToggleGhostHearingActionEntity, false);
            }

            var str = HasComp<GhostHearingComponent>(uid)
                ? Loc.GetString("ghost-gui-toggle-hearing-popup-on")
                : Loc.GetString("ghost-gui-toggle-hearing-popup-off");

            Popup.PopupEntity(str, uid, uid);
            Dirty(uid, component);
        }

        private void OnActionPerform(EntityUid uid, GhostComponent component, BooActionEvent args)
        {
            if (args.Handled)
                return;

            var entities = _lookup.GetEntitiesInRange(args.Performer, component.BooRadius).ToList();
            // Shuffle the possible targets so we don't favor any particular entities
            _random.Shuffle(entities);

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = DoGhostBooEvent(ent);

                if (handled)
                    booCounter++;

                if (booCounter >= component.BooMaxTargets)
                    break;
            }

            if (booCounter == 0)
                _popup.PopupEntity(Loc.GetString("ghost-component-boo-action-failed"), uid, uid);

            args.Handled = true;
        }

        private void OnRelayMoveInput(EntityUid uid, GhostOnMoveComponent component, ref MoveInputEvent args)
        {
            // If they haven't actually moved then ignore it.
            if ((args.Entity.Comp.HeldMoveButtons &
                 (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) == 0x0)
            {
                return;
            }

            // Let's not ghost if our mind is visiting...
            if (HasComp<VisitingMindComponent>(uid))
                return;

            if (!_minds.TryGetMind(uid, out var mindId, out var mind) || mind.IsVisitingEntity)
                return;

            if (component.MustBeDead && (_mobState.IsAlive(uid) || _mobState.IsCritical(uid)))
                return;

            OnGhostAttempt(mindId, component.CanReturn, mind: mind);
        }

        private void OnGhostStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by other ghosts.
            var visibility = EnsureComp<VisibilityComponent>(uid);

            if (_gameTicker.RunLevel != GameRunLevel.PostRound)
            {
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            SetCanSeeGhosts(uid, true);

            var time = _gameTiming.CurTime;
            component.TimeOfDeath = time;
        }

        private void OnGhostShutdown(EntityUid uid, GhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (Terminating(uid))
                return;

            // Entity can't be seen by ghosts anymore.
            if (TryComp(uid, out VisibilityComponent? visibility))
            {
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            // Entity can't see ghosts anymore.
            SetCanSeeGhosts(uid, false);
            _actions.RemoveAction(uid, component.BooActionEntity);
        }

        private void SetCanSeeGhosts(EntityUid uid, bool canSee, EyeComponent? eyeComponent = null)
        {
            if (!Resolve(uid, ref eyeComponent, false))
                return;

            if (canSee)
                _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | (int) VisibilityFlags.Ghost, eyeComponent);
            else
                _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask & ~(int) VisibilityFlags.Ghost, eyeComponent);
        }

        private void OnMapInit(EntityUid uid, GhostComponent component, MapInitEvent args)
        {
            _actions.AddAction(uid, ref component.BooActionEntity, component.BooAction);
            _actions.AddAction(uid, ref component.ToggleGhostHearingActionEntity, component.ToggleGhostHearingAction);
            _actions.AddAction(uid, ref component.ToggleLightingActionEntity, component.ToggleLightingAction);
            _actions.AddAction(uid, ref component.ToggleFoVActionEntity, component.ToggleFoVAction);
            _actions.AddAction(uid, ref component.ToggleGhostsActionEntity, component.ToggleGhostsAction);
        }

        private void OnGhostExamine(EntityUid uid, GhostComponent component, ExaminedEvent args)
        {
            var timeSinceDeath = _gameTiming.CurTime.Subtract(component.TimeOfDeath);
            var deathTimeInfo = timeSinceDeath.Minutes > 0
                ? Loc.GetString("comp-ghost-examine-time-minutes", ("minutes", timeSinceDeath.Minutes))
                : Loc.GetString("comp-ghost-examine-time-seconds", ("seconds", timeSinceDeath.Seconds));

            args.PushMarkup(deathTimeInfo);
        }

        #region Ghost Deletion

        private void OnMindRemovedMessage(EntityUid uid, GhostComponent component, MindRemovedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnMindUnvisitedMessage(EntityUid uid, GhostComponent component, MindUnvisitedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnPlayerDetached(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            DeleteEntity(uid);
        }

        private void OnMindGotRemoved(EntityUid uid, MindComponent component, MindGotRemovedEvent args)
        {
            TryCaptureMindHumanoidSnapshot(uid, args.Container.Owner);
        }

        private void OnMindTerminating(EntityUid uid, MindComponent component, ref EntityTerminatingEvent args)
        {
            ClearMindHumanoidSnapshot(uid);
        }

        private void DeleteEntity(EntityUid uid)
        {
            if (Deleted(uid) || Terminating(uid))
                return;

            QueueDel(uid);
        }

        #endregion

        private void OnGhostReturnToBodyRequest(GhostReturnToBodyRequest msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !_ghostQuery.TryComp(attached, out var ghost)
                || !ghost.CanReturnToBody
                || !TryComp(attached, out ActorComponent? actor))
            {
                Log.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(GhostReturnToBodyRequest)}");
                return;
            }

            _mind.UnVisit(actor.PlayerSession);
        }

        #region Warp

        private void OnGhostWarpsRequest(GhostWarpsRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} entity
                || !_ghostQuery.HasComp(entity))
            {
                Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
                return;
            }

            // WWDP-Start
            var mindContainers = GetMindContainersWarps();
            var places = GetLocationWarps();
            var response = new GhostWarpsResponseEvent(mindContainers.Concat(places).ToList());
            // WWDP-End
            RaiseNetworkEvent(response, args.SenderSession.Channel);
        }

        private void OnGhostWarpToTargetRequest(GhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !_ghostQuery.HasComp(attached))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Target} without being a ghost.");
                return;
            }

            var target = GetEntity(msg.Target);

            if (!Exists(target))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to an invalid entity id: {msg.Target}");
                return;
            }

            // WWDP EDIT START
            if (IsHiddenFromGhostWarps(target) || !IsValidWarpTarget(target))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to an invalid/hidden target: {ToPrettyString(target)}");
                return;
            }
            // WWDP EDIT END

            WarpTo(attached, target);
        }

        private void OnGhostnadoRequest(GhostnadoRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not { Valid: true } uid // WD EDIT
                || !_ghostQuery.HasComp(uid))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to ghostnado without being a ghost.");
                return;
            }

            if (_followerSystem.GetMostGhostFollowed() is not { } target)
                return;

            WarpTo(uid, target);
        }

        private void WarpTo(EntityUid uid, EntityUid target)
        {
            _adminLog.Add(LogType.GhostWarp, $"{ToPrettyString(uid)} ghost warped to {ToPrettyString(target)}");

            if ((TryComp(target, out WarpPointComponent? warp) && warp.Follow) || HasComp<MobStateComponent>(target))
            {
                _followerSystem.StartFollowingEntity(uid, target);
                return;
            }

            var xform = Transform(uid);
            _transformSystem.SetCoordinates(uid, xform, Transform(target).Coordinates);
            _transformSystem.AttachToGridOrMap(uid, xform);
            if (_physicsQuery.TryComp(uid, out var physics))
                _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        }

        // WD EDIT START
        private List<GhostWarp> GetMindContainersWarps()
        {
            var warps = new List<GhostWarp>();

            var query = EntityQueryEnumerator<MindContainerComponent>();

            while (query.MoveNext(out var entity, out var mindContainer))
            {
                if(IsHiddenFromGhostWarps(entity) || !IsValidWarpTarget(entity))
                    continue;

                if (TryComp<RoleCacheComponent>(entity, out var roleCacheComponent))
                {
                    if (_prototypeManager.TryIndex(roleCacheComponent.LastJobPrototype, out var jobPrototype) &&
                        _jobs.TryGetDepartment(jobPrototype.ID, out var departmentPrototype))
                    {
                        var warp = SetupWarp(entity, mindContainer, departmentPrototype.Name, departmentPrototype.Color, jobPrototype.Name);
                        warp.Group |= WarpGroup.Department;

                        warps.Add(warp);
                    }

                    if (roleCacheComponent.IsAntag &&
                        _prototypeManager.TryIndex(roleCacheComponent.LastAntagPrototype, out var antagPrototype))
                    {
                        var warp = SetupWarp(entity, mindContainer, antagPrototype.Name, AntagonistButtonColor, null);
                        warp.Group |= WarpGroup.Antag;

                        warps.Add(warp);
                    }
                }
                else
                {
                    var warp = SetupWarp(entity, mindContainer, MetaData(entity).EntityPrototype?.Name ?? "", null, null);
                    warp.Group |= WarpGroup.Other;

                    warps.Add(warp);
                }
            }

            return warps;
        }

        private GhostWarp SetupWarp(EntityUid entity, MindContainerComponent mindContainer, string subGroup, Color? color, string? description)
        {
            var hasAnyMind = (mindContainer.Mind ?? mindContainer.OriginalMind) != null;
            var isDead = _mobState.IsDead(entity);
            var isLeft = TryComp<SSDIndicatorComponent>(entity, out var indicator) && indicator.IsSSD && !isDead &&
                hasAnyMind;

            var metadata = Comp<MetaDataComponent>(entity);

            if (string.IsNullOrEmpty(description))
                description = metadata.EntityDescription;

            var warp = new GhostWarp(GetNetEntity(entity), metadata.EntityName, subGroup, description, color);

            if(isLeft)
                warp.Group |= WarpGroup.Left;

            if(HasComp<GhostComponent>(entity))
            {
                warp.Group |= WarpGroup.Ghost;
            }
            else
            {
                if(isDead)
                    warp.Group |= WarpGroup.Dead;
                else
                    warp.Group |= WarpGroup.Alive;
            }

            return warp;
        }

        private List<GhostWarp> GetLocationWarps()
        {
            var warps = new List<GhostWarp>();
            var allQuery = AllEntityQuery<WarpPointComponent>();

            while (allQuery.MoveNext(out var uid, out var warp))
            {
                if(IsHiddenFromGhostWarps(uid))
                    continue;

                var newWarp = new GhostWarp(GetNetEntity(uid), warp.Location ?? Name(uid), "", Description(uid), null);
                warps.Add(newWarp);
            }

            return warps;
        }

        private bool IsValidWarpTarget(EntityUid entity)
        {
            var transform = Transform(entity);
            if (transform.MapID == MapId.Nullspace)
                return false;

            return true;
        }

        private bool IsHiddenFromGhostWarps(EntityUid entity)
        {
            return _tag.HasTag(entity, "HideFromGhostWarps");
        }

        // WD EDIT END


        #endregion

        private void OnEntityStorageInsertAttempt(EntityUid uid, GhostComponent comp, ref InsertIntoEntityStorageAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnToggleGhostVisibilityToAll(ToggleGhostVisibilityToAllEvent ev)
        {
            if (ev.Handled)
                return;

            ev.Handled = true;
            MakeVisible(true);
        }

        /// <summary>
        /// When the round ends, make all players able to see ghosts.
        /// </summary>
        public void MakeVisible(bool visible)
        {
            var entityQuery = EntityQueryEnumerator<GhostComponent, VisibilityComponent>();
            while (entityQuery.MoveNext(out var uid, out var _, out var vis))
            {
                if (!_tag.HasTag(uid, "AllowGhostShownByEvent"))
                    continue;

                if (visible)
                {
                    _visibilitySystem.AddLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                    _visibilitySystem.RemoveLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                }
                else
                {
                    _visibilitySystem.AddLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                    _visibilitySystem.RemoveLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                }
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: vis);
            }
        }

        public bool DoGhostBooEvent(EntityUid target)
        {
            var ghostBoo = new GhostBooEvent();
            RaiseLocalEvent(target, ghostBoo, true);

            return ghostBoo.Handled;
        }

        public EntityUid? SpawnGhost(Entity<MindComponent?> mind, EntityUid targetEntity,
            bool canReturn = false)
        {
            _transformSystem.TryGetMapOrGridCoordinates(targetEntity, out var spawnPosition);
            return SpawnGhost(mind, spawnPosition, canReturn, targetEntity);
        }

        public EntityUid? SpawnLobbyObserverGhost(Entity<MindComponent?> mind, EntityCoordinates? spawnPosition = null,
            bool canReturn = false)
        {
            return SpawnGhostInternal(mind, spawnPosition, canReturn, GhostSpawnMode.LobbyObserver);
        }

        private bool IsValidSpawnPosition(EntityCoordinates? spawnPosition)
        {
            if (spawnPosition?.IsValid(EntityManager) != true)
                return false;

            var mapUid = spawnPosition?.GetMapUid(EntityManager);
            var gridUid = spawnPosition?.EntityId;
            // Test if the map is being deleted
            if (mapUid == null || TerminatingOrDeleted(mapUid.Value))
                return false;
            // Test if the grid is being deleted
            if (gridUid != null && TerminatingOrDeleted(gridUid.Value))
                return false;

            return true;
        }

        public EntityUid? SpawnGhost(Entity<MindComponent?> mind, EntityCoordinates? spawnPosition = null,
            bool canReturn = false, EntityUid? sourceEntity = null)
        {
            return SpawnGhostInternal(mind, spawnPosition, canReturn, GhostSpawnMode.Default, sourceEntity);
        }

        private EntityUid? SpawnGhostInternal(Entity<MindComponent?> mind, EntityCoordinates? spawnPosition,
            bool canReturn, GhostSpawnMode mode, EntityUid? sourceEntity = null)
        {
            if (!Resolve(mind, ref mind.Comp))
                return null;

            // Test if the map or grid is being deleted
            if (!IsValidSpawnPosition(spawnPosition))
                spawnPosition = null;

            // If it's bad, look for a valid point to spawn
            spawnPosition ??= _gameTicker.GetObserverSpawnPoint();

            // Make sure the new point is valid too
            if (!IsValidSpawnPosition(spawnPosition))
            {
                Log.Warning($"No spawn valid ghost spawn position found for {mind.Comp.CharacterName}"
                    + $" \"{ToPrettyString(mind)}\"");
                _minds.TransferTo(mind.Owner, null, createGhost: false, mind: mind.Comp);
                return null;
            }

            EntityUid ghost = EntityUid.Invalid;
            var spawnedVisualGhost = false;
            var snapshotSource = mode == GhostSpawnMode.Default
                ? ResolveVisualSnapshotSource(mind.Owner, sourceEntity)
                : null;

            if (mode == GhostSpawnMode.LobbyObserver)
                spawnedVisualGhost = TrySpawnLobbyObserverGhost((mind.Owner, mind.Comp), spawnPosition.Value, out ghost);
            else if (snapshotSource is { } source)
                spawnedVisualGhost = TrySpawnBodySnapshotGhost(source, spawnPosition.Value, out ghost);

            if (!spawnedVisualGhost)
                ghost = SpawnFallbackGhost((mind.Owner, mind.Comp), spawnPosition.Value);

            if (!TryComp<GhostComponent>(ghost, out var ghostComponent))
            {
                Log.Error($"Spawned ghost prototype without {nameof(GhostComponent)}: {ToPrettyString(ghost)}");
                QueueDel(ghost);
                _minds.TransferTo(mind.Owner, null, createGhost: false, mind: mind.Comp);
                return null;
            }

            CopyMovementDefaultsFromCurrentEntity(mind.Comp.CurrentEntity, ghost);

            if (spawnedVisualGhost)
            {
                ghostComponent.CanGhostInteract = false;
                EnsureComp<IgnoreSlowOnDamageComponent>(ghost);
                EnsureComp<SpeedModifierImmunityComponent>(ghost);
                _movementSpeed.RefreshMovementSpeedModifiers(ghost);
            }

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            if (FirstNonNullNonEmpty(mind.Comp.CharacterName, mind.Comp.Session?.Name) is string ghostName)
                _metaData.SetEntityName(ghost, ghostName);

            if (mind.Comp.TimeOfDeath.HasValue)
            {
                SetTimeOfDeath(ghost, mind.Comp.TimeOfDeath!.Value, ghostComponent);
            }

            SetCanReturnToBody(ghostComponent, canReturn);

            if (canReturn)
                _minds.Visit(mind.Owner, ghost, mind.Comp);
            else
                _minds.TransferTo(mind.Owner, ghost, mind: mind.Comp);
            Log.Debug($"Spawned ghost \"{ToPrettyString(ghost)}\" for {mind.Comp.CharacterName}.");
            return ghost;
        }

        private void CopyMovementDefaultsFromCurrentEntity(EntityUid? sourceEntity, EntityUid ghost)
        {
            if (sourceEntity is not { } source ||
                !TryComp<InputMoverComponent>(source, out var sourceMover) ||
                !TryComp<InputMoverComponent>(ghost, out var ghostMover))
            {
                return;
            }

            if (ghostMover.DefaultSprinting == sourceMover.DefaultSprinting)
                return;

            ghostMover.DefaultSprinting = sourceMover.DefaultSprinting;
            Dirty(ghost, ghostMover);
        }

        private EntityUid SpawnFallbackGhost(Entity<MindComponent> mind, EntityCoordinates spawnPosition)
        {
            CustomGhostPrototype? customGhost = null;
            if (_prototypeManager.TryIndex(_prefs.GetPreferencesOrNull(mind.Comp.UserId)?.CustomGhost, out var ghostProto))
                customGhost = ghostProto;

            return SpawnAtPosition(customGhost?.GhostEntityPrototype ?? GameTicker.ObserverPrototypeName, spawnPosition);
        }

        private bool TrySpawnLobbyObserverGhost(Entity<MindComponent> mind, EntityCoordinates spawnPosition, out EntityUid ghost)
        {
            ghost = default;

            if (mind.Comp.UserId is not { } userId)
                return false;

            var prefs = _prefs.GetPreferencesOrNull(userId);
            if (prefs?.SelectedCharacter is not HumanoidCharacterProfile profile)
                return false;

            if (!TryGetVisualObserverPrototype(profile.Species, out var ghostPrototype))
                return false;

            ghost = SpawnAtPosition(ghostPrototype, spawnPosition);

            if (!HasComp<HumanoidAppearanceComponent>(ghost))
            {
                QueueDel(ghost);
                ghost = default;
                return false;
            }

            _humanoid.LoadProfile(ghost, profile, loadExtensions: false, generateLoadouts: false);
            _metaData.SetEntityName(ghost, profile.Name);

            if (!TryPickLobbyObserverJob(profile, out var jobId) ||
                !_prototypeManager.TryIndex<JobPrototype>(jobId, out var jobProto))
            {
                return true;
            }

            if (jobProto.StartingGear != null &&
                _prototypeManager.TryIndex<StartingGearPrototype>(jobProto.StartingGear, out var startingGear))
            {
                startingGear = _stationSpawning.ApplySubGear(startingGear, profile, jobProto);
                _stationSpawning.EquipStartingGear(ghost, startingGear, raiseEvent: false);
            }

            if (!_configurationManager.GetCVar(CCVars.GameLoadoutsEnabled))
                return true;

            Dictionary<string, TimeSpan> playTimes = new();
            var whitelisted = false;
            if (_playerManager.TryGetSessionById(userId, out var session))
            {
                if (_playTimeTracking.TryGetTrackerTimes(session, out var trackedTimes))
                    playTimes = trackedTimes;

                whitelisted = session.ContentData()?.Whitelisted ?? false;
            }

            _loadout.ApplyCharacterLoadout(ghost, jobId, profile, playTimes, whitelisted, deleteFailed: true, jobProto: jobProto);
            return true;
        }

        private bool TryPickLobbyObserverJob(HumanoidCharacterProfile profile, out ProtoId<JobPrototype> jobId)
        {
            foreach (var (job, priority) in profile.JobPriorities)
            {
                if (priority != JobPriority.High || !_prototypeManager.HasIndex<JobPrototype>(job))
                    continue;

                jobId = job;
                return true;
            }

            var overflowJob = new ProtoId<JobPrototype>(SharedGameTicker.FallbackOverflowJob);
            if (_prototypeManager.HasIndex<JobPrototype>(overflowJob))
            {
                jobId = overflowJob;
                return true;
            }

            jobId = default;
            return false;
        }

        private bool TrySpawnBodySnapshotGhost(EntityUid sourceEntity, EntityCoordinates spawnPosition, out EntityUid ghost)
        {
            ghost = default;

            if (!TryGetVisualObserverPrototype(sourceEntity, out var ghostPrototype))
                return false;

            ghost = SpawnAtPosition(ghostPrototype, spawnPosition);

            if (!HasComp<HumanoidAppearanceComponent>(ghost))
            {
                QueueDel(ghost);
                ghost = default;
                return false;
            }

            _humanoid.CloneAppearance(sourceEntity, ghost);
            CopyDamageSnapshot(sourceEntity, ghost);
            CopyInventorySnapshot(sourceEntity, ghost);

            return true;
        }

        private EntityUid? ResolveVisualSnapshotSource(EntityUid mindId, EntityUid? sourceEntity)
        {
            if (sourceEntity is { } source && Exists(source) && HasComp<HumanoidAppearanceComponent>(source))
                return source;

            return TryGetCachedMindHumanoidSnapshot(mindId, out var cached) ? cached : null;
        }

        private bool TryGetCachedMindHumanoidSnapshot(EntityUid mindId, out EntityUid cached)
        {
            cached = default;
            if (!_mindHumanoidSnapshotSources.TryGetValue(mindId, out var snapshot))
                return false;

            if (!Exists(snapshot) || !HasComp<HumanoidAppearanceComponent>(snapshot))
            {
                _mindHumanoidSnapshotSources.Remove(mindId);
                return false;
            }

            cached = snapshot;
            return true;
        }

        private bool TryCaptureMindHumanoidSnapshot(EntityUid mindId, EntityUid sourceEntity)
        {
            if (!Exists(sourceEntity) || !TryGetVisualObserverPrototype(sourceEntity, out var ghostPrototype))
                return false;

            ClearMindHumanoidSnapshot(mindId);

            var snapshot = Spawn(ghostPrototype, MapCoordinates.Nullspace);
            if (!HasComp<HumanoidAppearanceComponent>(snapshot))
            {
                QueueDel(snapshot);
                return false;
            }

            _mindHumanoidSnapshotSources[mindId] = snapshot;
            _humanoid.CloneAppearance(sourceEntity, snapshot);
            CopyDamageSnapshot(sourceEntity, snapshot);
            CopyInventorySnapshot(sourceEntity, snapshot);
            return true;
        }

        private bool TryGetVisualObserverPrototype(EntityUid sourceEntity, out string prototype)
        {
            prototype = default!;
            if (!TryComp<HumanoidAppearanceComponent>(sourceEntity, out var humanoid))
                return false;

            return TryGetVisualObserverPrototype(humanoid.Species, out prototype);
        }

        private bool TryGetVisualObserverPrototype(ProtoId<SpeciesPrototype> species, out string prototype)
        {
            prototype = VisualObserverPrototypeName;
            if (!_prototypeManager.TryIndex(species, out var speciesPrototype))
                return _prototypeManager.HasIndex<EntityPrototype>(prototype);

            var bySpecies = $"MobObserverVisual{speciesPrototype.ID}";
            if (_prototypeManager.HasIndex<EntityPrototype>(bySpecies))
            {
                prototype = bySpecies;
                return true;
            }

            var byDoll = TryGetVisualObserverPrototypeFromDoll(speciesPrototype.DollPrototype);
            if (byDoll != null && _prototypeManager.HasIndex<EntityPrototype>(byDoll))
            {
                prototype = byDoll;
                return true;
            }

            return _prototypeManager.HasIndex<EntityPrototype>(prototype);
        }

        private static string? TryGetVisualObserverPrototypeFromDoll(EntProtoId dollPrototype)
        {
            const string dollPrefix = "Mob";
            const string dollSuffix = "Dummy";

            var dollId = dollPrototype.Id;
            if (!dollId.StartsWith(dollPrefix, StringComparison.Ordinal) ||
                !dollId.EndsWith(dollSuffix, StringComparison.Ordinal))
            {
                return null;
            }

            var speciesIdLength = dollId.Length - dollPrefix.Length - dollSuffix.Length;
            if (speciesIdLength <= 0)
                return null;

            var speciesId = dollId.Substring(dollPrefix.Length, speciesIdLength);
            return $"MobObserverVisual{speciesId}";
        }

        private void ClearMindHumanoidSnapshot(EntityUid mindId)
        {
            if (!_mindHumanoidSnapshotSources.Remove(mindId, out var snapshot))
                return;

            if (Exists(snapshot) && !TerminatingOrDeleted(snapshot))
                QueueDel(snapshot);
        }

        private void CopyDamageSnapshot(EntityUid sourceEntity, EntityUid ghost)
        {
            if (!TryComp<DamageableComponent>(sourceEntity, out var sourceDamageable) ||
                !TryComp<DamageableComponent>(ghost, out var ghostDamageable))
            {
                return;
            }

            _damageable.SetDamage(ghost, ghostDamageable, new DamageSpecifier(sourceDamageable.Damage));
        }

        private void CopyInventorySnapshot(EntityUid sourceEntity, EntityUid ghost)
        {
            if (!_inventory.TryGetSlots(sourceEntity, out var slots))
                return;

            var ghostCoordinates = Transform(ghost).Coordinates;
            foreach (var slot in slots)
            {
                if (!VisualSnapshotSlots.Contains(slot.Name))
                    continue;

                if (!_inventory.TryGetSlotEntity(sourceEntity, slot.Name, out var sourceItem))
                    continue;

                CopyInventorySlotVisualSnapshot(sourceItem.Value, ghost, slot.Name, ghostCoordinates);
            }
        }

        private void CopyInventorySlotVisualSnapshot(EntityUid sourceItem, EntityUid ghost, string slotName, EntityCoordinates ghostCoordinates)
        {
            var prototype = MetaData(sourceItem).EntityPrototype?.ID;
            if (prototype == null || !_prototypeManager.HasIndex<EntityPrototype>(prototype))
                return;

            var clone = SpawnAtPosition(prototype, ghostCoordinates);

            if (TryComp<ItemComponent>(sourceItem, out var sourceItemComp) &&
                TryComp<ItemComponent>(clone, out var cloneItemComp))
            {
                _item.CopyVisuals(clone, sourceItemComp, cloneItemComp);
            }

            if (TryComp<ClothingComponent>(sourceItem, out var sourceClothingComp) &&
                TryComp<ClothingComponent>(clone, out var cloneClothingComp))
            {
                _clothing.CopyVisuals(clone, sourceClothingComp, cloneClothingComp);
            }

            _appearance.CopyData(sourceItem, clone);

            if (!_inventory.TryEquip(ghost, clone, slotName, silent: true, force: true))
                QueueDel(clone);
        }

        private static string? FirstNonNullNonEmpty(params string?[] strings)
        {
            foreach (var str in strings)
            {
                if (!string.IsNullOrWhiteSpace(str))
                    return str;
            }

            return null;
        }

        public bool OnGhostAttempt(EntityUid mindId, bool canReturnGlobal, bool viaCommand = false, MindComponent? mind = null)
        {
            if (!Resolve(mindId, ref mind))
                return false;

            var playerEntity = mind.CurrentEntity;

            if (playerEntity != null && viaCommand)
                _adminLog.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} is attempting to ghost via command");

            var handleEv = new GhostAttemptHandleEvent(mind, canReturnGlobal);
            RaiseLocalEvent(handleEv);

            // Something else has handled the ghost attempt for us! We return its result.
            if (handleEv.Handled)
                return handleEv.Result;

            if (mind.PreventGhosting)
            {
                if (mind.Session != null) // Logging is suppressed to prevent spam from ghost attempts caused by movement attempts
                {
                    _chatManager.DispatchServerMessage(mind.Session, Loc.GetString("comp-mind-ghosting-prevented"),
                        true);
                }

                return false;
            }

            if (TryComp<GhostComponent>(playerEntity, out var comp) && !comp.CanGhostInteract)
                return false;

            if (mind.VisitingEntity != default)
            {
                _mind.UnVisit(mindId, mind: mind);
            }

            var position = Exists(playerEntity)
                ? Transform(playerEntity.Value).Coordinates
                : _gameTicker.GetObserverSpawnPoint();

            if (position == default)
                return false;

            // Ok, so, this is the master place for the logic for if ghosting is "too cheaty" to allow returning.
            // There's no reason at this time to move it to any other place, especially given that the 'side effects required' situations would also have to be moved.
            // + If CharacterDeadPhysically applies, we're physically dead. Therefore, ghosting OK, and we can return (this is critical for gibbing)
            //   Note that we could theoretically be ICly dead and still physically alive and vice versa.
            //   (For example, a zombie could be dead ICly, but may retain memories and is definitely physically active)
            // + If we're in a mob that is critical, and we're supposed to be able to return if possible,
            //   we're succumbing - the mob is killed. Therefore, character is dead. Ghosting OK.
            //   (If the mob survives, that's a bug. Ghosting is kept regardless.)
            var canReturn = canReturnGlobal && _mind.IsCharacterDeadPhysically(mind);

            if (_configurationManager.GetCVar(CCVars.GhostKillCrit) &&
                canReturnGlobal &&
                TryComp(playerEntity, out MobStateComponent? mobState))
            {
                if (_mobState.IsCritical(playerEntity.Value, mobState))
                {
                    canReturn = true;

                    //todo: what if they dont breathe lol
                    //cry deeply

                    FixedPoint2 dealtDamage = 200;

                    if (TryComp<DamageableComponent>(playerEntity, out var damageable)
                        && TryComp<MobThresholdsComponent>(playerEntity, out var thresholds))
                    {
                        var playerDeadThreshold = _mobThresholdSystem.GetThresholdForState(playerEntity.Value, MobState.Dead, thresholds);
                        dealtDamage = playerDeadThreshold - damageable.TotalDamage;
                    }

                    DamageSpecifier damage = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), dealtDamage);

                    _damageable.TryChangeDamage(playerEntity, damage, true);
                }
            }

            if (playerEntity != null)
                _adminLog.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} ghosted{(!canReturn ? " (non-returnable)" : "")}");

            var ghost = SpawnGhost((mindId, mind), position, canReturn, playerEntity);

            if (ghost == null)
                return false;

            return true;
        }

    }

    public sealed class GhostAttemptHandleEvent(MindComponent mind, bool canReturnGlobal) : HandledEntityEventArgs
    {
        public MindComponent Mind { get; } = mind;
        public bool CanReturnGlobal { get; } = canReturnGlobal;
        public bool Result { get; set; }
    }
}
