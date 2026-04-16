using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.DollyMixture;

public abstract class SharedDollyMixtureSystem : EntitySystem
{
    public virtual void Apply3D(Entity<DollyMixtureComponent?> entity, string rsiPath, string? statePrefix = null, Vector2? layerOffset = null)
    {
        entity.Comp ??= EnsureComp<DollyMixtureComponent>(entity);

        entity.Comp.RSIPath = rsiPath;
        Dirty(entity);
    }

    public virtual void Remove3D(Entity<DollyMixtureComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.RSIPath = null;
        Dirty(entity);
    }
}

