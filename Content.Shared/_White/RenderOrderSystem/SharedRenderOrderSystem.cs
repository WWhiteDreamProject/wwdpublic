using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._White.RenderOrderSystem;


public abstract class SharedRenderOrderSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RenderOrderComponent, ComponentStartup>(DrawOrderComponentStartup);
    }

    public void MoveToTop(EntityUid uid) => SetRenderOrder(uid, EntityManager.CurrentTick.Value, "default");
    public void MoveToTop(EntityUid uid, string key) => SetRenderOrder(uid, EntityManager.CurrentTick.Value, key);

    public void SetRenderOrder(EntityUid uid, uint value) => SetRenderOrder(uid, value, "default");
    public void SetRenderOrder(EntityUid uid, uint value, string key)
    {
        var comp = EnsureComp<RenderOrderComponent>(uid);

        if (comp.ValueOrder.Remove(key))
            DebugTools.Assert(comp.Values.Remove(key), $"Had key \"{key}\" in comp.ValueOrder but not in comp.Values");

        comp.Values[key] = value;
        comp.ValueOrder.Add(key);

        UpdateRenderOrder(uid, comp);
    }
    public void UnsetRenderOrder(EntityUid uid) => UnsetRenderOrder(uid, "default");
    public void UnsetRenderOrder(EntityUid uid, string key)
    {
        if (!TryComp<RenderOrderComponent>(uid, out var comp))
            return;

        var dontcryiam = comp.Values.Remove(key);
        var justafish = comp.ValueOrder.Remove(key);
        DebugTools.Assert( dontcryiam == justafish, $"{(dontcryiam ? "Removed": "Did not remove")} key from comp.Values: but {(justafish ? "removed" : "did not remove")} same key from comp.ValueOrder.");
        UpdateRenderOrder(uid, comp);
    }

    protected virtual void UpdateRenderOrder(EntityUid uid, RenderOrderComponent comp)
    {
        Dirty(uid, comp);
    }

    protected virtual void DrawOrderComponentStartup(EntityUid uid, RenderOrderComponent comp, ComponentStartup args)
    {
        UpdateRenderOrder(uid, comp);
    }
}


[RegisterComponent]
[AutoGenerateComponentState(true), NetworkedComponent]
public sealed partial class RenderOrderComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, uint> Values = new();

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<string> ValueOrder = new();
}
