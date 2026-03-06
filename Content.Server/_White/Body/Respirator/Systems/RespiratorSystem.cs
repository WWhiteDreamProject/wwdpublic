using Content.Server._White.Body.Respirator.Components;
using Content.Server._White.Body.Wound.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Atmos;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server._White.Body.Respirator.Systems;

[UsedImplicitly]
public sealed partial class RespiratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRespirator();
        InitializeConsumer();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateRespirators();
        UpdateConsumers();
    }
}


/// <summary>
/// Event raised after an entity changes his oxidant saturation level.
/// </summary>
public record struct AfterSaturationLevelChangedEvent(float SaturationLevel, float OldSaturationLevel);

/// <summary>
/// Event raised when an entity is about to take a breath.
/// </summary>
/// <param name="BreathVolume">The volume to breathe in.</param>
[ByRefEvent]
public record struct BeforeBreathEvent(float BreathVolume = 0f);

/// <summary>
/// Event raised when an entity is about to update respirator.
/// </summary>
[ByRefEvent]
public record struct BeforeUpdateRespiratorEvent(float SaturationConsumption = 0f);

/// <summary>
/// An event raised to inhalation handlers that asks them nicely to simulate what it would be like to metabolize
/// a given volume of gas, without actually metabolizing it.
/// </summary>
/// <param name="Gas">The gas mixture we are testing.</param>
/// <param name="Toxic">Whether the gas returns as toxic to any respirator.</param>
/// <param name="Saturation">The amount of saturation we got from the gas.</param>
[ByRefEvent]
public record struct CanMetabolizeGasEvent(GasMixture Gas, bool Toxic = false, float Saturation = 0f);

/// <summary>
/// Event raised when an entity is exhaling
/// </summary>
/// <param name="Gas">The gas mixture we're exhaling into.</param>
/// <param name="Handled">Whether we have successfully exhaled or not.</param>
[ByRefEvent]
public record struct ExhaledGasEvent(GasMixture Gas);

/// <summary>
/// Event raised when an entity first tries to exhale a gas, determines where the gas they're exhaling will be sent.
/// </summary>
/// <param name="Gas">The gas mixture that the exhaled gas will be merged into.</param>
[ByRefEvent]
public record struct ExhaleLocationEvent(GasMixture? Gas);

/// <summary>
/// Event raised when an entity successfully inhales a gas, attempts to find a place to put the gas.
/// </summary>
/// <param name="Gas">The gas we're inhaling.</param>
/// <param name="Handled">Whether a system has responded appropriately.</param>
/// <param name="Succeeded">Whether we successfully managed to inhale the gas</param>
[ByRefEvent]
public record struct InhaledGasEvent(GasMixture Gas, bool Succeeded = false);

/// <summary>
/// Event raised when an entity first tries to inhale that returns a GasMixture from a given location.
/// </summary>
/// <param name="Gas">The gas that gets returned, null if there is none.</param>
/// <param name="Respirator">The Respirator component of the entity attempting to inhale</param>
[ByRefEvent]
public record struct InhaleLocationEvent(GasMixture? Gas, RespiratorComponent Respirator);

/// <summary>
/// Raised when an entity that was suffocating stops suffocating.
/// </summary>
[ByRefEvent]
public record struct StopSuffocatingEvent;

/// <summary>
/// Raised when an entity starts suffocating and when suffocation progresses.
/// </summary>
[ByRefEvent]
public record struct SuffocationEvent;
