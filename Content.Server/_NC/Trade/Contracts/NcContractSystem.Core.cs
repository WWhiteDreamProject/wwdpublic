using Content.Shared._NC.Trade;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;


namespace Content.Server._NC.Trade;


public sealed partial class NcContractSystem : EntitySystem
{
    private const double Golden = 0.6180339887498948;
    private const double DefaultJitter = 0.06;
    private const int MaxRewardDepth = 6;
    private const int DepthInProgress = -1;
    private static readonly ISawmill Sawmill = Logger.GetSawmill("nccontracts");
    private readonly Dictionary<string, List<string>> _ancestorsCache = new(StringComparer.Ordinal);
    private readonly Dictionary<(EntityUid Store, string Difficulty), CooldownState> _contractCooldown = new();
    private readonly Dictionary<string, int> _depthCache = new(StringComparer.Ordinal);
    [Dependency] private readonly NcStoreInventorySystem _inventory = default!;
    [Dependency] private readonly NcStoreLogicSystem _logic = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    private readonly Dictionary<QuasiKey, double> _quasiPhase = new();
    [Dependency] private readonly IRobustRandom _random = default!;
    private readonly List<EntityUid> _scratchCrateItems = new();
    private readonly List<EntityUid> _scratchUserItems = new();
    private readonly Dictionary<QuasiKey, SmallBagState> _smallBags = new();

    public override void Initialize()
    {
        base.Initialize();
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        _prototypes.PrototypesReloaded -= OnPrototypesReloaded;
        base.Shutdown();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev) => ClearCaches();

    private void ClearCaches()
    {
        _ancestorsCache.Clear();
        _depthCache.Clear();

        _quasiPhase.Clear();
        _smallBags.Clear();

        _contractCooldown.Clear();
    }

    private static List<ContractTargetServerData> GetEffectiveTargets(ContractServerData contract) => contract.Targets;

    private int GetProtoDepth(string protoId)
    {
        if (_depthCache.TryGetValue(protoId, out var cached))
            return cached >= 0 ? cached : 0;

        if (!_prototypes.TryIndex<EntityPrototype>(protoId, out var proto))
        {
            _depthCache[protoId] = 0;
            return 0;
        }

        _depthCache[protoId] = DepthInProgress;

        var best = 0;
        var parents = proto.Parents;

        if (parents is { Length: > 0, })
        {
            foreach (var parentId in parents)
            {
                var depth = GetProtoDepth(parentId) + 1;
                if (depth > best)
                    best = depth;
            }
        }

        _depthCache[protoId] = best;
        return best;
    }

    private sealed class SmallBagState
    {
        public readonly List<int> Order = new();
        public int Cursor;
        public int LastIdx = -1;
        public int Max;
        public int Min;
    }

    private sealed class CooldownState
    {
        public readonly Dictionary<string, int> Counts = new(StringComparer.Ordinal);
        public readonly Queue<string> Queue = new();

        public int Limit;

        public bool Contains(string id) => Limit > 0 && Counts.ContainsKey(id);

        public void TrimToLimit()
        {
            if (Limit <= 0)
            {
                Queue.Clear();
                Counts.Clear();
                return;
            }

            while (Queue.Count > Limit)
            {
                var old = Queue.Dequeue();

                if (!Counts.TryGetValue(old, out var c))
                    continue;

                c--;
                if (c <= 0)
                    Counts.Remove(old);
                else
                    Counts[old] = c;
            }
        }

        public void Push(string id)
        {
            if (Limit <= 0 || string.IsNullOrWhiteSpace(id))
                return;

            Queue.Enqueue(id);

            Counts.TryGetValue(id, out var c);
            Counts[id] = c + 1;

            TrimToLimit();
        }
    }
}
