using System.Numerics;
using Content.Server.Body.Components;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Gibbing;
using Robust.Shared.Audio;

namespace Content.Server._White.Gibbing;

public sealed class GibbingSystem : SharedGibbingSystem
{
    public override HashSet<EntityUid> GibBody(
        Entity<BodyComponent?, GibbableComponent?> body,
        bool gibOrgans = false,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null
    )
    {
        if (!Resolve(body, ref body.Comp1, logMissing: false)
            || TerminatingOrDeleted(body)
            || EntityManager.IsQueuedForDeletion(body))
            return new HashSet<EntityUid>();

        var xform = Transform(body);
        if (xform.MapUid is null)
            return new HashSet<EntityUid>();

        var gibs = base.GibBody(
            body,
            gibOrgans,
            splatDirection: splatDirection,
            splatModifier: splatModifier,
            splatCone:splatCone);

        var ev = new BeingGibbedEvent(gibs);
        RaiseLocalEvent(body, ref ev);

        QueueDel(body);

        return gibs;
    }
}
