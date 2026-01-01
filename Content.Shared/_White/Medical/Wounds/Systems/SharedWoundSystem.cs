using Content.Shared._White.Body.Systems;
using Content.Shared._White.Medical.Wounds.Components;
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
        InitializeBone();
        InitializeOrgan();
        InitializeWoundable();
    }
}

/// <summary>
/// Raised on wound after changing damage.
/// </summary>
public sealed class WoundDamageChangedEvent(Entity<WoundComponent> wound, FixedPoint2 oldDamage) : HandledEntityEventArgs
{
    public readonly Entity<WoundComponent> Wound = wound;
    public readonly FixedPoint2 OldDamage = oldDamage;
}

/// <summary>
/// Raised on woudable entity after changing the damage of its wound.
/// </summary>
public record struct WoundableDamageChangedEvent(DamageSpecifier DamageDelta);

/// <summary>
/// Raised on bone entity after changing his health.
/// </summary>
public record struct BoneHealthChangedEvent(FixedPoint2 DamageDelta);

/// <summary>
/// Raised on bone entity after changing his state.
/// </summary>
public record struct BoneStatusChangedEvent(Entity<WoundableBoneComponent> Bone, BoneStatus BoneState);

/// <summary>
/// Raised on organ entity after changing his health.
/// </summary>
public record struct OrganHealthChangedEvent(FixedPoint2 DamageDelta);

/// <summary>
/// Raised on organ entity after changing his state.
/// </summary>
public record struct OrganStatusChangedEvent(Entity<WoundableOrganComponent> Organ, OrganStatus OrganStatus);
