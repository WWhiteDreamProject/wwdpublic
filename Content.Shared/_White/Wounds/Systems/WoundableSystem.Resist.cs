using Content.Shared._White.Wounds.Components;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeResist()
    {
        SubscribeLocalEvent<WoundableResistComponent, GetWoundableResistanceEvent>(OnGetWoundableResistance);
        SubscribeLocalEvent<WoundableResistComponent, WoundableSeverityChangedEvent>(OnWoundableSeverityChanged);
    }

    #region Event Handling

    private void OnGetWoundableResistance(Entity<WoundableResistComponent> ent, ref GetWoundableResistanceEvent args)
    {
        args.Damage *= ent.Comp.CurrentResistance;
    }

    private void OnWoundableSeverityChanged(Entity<WoundableResistComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        var resistance = ent.Comp.Thresholds.GetValueOrDefault(args.Severity);
        if (ent.Comp.CurrentResistance == resistance)
            return;

        ent.Comp.CurrentResistance = resistance;
        Dirty(ent);
    }

    #endregion
}
