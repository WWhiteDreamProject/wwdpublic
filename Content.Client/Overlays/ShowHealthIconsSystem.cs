using System.Linq;
using Content.Shared._White.Damage.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Shows a healthy icon on mobs.
/// </summary>
public sealed class ShowHealthIconsSystem : EquipmentHudSystem<ShowHealthIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;

    [ViewVariables]
    public HashSet<string> DamageContainers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<ShowHealthIconsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthIconsComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
        {
            DamageContainers.Add(damageContainerId);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        DamageContainers.Clear();
    }

    private void OnHandleState(Entity<ShowHealthIconsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    private void OnGetStatusIconsEvent(Entity<DamageableComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var healthIcons = DecideHealthIcons(entity);

        args.StatusIcons.AddRange(healthIcons);
    }

    private IReadOnlyList<HealthIconPrototype> DecideHealthIcons(Entity<DamageableComponent> entity)
    {
        var damageableComponent = entity.Comp;

        if (damageableComponent.Container == null || // WD EDIT
            !DamageContainers.Contains(damageableComponent.Container)) // WD EDIT
        {
            return Array.Empty<HealthIconPrototype>();
        }

        var result = new List<HealthIconPrototype>();

        // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
        if (damageableComponent?.Container == "Biological") // WD EDIT
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                /*// Since there is no MobState for a rotting mob, we have to deal with this case first.
                if (HasComp<RottingComponent>(entity) && _prototypeMan.TryIndex(damageableComponent.RottingIcon, out var rottingIcon))
                    result.Add(rottingIcon);
                else if (damageableComponent.HealthIcons.TryGetValue(state.CurrentState, out var value) && _prototypeMan.TryIndex(value, out var icon))
                    result.Add(icon); TODO */
            }
        }

        return result;
    }
}
