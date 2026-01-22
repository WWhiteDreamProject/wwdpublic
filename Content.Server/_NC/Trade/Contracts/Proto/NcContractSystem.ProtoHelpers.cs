using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NC.Trade;

public sealed partial class NcContractSystem : EntitySystem
{
    private bool TryGetStackTypeId(string productProtoId, out string stackTypeId)
    {
        stackTypeId = string.Empty;

        if (!_prototypes.TryIndex<EntityPrototype>(productProtoId, out var expectedProto))
            return false;

        if (!expectedProto.TryGetComponent("Stack", out StackComponent? prodStackDef))
            return false;

        if (string.IsNullOrWhiteSpace(prodStackDef.StackTypeId))
            return false;

        stackTypeId = prodStackDef.StackTypeId;
        return true;
    }


    private List<string> GetAncestorsInclusive(string protoId)
    {
        if (_ancestorsCache.TryGetValue(protoId, out var list))
            return list;

        var result = new List<string> { protoId };

        if (_prototypes.TryIndex<EntityPrototype>(protoId, out var proto))
        {
            var parents0 = proto.Parents;
            if (parents0 is { Length: > 0 })
            {
                var stack = new Stack<string>(parents0);
                var seen = new HashSet<string>(StringComparer.Ordinal);

                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    if (!seen.Add(cur))
                        continue;

                    result.Add(cur);

                    if (_prototypes.TryIndex<EntityPrototype>(cur, out var p))
                    {
                        var parents = p.Parents;
                        if (parents is { Length: > 0 })
                        {
                            foreach (var t in parents)
                                stack.Push(t);
                        }
                    }
                }
            }
        }

        _ancestorsCache[protoId] = result;
        return result;
    }
}
