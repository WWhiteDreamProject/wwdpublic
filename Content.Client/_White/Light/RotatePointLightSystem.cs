using Content.Shared._White.Light.Components;
using Robust.Client.GameObjects;

namespace Content.Client._White.Light;

/// <summary>
///  pointlight's rotation is not a datafield and is restricted by access attribute
///  Fuck robust toolbox, all my homies hate robust toolbox
/// </summary>
public sealed class RotatePointLightSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RotatePointLightComponent, ComponentStartup>(CompInit);
    }

    private void CompInit(EntityUid uid, RotatePointLightComponent comp, ComponentStartup args)
    {
        if (!TryComp<PointLightComponent>(uid, out var light))
            return;

#pragma warning disable RA0002
        light.Rotation = comp.Angle;
#pragma warning restore RA0002
    }
}
