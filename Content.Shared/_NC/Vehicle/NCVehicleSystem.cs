using Content.Shared.Vehicles; // Correct namespace for SharedVehicleSystem
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems; // Added for MoveButtons
using Content.Shared.Popups;
using Robust.Shared.Maths;
using Content.Shared.FixedPoint; // Added for FixedPoint2
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Access;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Content.Shared.Audio;
using Content.Shared.Throwing;
using Content.Shared.Stunnable;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Verbs;
using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared._NC.Vehicle; // Changed from Content.Goobstation.Shared._NC.Vehicle for cleaner integration

/// <summary>
///     Handles specialized vehicle mechanics for Night City using NCVehicleComponent.
///     Extends base vehicle functionality without modifying core files.
/// </summary>
public sealed class NCVehicleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedVehicleSystem _vehicle = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    // Removed Actions dependency
    [Dependency] private readonly LockSystem _lock = default!; // Changed from SharedLockSystem
    [Dependency] private readonly AccessReaderSystem _accessReader = default!; // Injected for modifying access lists

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    // NC
    public void PersonalizeVehicle(EntityUid vehicle, EntityUid buyer, NCVehicleComponent? component = null)
    {
        if (!Resolve(vehicle, ref component)) return;
        if (_net.IsClient) return;

        // 1. Generate Plate: 2 Letters + 4 Digits
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var numbers = "0123456789";
        var plate = $"{letters[_random.Next(letters.Length)]}{letters[_random.Next(letters.Length)]}-" +
                    $"{numbers[_random.Next(numbers.Length)]}{numbers[_random.Next(numbers.Length)]}" +
                    $"{numbers[_random.Next(numbers.Length)]}{numbers[_random.Next(numbers.Length)]}";

        component.OwnerPlate = plate;
        component.OwnerName = Name(buyer);
        Dirty(vehicle, component);

        // 2. Lock AccessReader (Prevent native access)
        if (EnsureComp<AccessReaderComponent>(vehicle, out var reader))
        {
            // Add an impossible tag to disable native interactions for non-admins
            // Using "Command" as a placeholder high-level tag, or we could leave it empty
            // IF we assume empty reader = check fails (which it does if we rely on our custom verb only)
            // But LockSystem allows empty readers.
            // So we MUST add a requirement.
            var impossible = new ProtoId<AccessLevelPrototype>("Syndicate"); // Require Syndicate access (unlikely for civs)
            reader.AccessLists.Add(new HashSet<ProtoId<AccessLevelPrototype>> { impossible });
            Dirty(vehicle, reader);
        }

        // 3. Find PDA/ID and stamp it
        var idOrPdaFound = false;

        void StampItem(EntityUid item)
        {
            var key = EnsureComp<VehicleKeyComponent>(item);
            key.Plate = plate;
            Dirty(item, key);
        }

        void ProcessItem(EntityUid item)
        {
            if (idOrPdaFound) return;

            if (TryComp<PdaComponent>(item, out var pda))
            {
                pda.VehicleId = plate;
                pda.VehicleName = Name(vehicle);
                Dirty(item, pda);

                StampItem(item); // Stamp the PDA itself

                if (pda.ContainedId != null)
                {
                    StampItem(pda.ContainedId.Value); // Stamp the ID inside
                }
                idOrPdaFound = true;
            }
            else if (HasComp<IdCardComponent>(item))
            {
                StampItem(item); // Stamp the ID
                idOrPdaFound = true;
            }
        }

        // Check Hands and Inventory
        foreach (var item in _inventory.GetHandOrInventoryEntities(buyer))
        {
            ProcessItem(item);
            if (idOrPdaFound) break;
        }
    }
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NCVehicleComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<NCVehicleComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<NCVehicleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NCVehicleComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        SubscribeLocalEvent<NCVehicleComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<NCVehicleComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NCVehicleComponent, LockToggleAttemptEvent>(OnLockAttempt);
        SubscribeLocalEvent<StrapComponent, BuckleAttemptEvent>(OnBuckleAttempt);
    }

    private void OnUnstrapAttempt(EntityUid uid, NCVehicleComponent component, ref UnstrapAttemptEvent args)
    {
        // Allow self-unbuckle
        if (args.User == args.Buckle)
            return;

        if (args.User == null) return;

        // Allow unbuckle by anyone INSIDE the vehicle (Driver or Passengers)
        if (IsPassenger(uid, args.User.Value))
            return;

        // Prevent outsiders from unbuckling anyone (Driver or Passenger)
        args.Cancelled = true;

        if (args.Popup)
            _popup.PopupEntity("Вы не можете отстегнуть водителя снаружи!", uid, args.User.Value);
    }

    private bool IsPassenger(EntityUid vehicle, EntityUid user)
    {
        if (!TryComp<BuckleComponent>(user, out var buckle) || buckle.BuckledTo == null)
            return false;

        var strap = buckle.BuckledTo.Value;
        if (strap == vehicle) return true; // Driver/Main seat

        // Check if strap parent is vehicle (Passenger seats)
        if (Transform(strap).ParentUid == vehicle) return true;

        return false;
    }

    private void OnBuckleAttempt(EntityUid uid, StrapComponent component, ref BuckleAttemptEvent args)
    {
        // Enforce Lock
        if (TryComp<LockComponent>(uid, out var lockComp) && lockComp.Locked)
        {
            args.Cancelled = true;
            return;
        }

        // Check Parent (for seats)
        if (Transform(uid).ParentUid is { Valid: true } parent &&
            TryComp<LockComponent>(parent, out var parentLock) &&
            parentLock.Locked &&
            HasComp<NCVehicleComponent>(parent))
        {
            args.Cancelled = true;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<NCVehicleComponent, VehicleComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var ncVehicle, out var vehicle, out var mover))
        {
            if (!vehicle.EngineRunning)
                continue;

            // Check fuel
            if (!HasFuel(uid, ncVehicle))
            {
                vehicle.EngineRunning = false;
                _appearance.SetData(uid, VehicleState.Animated, false);
                _ambientSound.SetAmbience(uid, false);

                if (vehicle.Driver != null)
                {
                    RemComp<RelayInputMoverComponent>(vehicle.Driver.Value);
                    vehicle.Driver = null;
                }

                continue;
            }

            // Consume fuel
            var isMoving = (mover.HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;
            var consumption = isMoving
                ? ncVehicle.FuelConsumptionPerSecond
                : ncVehicle.IdleFuelConsumption;
            ConsumeFuel(uid, ncVehicle, consumption * frameTime);
        }
    }

    // ===== ACCESS CONTROL =====

    private void OnInsertAttempt(EntityUid uid, NCVehicleComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        // Do NOT return early for client (Enable prediction)
        // if (_net.IsClient) return;

        // Only care about the key slot
        if (TryComp<VehicleComponent>(uid, out var vehicle) && args.Slot.ID != vehicle.KeySlot)
            return;

        if (args.User == null || args.Item == EntityUid.Invalid)
            return;

        // Check Access
        if (!CheckAccess(uid, args.User.Value, args.Item))
        {
            // _popup.PopupEntity("Доступ запрещен!", uid, args.User.Value);
            // Popup handled by CheckAccess or user feedback?
            // Better not spam popups on prediction.
            args.Cancelled = true;
        }
    }

    // New: Prevent unauthorized usage of Keys/IDs (e.g. putting in trunk)
    private void OnInteractUsing(EntityUid uid, NCVehicleComponent component, InteractUsingEvent args)
    {
        // If already handled (e.g. by ItemSlots), ignore
        if (args.Handled)
            return;

        // If user has key access, allow everything
        if (HasKeyAccess(uid, args.User))
            return;

        // If no access, Block ID cards/Keys/PDAs from falling into the trunk
        if (HasComp<IdCardComponent>(args.Used) ||
            HasComp<VehicleKeyComponent>(args.Used) ||
            HasComp<PdaComponent>(args.Used))
        {
            // Block it
            args.Handled = true;
            _popup.PopupClient(Loc.GetString("Доступ запрещен!"), uid, args.User);
        }
    }

    private void OnLockAttempt(EntityUid uid, NCVehicleComponent component, ref LockToggleAttemptEvent args)
    {
        // Prevent strangers from using the native lock verb (or any direct lock toggle)
        if (!HasKeyAccess(uid, args.User))
        {
            args.Cancelled = true;
            if (!args.Silent)
                _popup.PopupClient(Loc.GetString("У вас нет ключей от этой машины!"), uid, args.User);
        }
    }

    private bool CheckAccess(EntityUid vehicle, EntityUid user, EntityUid key)
    {
        // 1. Check Owner Plate logic
        if (TryComp<NCVehicleComponent>(vehicle, out var nc) && !string.IsNullOrEmpty(nc.OwnerPlate))
        {
            // Check if KEY matches
            if (TryComp<VehicleKeyComponent>(key, out var keyComp) && keyComp.Plate == nc.OwnerPlate)
                return true;

            // Check if KEY is PDA containing ID with key?
            if (TryComp<PdaComponent>(key, out var pda) && pda.ContainedId != null)
            {
                if (TryComp<VehicleKeyComponent>(pda.ContainedId.Value, out var idKey) && idKey.Plate == nc.OwnerPlate)
                    return true;
            }

            return false;
        }

        // 2. Fallback to native AccessReader (e.g. for unowned vehicles or public ones)
        if (TryComp<AccessReaderComponent>(vehicle, out var accessReader))
        {
            // Note: Our personalized vehicles have "Syndicate" tag, so this will fail for civs.
            var tags = _access.FindAccessTags(key);
            return _access.AreAccessTagsAllowed(tags, accessReader);
        }

        return true; // No restrictions
    }

    // Check if user has ANY key in inventory matching the plate
    private bool HasKeyAccess(EntityUid vehicle, EntityUid user)
    {
        if (!TryComp<NCVehicleComponent>(vehicle, out var nc) || string.IsNullOrEmpty(nc.OwnerPlate))
            return true; // Unowned

        foreach (var item in _inventory.GetHandOrInventoryEntities(user))
        {
            if (TryComp<VehicleKeyComponent>(item, out var key) && key.Plate == nc.OwnerPlate)
                return true;

            if (TryComp<PdaComponent>(item, out var pda) && pda.ContainedId != null)
            {
                if (TryComp<VehicleKeyComponent>(pda.ContainedId.Value, out var idKey) && idKey.Plate == nc.OwnerPlate)
                    return true;
            }
        }
        return false;
    }

    // ===== FUEL SYSTEM =====

    private bool HasFuel(EntityUid uid, NCVehicleComponent component)
    {
        if (!_solution.TryGetSolution(uid, component.FuelSolution, out var solution))
            return false;
        return solution.Value.Comp.Solution.Volume > 0;
    }

    private void ConsumeFuel(EntityUid uid, NCVehicleComponent component, FixedPoint2 amount)
    {
        if (!_solution.TryGetSolution(uid, component.FuelSolution, out var solution))
            return;
        _solution.SplitSolution(solution.Value, amount);
    }

    // ===== COLLISION DAMAGE =====

    private void OnCollide(EntityUid uid, NCVehicleComponent component, ref StartCollideEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<VehicleComponent>(uid, out var vehicle) || !vehicle.EngineRunning)
            return;

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        float speed = physics.LinearVelocity.Length();
        if (speed < component.MinCollisionSpeed)
            return;

        var other = args.OtherEntity;
        float damageAmount = (speed - component.MinCollisionSpeed) * component.CollisionDamageMultiplier;

        // Damage target
        if (TryComp<DamageableComponent>(other, out _))
        {
            var damageSpec = new DamageSpecifier(_prototype.Index<DamageTypePrototype>("Blunt"), damageAmount);
            _damageable.TryChangeDamage(other, damageSpec, origin: uid);

            // Knockback (Throw + Stun) for mobs
            if (HasComp<MobStateComponent>(other) || HasComp<InputMoverComponent>(other)) // Better check for mobs
            {
                var direction = physics.LinearVelocity.Normalized();
                var throwForce = component.KnockbackMultiplier * (speed / 5.0f); // Scale speed relative to base 5

                _stun.TryKnockdown(other, TimeSpan.FromSeconds(3), true);
                _throwing.TryThrow(other, direction, throwForce, uid, 10.0f);
            }
        }

        // Damage vehicle itself
        if (TryComp<DamageableComponent>(uid, out _))
        {
            var vehicleDamage = new DamageSpecifier(_prototype.Index<DamageTypePrototype>("Blunt"), damageAmount * component.SelfDamageMultiplier);
            _damageable.TryChangeDamage(uid, vehicleDamage);
        }
    }
    private void OnMapInit(EntityUid uid, NCVehicleComponent component, MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        foreach (var offset in component.PassengerSlots)
        {
            var seat = Spawn("NCVehicleSeat", Transform(uid).Coordinates);
            var xform = Transform(seat);
            xform.AttachParent(uid);
            xform.LocalPosition = offset;
            component.SpawnedSeats.Add(seat);
        }
        Dirty(uid, component);
    }

    private void OnGetVerbs(EntityUid uid, NCVehicleComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // Locking Verb
        if (TryComp<LockComponent>(uid, out var lockComp))
        {
            // Only show if we have Key Access
            // NOTE: Optimization - maybe don't scan inventory on every frame?
            // Verbs are requested on right click, so scanning inventory is fine.
            if (HasKeyAccess(uid, args.User))
            {
                args.Verbs.Add(new AlternativeVerb
                {
                    Act = () =>
                    {
                        // Toggle Lock
                        if (lockComp.Locked)
                            _lock.Unlock(uid, args.User, lockComp);
                        else
                            _lock.Lock(uid, args.User, lockComp);
                    },
                    Text = lockComp.Locked ? Loc.GetString("Открыть дверь") : Loc.GetString("Закрыть дверь"),
                    Priority = 10,
                    Icon = new SpriteSpecifier.Texture(new ResPath(lockComp.Locked ? "/Textures/Interface/VerbIcons/unlock.svg.192dpi.png" : "/Textures/Interface/VerbIcons/lock.svg.192dpi.png"))
                });
            }
        }

        // Seat Verbs
        int index = 1;
        foreach (var seat in component.SpawnedSeats)
        {
            var i = index++;
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => _buckle.TryBuckle(args.User, args.User, seat),
                Text = $"Sit (Passenger {i})",
                Priority = 1
            });
        }
    }
}
