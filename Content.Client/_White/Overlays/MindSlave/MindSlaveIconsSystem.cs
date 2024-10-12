using Content.Client.Overlays;
using Content.Shared._White.Implants.MindSlave;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Overlays.MindSlave;

public sealed class MindSlaveIconsSystem : EquipmentHudSystem<MindSlaveComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(
        EntityUid uid,
        MindSlaveComponent mindSlaveComponent,
        ref GetStatusIconsEvent args)
    {
        if (!IsActive
            || args.InContainer
            || !TryComp(_player.LocalEntity, out MindSlaveComponent? ownerMindSlave))
            return;

        var mindSlaveIcon = MindslaveIcon(uid, ownerMindSlave);
        args.StatusIcons.AddRange(mindSlaveIcon);
    }

    private IEnumerable<StatusIconPrototype> MindslaveIcon(EntityUid uid, MindSlaveComponent mindSlave)
    {
        var result = new List<StatusIconPrototype>();
        string iconType;
        var netUid = GetNetEntity(uid);

        if (mindSlave.Master == netUid)
        {
            iconType = mindSlave.MasterStatusIcon;
        }
        else if (mindSlave.Slaves.Contains(netUid))
        {
            iconType = mindSlave.SlaveStatusIcon;
        }
        else
        {
            return result;
        }

        if (_prototype.TryIndex<StatusIconPrototype>(iconType, out var mindslaveIcon))
            result.Add(mindslaveIcon);

        return result;
    }
}
