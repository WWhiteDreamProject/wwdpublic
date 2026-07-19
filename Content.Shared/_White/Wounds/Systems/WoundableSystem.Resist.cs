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
        args.Damage *= ent.Comp.Resistance;
    }

    private void OnWoundableSeverityChanged(Entity<WoundableResistComponent> ent, ref WoundableSeverityChangedEvent args)
    {
        var resistance = ent.Comp.Thresholds.GetValueOrDefault(args.Severity);
        if (ent.Comp.Resistance == resistance)
            return;

        ent.Comp.Resistance = resistance;
        Dirty(ent);
    }

    #endregion
}
