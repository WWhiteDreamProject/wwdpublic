using Content.Shared._White.Light.Components;
using Robust.Shared.Containers;

namespace Content.Shared._White.RotatePointLight;

public abstract class SharedRotatePointLightSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RotatePointLightComponent, ComponentInit>(CompInit);
        SubscribeLocalEvent<RotatePointLightComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RotatePointLightComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void CompInit(EntityUid uid, RotatePointLightComponent comp, ComponentInit args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;

        comp.Enabled = xform.GridUid == xform.ParentUid ||
                       xform.MapUid == xform.ParentUid;
        Dirty(uid, comp);
        UpdateRotation(uid, comp);
    }


    private void OnInserted(EntityUid uid, RotatePointLightComponent comp, EntGotInsertedIntoContainerMessage args)
    {
        comp.Enabled = false;
        Dirty(uid, comp);
        UpdateRotation(uid, comp);
    }

    private void OnRemoved(EntityUid uid, RotatePointLightComponent comp, EntGotRemovedFromContainerMessage args)
    {
        comp.Enabled = true;
        Dirty(uid, comp);
        UpdateRotation(uid, comp);
    }

    protected virtual void UpdateRotation(EntityUid uid, RotatePointLightComponent comp) { }

}
