using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._NC.Trade;


public sealed partial class NcStoreLogicSystem : EntitySystem
{
    private static readonly ISawmill Sawmill = Logger.GetSawmill("ncstore-logic");
    private static readonly IComparer<string> OrdinalIds = new OrdinalIdComparer();

    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] public readonly NcStoreCurrencySystem _currency = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] public readonly IEntityManager _ents = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] public readonly NcStoreInventorySystem _inventory = default!;
    [Dependency] public readonly IPrototypeManager _protos = default!;
    [Dependency] public readonly SharedStackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeServices();
    }
    public void InvalidateInventoryCache(EntityUid root) => _inventory.InvalidateInventoryCache(root);

    public void QueuePickupToHandsOrCrateNextTick(EntityUid user, EntityUid spawned)
    {
        Timer.Spawn(0, () =>
        {
            if (!Exists(user) || !Exists(spawned))
                return;

            if (_ents.TryGetComponent(spawned, out TransformComponent? xform) && xform.ParentUid == user)
            {
                InvalidateInventoryCache(user);
                return;
            }

            var pickedUp = false;
            if (_ents.HasComponent<HandsComponent>(user))
                pickedUp = _hands.TryPickupAnyHand(user, spawned, false);

            if (pickedUp)
            {
                InvalidateInventoryCache(user);
                return;
            }

            if (TryGetPulledClosedCrate(user, out var crate) && Exists(crate))
            {
                _entityStorage.Insert(spawned, crate);
                InvalidateInventoryCache(crate);
            }

            InvalidateInventoryCache(user);
        });
    }

    public EntityUid? GetPulledClosedCrate(EntityUid user) =>
        TryGetPulledClosedCrate(user, out var crate) ? crate : null;

    public bool TryGetPulledClosedCrate(EntityUid user, out EntityUid crate)
    {
        crate = default;
        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var hand in hands.Hands.Values)
            {
                if (hand.HeldEntity is not { } held)
                    continue;
                if (TryComp<EntityStorageComponent>(held, out var storage) && !storage.Open)
                {
                    crate = held;
                    return true;
                }
            }
        }

        if (!TryComp(user, out PullerComponent? puller) || puller.Pulling is not { } pulled)
            return false;
        if (!TryComp<EntityStorageComponent>(pulled, out var pulledStorage) || pulledStorage.Open)
            return false;
        crate = pulled;
        return true;
    }

    private sealed class OrdinalIdComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
    }
}
