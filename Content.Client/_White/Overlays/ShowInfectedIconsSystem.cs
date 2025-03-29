using System.Linq;
using Content.Client.Overlays;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Overlays;

public sealed class ShowInfectedIconsSystem : EquipmentHudSystem<ShowInfectedIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlienInfectedComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, AlienInfectedComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (component.GrowthStage <= 6)
        {
            if (_prototype.TryIndex(component.InfectedIcons.ElementAt(component.GrowthStage), out var iconPrototype))
                ev.StatusIcons.Add(iconPrototype);
        }
        else
        {
            if (_prototype.TryIndex(component.InfectedIcons.ElementAt(6), out var iconPrototype))
                ev.StatusIcons.Add(iconPrototype);
        }

    }
}
