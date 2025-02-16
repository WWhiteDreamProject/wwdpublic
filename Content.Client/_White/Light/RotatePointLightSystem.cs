using Content.Client.Items;
using Content.Client.Light.Components;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using Content.Client.Light.EntitySystems;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using static Content.Shared.Fax.AdminFaxEuiMsg;

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
