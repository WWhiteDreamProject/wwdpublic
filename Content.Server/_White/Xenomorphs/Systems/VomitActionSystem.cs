using Content.Server.Medical;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared._White.Xenomorphs.Systems;

namespace Content.Server._White.Xenomorphs.Systems;

public sealed class VomitActionSystem : SharedVomitActionSystem
{
    [Dependency] private readonly VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VomitActionComponent, VomitActionEvent>(OnVomitAction);
    }

    protected void OnVomitAction(EntityUid uid, VomitActionComponent component, VomitActionEvent args)
    {
        _vomit.Vomit(uid, component.ThirstAdded, component.HungerAdded);
        ContainerSystem.EmptyContainer(component.Stomach, true);
    }
}
