using Content.Shared._White.Body.Systems;
using Content.Shared._White.Medical.Wounds.Components.Wound;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._White.Medical.Wounds.Systems;

public abstract partial class SharedWoundSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] protected readonly DamageableSystem Damageable = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string WoundsContainerId = "wounds";

    public override void Initialize()
    {
        base.Initialize();

        InitializeBodyPart();
        InitializeWoundable();
    }
}

/// <summary>
/// Raised on wound after changing damage.
/// </summary>
public record struct WoundDamageChangedEvent(WoundComponent WoundComponent, FixedPoint2 OldDamage);

/// <summary>
/// Raised on woudable entity after changing the damage of its wound.
/// </summary>
public record struct WoundableDamageChangedEvent(DamageSpecifier DamageDelta);
