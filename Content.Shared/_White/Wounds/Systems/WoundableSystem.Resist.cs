using Content.Shared._White.Damage.Systems;
using Content.Shared._White.Wounds.Components;

namespace Content.Shared._White.Wounds.Systems;

public sealed partial class WoundableSystem
{
    private void InitializeResist()
    {
        SubscribeLocalEvent<WoundableResistComponent, GetModifiedDamageEvent>(OnGetModifiedDamage);
        SubscribeLocalEvent<WoundableResistComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
    }

    #region Event Handling

    private void OnGetModifiedDamage(Entity<WoundableResistComponent> ent, ref GetModifiedDamageEvent args)
    {
        args.Result *= ent.Comp.Resistance;
    }

    private void OnWoundSeverityChanged(Entity<WoundableResistComponent> ent, ref WoundSeverityChangedEvent args)
    {
        var resistance = ent.Comp.Thresholds.GetValueOrDefault(args.Severity);
        if (ent.Comp.Resistance == resistance)
            return;

        ent.Comp.Resistance = resistance;
        Dirty(ent);
    }

    #endregion
}
