using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._White.Other;

[RegisterComponent]
public sealed partial class UprightOrbitComponent : Component;

public sealed class UprightOrbitSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<UprightOrbitComponent, StartedFollowingEntityEvent>(OnStartedFollowing);
    }

    private void OnStartedFollowing(EntityUid uid, UprightOrbitComponent comp, StartedFollowingEntityEvent args)
    {
        if (TryComp<OrbitVisualsComponent>(uid, out var orbit))
            orbit.KeepUpright = true;
    }
}
