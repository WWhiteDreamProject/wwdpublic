using Content.Server.Popups;
using Content.Server.DeltaV.Weapons.Ranged.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Item;
using Content.Shared.DeltaV.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._White.Guns;

// Basically everything in the file was touched by me so no point in WWDP Edit ig
namespace Content.Server.DeltaV.Weapons.Ranged.Systems;

public sealed class EnergyGunSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyGunComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EnergyGunComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<EnergyGunComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<EnergyGunComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<EnergyGunComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.FireModes.Count < 2 ||
            entity.Comp.CurrentFireMode?.Prototype == null ||
            !_prototypeManager.TryIndex<EntityPrototype>(entity.Comp.CurrentFireMode.Prototype, out var proto))
            return;

        var fireMode = "battery-fire-mode-" + entity.Comp.CurrentFireMode.Name;
        var mode = Loc.GetString(fireMode);

        if (entity.Comp.CurrentFireMode.Name == string.Empty)
            mode = proto.Name;

        var color = entity.Comp.CurrentFireMode.Name switch
        {
            "disable" => "lightblue",
            "ion" => "blue",
            _ => "crimson"
        };

        args.PushMarkup(Loc.GetString("energygun-examine-fire-mode", ("mode", mode), ("color", color)));
    }

    private void OnGetVerb(Entity<EnergyGunComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || entity.Comp.FireModes.Count < 2)
            return;

        var user = args.User;
        foreach (var fireMode in entity.Comp.FireModes)
        {
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = entProto.Name,
                Disabled = fireMode == entity.Comp.CurrentFireMode,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(entity, fireMode, user);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnComponentStartup(Entity<EnergyGunComponent> entity, ref ComponentStartup args)
    {
        if (entity.Comp.FireModes.Count == 0)
            Del(entity); // we can't have energy gun with 0 fire mods
        else
            SetFireMode(entity, entity.Comp.FireModes[0]);
    }

    private void OnInteractHandEvent(Entity<EnergyGunComponent> entity, ref ActivateInWorldEvent args)
    {
        if (entity.Comp.FireModes.Count >= 2)
            CycleFireMode(entity, args.User);
    }

    private void CycleFireMode(Entity<EnergyGunComponent> entity, EntityUid user)
    {
        var index = entity.Comp.CurrentFireMode != null
            ? Math.Max(entity.Comp.FireModes.IndexOf(entity.Comp.CurrentFireMode), 0) + 1
            : 1;

        var fireMode = index >= entity.Comp.FireModes.Count
            ? entity.Comp.FireModes[0]
            : entity.Comp.FireModes[index];

        SetFireMode(entity, fireMode, user);
    }

    private void SetFireMode(Entity<EnergyGunComponent> entity, EnergyWeaponFireMode? fireMode, EntityUid? user = null)
    {
        if (fireMode?.Prototype == null)
            return;

        entity.Comp.CurrentFireMode = fireMode;

        if (!TryComp(entity, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider) ||
            !_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
            return;

        projectileBatteryAmmoProvider.Prototype = fireMode.Prototype;
        projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;
        if (TryComp<GunFluxComponent>(entity, out var overheat))
        {
            overheat.HeatCost = fireMode.HeatCost;
            Dirty(entity, overheat);
        }

        _gun.UpdateShots(entity, projectileBatteryAmmoProvider);

        if (user != null)
        {
            var fireModeName = "battery-fire-mode-" + fireMode.Name;
            var mode = Loc.GetString(fireModeName);

            if (fireMode.Name == string.Empty)
                mode = prototype.Name;

            _popupSystem.PopupEntity(Loc.GetString("gun-set-fire-mode", ("mode", mode)), entity, user.Value);
        }

        if (entity.Comp.CurrentFireMode.State == string.Empty ||
            !HasComp<AppearanceComponent>(entity) ||
            !TryComp<ItemComponent>(entity, out var item))
            return;

        _item.SetHeldPrefix(entity, entity.Comp.CurrentFireMode.State, false, item);
        switch (entity.Comp.CurrentFireMode.State)
        {
            case "disabler":
                _appearance.SetData(entity, EnergyGunFireModeVisuals.State, EnergyGunFireModeState.Disabler);
                break;
            case "lethal":
                _appearance.SetData(entity, EnergyGunFireModeVisuals.State, EnergyGunFireModeState.Lethal);
                break;
            case "special":
                _appearance.SetData(entity, EnergyGunFireModeVisuals.State, EnergyGunFireModeState.Special);
                break;
        }
    }
}
