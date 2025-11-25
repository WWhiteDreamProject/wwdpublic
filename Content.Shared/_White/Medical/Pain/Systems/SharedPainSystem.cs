using Content.Shared._White.Medical.Pain.Components;
using Content.Shared._White.Medical.Wounds.Systems;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._White.Medical.Pain.Systems;

public abstract partial class SharedPainSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedWoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBodyPart();
        InitializePainful();
        InitializeThresholds();
    }
}

/// <summary>
/// Raised on an entity after its pain has changed.
/// </summary>
public record struct AfterPainChangedEvent(EntityUid Target, FixedPoint2 CurrentPain, FixedPoint2 OldPain);

/// <summary>
/// Raised on an entity to get the sum total of pain.
/// </summary>
[ByRefEvent]
public record struct GetPainEvent(Entity<PainfulComponent> Painful, FixedPoint2 Pain);
