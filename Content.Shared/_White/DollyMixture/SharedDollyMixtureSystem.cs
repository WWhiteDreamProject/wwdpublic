using System.Numerics;

namespace Content.Shared._White.DollyMixture;

public abstract class SharedDollyMixtureSystem : EntitySystem
{
    public virtual void Apply3D(EntityUid uid, string RsiPath, string? statePrefix = null, Vector2? layerOffset = null, DollyMixtureComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.RSIPath = RsiPath;
        Dirty(uid, comp);
    }

    public virtual void Remove3D(EntityUid uid, DollyMixtureComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.RSIPath = null;
        Dirty(uid, comp);
    }
}

