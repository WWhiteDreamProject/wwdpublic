using System.Linq;
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

    private void OnGetStatusIconsEvent(EntityUid uid, MindSlaveComponent component, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var mindSlaveIcon = MindSlaveIcon(uid, component);
        args.StatusIcons.AddRange(mindSlaveIcon);
    }

    private IEnumerable<StatusIconPrototype> MindSlaveIcon(EntityUid uid, MindSlaveComponent mindSlave)
    {
        var result = new List<FactionIconPrototype>();

        if (TryComp(_player.LocalEntity, out MindSlaveComponent? ownerMindSlave))
        {
            var netUid = GetNetEntity(uid);
            if (ownerMindSlave.Master == netUid && _prototype.TryIndex<FactionIconPrototype>(ownerMindSlave.MasterStatusIcon, out var masterIcon))
                result.Add(masterIcon);

            if (ownerMindSlave.Slaves.Contains(netUid) && _prototype.TryIndex<FactionIconPrototype>(ownerMindSlave.MasterStatusIcon, out var slaveIcon))
                result.Add(slaveIcon);
        }
        else
        {
            if (mindSlave.Slaves.Any() && _prototype.TryIndex<FactionIconPrototype>(mindSlave.MasterStatusIcon, out var masterIcon))
                result.Add(masterIcon);

            if (mindSlave.Master.HasValue && _prototype.TryIndex<FactionIconPrototype>(mindSlave.MasterStatusIcon, out var slaveIcon))
                result.Add(slaveIcon);
        }

        return result;
    }
}
