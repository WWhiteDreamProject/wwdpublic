using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._White.BloodCult.BloodCultist;

public sealed class BloodCultistSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, GetStatusIconsEvent>(GeBloodCultistIcon);
        SubscribeLocalEvent<BloodCultistLeaderComponent, GetStatusIconsEvent>(GeBloodCultistLeaderIcon);
    }

    private void GeBloodCultistIcon(Entity<BloodCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<BloodCultistLeaderComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GeBloodCultistLeaderIcon(Entity<BloodCultistLeaderComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
