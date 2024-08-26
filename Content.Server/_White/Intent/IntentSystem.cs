using Content.Server.NPC.HTN;
using Content.Shared._White.Intent;

namespace Content.Server._White.Intent;

public sealed class IntentSystem : SharedIntentSystem
{
    protected override bool IsNpc(EntityUid uid)
    {
        return HasComp<HTNComponent>(uid);
    }
}
