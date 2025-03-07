using Content.Server.Medical;
using Content.Shared._White.Xenomorphs.Systems;
using VomitActionComponent = Content.Shared._White.Xenomorphs.Components.VomitActionComponent;
using VomitActionEvent = Content.Shared._White.Xenomorphs.Systems.VomitActionEvent;


namespace Content.Server._White.Xenomorphs.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class VomitActionSystem : SharedVomitActionSystem
{
    /// <inheritdoc/>
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
