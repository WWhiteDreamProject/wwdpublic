using Content.Shared._White.Light.Components;
using Content.Shared._White.RotatePointLight;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client._White.Light;

/// <summary>
///  pointlight's rotation is not a datafield and is restricted by access attribute
///  Fuck robust toolbox, all my homies hate robust toolbox
/// </summary>
public sealed class RotatePointLightSystem : SharedRotatePointLightSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RotatePointLightComponent, ComponentStartup>(CompStartup);
        SubscribeLocalEvent<RotatePointLightComponent, AfterAutoHandleStateEvent>(AfterAutoState);
    }

    private void CompStartup(EntityUid uid, RotatePointLightComponent comp, ComponentStartup args)
    {
        UpdateRotation(uid, comp);
    }

    private void AfterAutoState(EntityUid uid, RotatePointLightComponent comp, AfterAutoHandleStateEvent args)
    {
        UpdateRotation(uid, comp);
    }

    protected override void UpdateRotation(EntityUid uid, RotatePointLightComponent comp)
    {
        if (!TryComp<PointLightComponent>(uid, out var light))
            return;

#pragma warning disable RA0002
        light.Rotation = comp.Enabled ? comp.Angle : 0;
#pragma warning restore RA0002
    }
}
