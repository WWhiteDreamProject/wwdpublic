using System.Numerics;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Vehicle;

/// <summary>
///     Custom vehicle functionality for Night City: Fuel, collision damage, etc.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NCVehicleComponent : Component
{
    // ===== FUEL SYSTEM =====

    /// <summary>
    /// Name of the solution container for fuel.
    /// </summary>
    [DataField("fuelSolution"), AutoNetworkedField]
    public string FuelSolution = "fuel";

    /// <summary>
    /// How much reagent is consumed per second while moving.
    /// </summary>
    [DataField("fuelConsumptionPerSecond"), AutoNetworkedField]
    public float FuelConsumptionPerSecond = 0.1f;

    /// <summary>
    /// How much reagent is consumed per second while engine is on but idle.
    /// </summary>
    [DataField("idleFuelConsumption"), AutoNetworkedField]
    public float IdleFuelConsumption = 0.01f;

    // ===== COLLISION DAMAGE =====

    [DataField("collisionDamageMultiplier"), AutoNetworkedField]
    public float CollisionDamageMultiplier = 1.0f;

    [DataField("minCollisionSpeed"), AutoNetworkedField]
    public float MinCollisionSpeed = 3.0f;

    [DataField("selfDamageMultiplier"), AutoNetworkedField]
    public float SelfDamageMultiplier = 0.5f;

    [DataField("knockbackMultiplier"), AutoNetworkedField]
    public float KnockbackMultiplier = 1.0f;

    // ===== PASSENGER SEATS =====

    /// <summary>
    /// List of local offsets where passenger seats should be spawned.
    /// </summary>
    [DataField("passengerSlots"), AutoNetworkedField]
    public List<Vector2> PassengerSlots = new();

    /// <summary>
    /// List of spawned seat entities (server-side only tracking, but networked for clients if needed).
    /// </summary>
    [DataField("spawnedSeats"), AutoNetworkedField]
    public List<EntityUid> SpawnedSeats = new();

    /// <summary>
    /// Whether to hide entities when they buckle to this vehicle (and its seats).
    /// </summary>
    [DataField("hidePassengers"), AutoNetworkedField]
    public bool HidePassengers = true;

    [DataField, AutoNetworkedField]
    public string? OwnerPlate;

    [DataField, AutoNetworkedField]
    public string? OwnerName;

    [DataField("toggleLockEvent")]
    public object? ToggleVehicleLockEvent;
}

[DataDefinition]
public sealed partial class ToggleVehicleLockEvent : InstantActionEvent { }
