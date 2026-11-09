using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._White.Atmos.Systems;

public sealed class BreathToolSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BreathToolComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BreathToolComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<BreathToolComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<BreathToolComponent, ItemMaskToggledEvent>(OnMaskToggled);
    }

    private void OnInit(Entity<BreathToolComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;

        comp.IsFunctional = true;

        if (!_inventory.TryGetContainingEntity(ent.Owner, out var parent)
            || !_inventory.TryGetContainingSlot(ent.Owner, out var slot)
            || (slot.SlotFlags & comp.AllowedSlots) == 0
            || !TryComp(parent, out InternalsComponent? internals))
            return;

        ent.Comp.ConnectedInternalsEntity = parent;
        _internals.ConnectBreathTool((parent.Value, internals), ent);
    }

    private void OnGotUnequipped(Entity<BreathToolComponent> ent, ref GotUnequippedEvent args)
    {
        _atmos.DisconnectInternals(ent);
    }

    private void OnGotEquipped(Entity<BreathToolComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ent.Comp.AllowedSlots) == 0)
            return;

        ent.Comp.IsFunctional = true;

        if (TryComp(args.Equipee, out InternalsComponent? internals))
        {
            ent.Comp.ConnectedInternalsEntity = args.Equipee;
            _internals.ConnectBreathTool((args.Equipee, internals), ent);
        }
    }

    private void OnMaskToggled(Entity<BreathToolComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (args.IsToggled || args.IsEquip)
        {
            _atmos.DisconnectInternals(ent);
            return;
        }

        ent.Comp.IsFunctional = true;

        if (!TryComp(args.Wearer, out InternalsComponent? internals))
            return;

        ent.Comp.ConnectedInternalsEntity = args.Wearer;
        _internals.ConnectBreathTool((args.Wearer, internals), ent);
    }
}
