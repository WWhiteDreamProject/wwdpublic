using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!; // WWDP
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!; // WWDP


    protected virtual void InitializeBallistic()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentInit>(OnBallisticInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, MapInitEvent>(OnBallisticMapInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, TakeAmmoEvent>(OnBallisticTakeAmmo);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ForceSpawnAmmoEvent>(OnForceSpawnAmmo); // WD EDIT

        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetAmmoCountEvent>(OnBallisticAmmoCount);

        SubscribeLocalEvent<BallisticAmmoProviderComponent, ExaminedEvent>(OnBallisticExamine);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<Verb>>(OnBallisticVerb);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<InteractionVerb>>(AddInteractionVerb); // WWDP
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerb); // WWDP
        SubscribeLocalEvent<BallisticAmmoProviderComponent, InteractUsingEvent>(OnBallisticInteractUsing);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AfterInteractEvent>(OnBallisticAfterInteract);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AmmoFillDoAfterEvent>(OnBallisticAmmoFillDoAfter);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, UseInHandEvent>(OnBallisticUse);
    }

    private void OnBallisticUse(EntityUid uid, BallisticAmmoProviderComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User);
        args.Handled = true;
    }

    private void OnBallisticInteractUsing(EntityUid uid, BallisticAmmoProviderComponent component, InteractUsingEvent args)
    {
        if (args.Handled
            || _whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Used)
            || GetBallisticShots(component) >= component.Capacity)
            return;

        if (_whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Used))
            return;
        // WWDP EDIT
        if (HasComp<BallisticAmmoProviderComponent>(args.Used)) // Ammo providers use the doafter
            return;

        if (GetBallisticShots(component) >= component.Capacity)
        {
            Popup(Loc.GetString("gun-ballistic-full"), uid, args.User);
            return;
        }
        // WWDP EDIT END

        // WD EDIT START
        var entity = args.Used;
        var doInsert = true;
        if (TryComp(args.Used, out StackComponent? stack) && stack.Count > 1)
        {
            entity = GetStackEntity(args.Used, stack);
            doInsert = false;
        }

        component.Entities.Add(entity);
        if (_netManager.IsServer || doInsert)
            Containers.Insert(entity, component.Container);
        // WD EDIT END
        // Not predicted so
        Audio.PlayPredicted(component.SoundInsert, uid, args.User);
        args.Handled = true;
        component.Cycled = true;
        UpdateAmmoCount(uid);
        UpdateBallisticAppearance(uid, component);
        Dirty(uid, component);
    }

    private void OnBallisticAfterInteract(EntityUid uid, BallisticAmmoProviderComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !component.MayTransfer || !Timing.IsFirstTimePredicted
            || args.Target is null || args.Used == args.Target
            || Deleted(args.Target)
            || !TryComp(args.Target, out BallisticAmmoProviderComponent? targetComponent)
            || targetComponent.Whitelist is null)
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.FillDelay, new AmmoFillDoAfterEvent(), used: uid, target: args.Target, eventTarget: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            NeedHand = true
        });
    }

    private void OnBallisticAmmoFillDoAfter(EntityUid uid, BallisticAmmoProviderComponent component, AmmoFillDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled) // WWDP
            return;

        if (Deleted(args.Target)
            || !TryComp(args.Target, out BallisticAmmoProviderComponent? target)
            || target.Whitelist is null)
            return;

        if (target.Entities.Count + target.UnspawnedCount == target.Capacity)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-target-full",
                    ("entity", args.Target)),
                args.Target,
                args.User);
            return;
        }

        if (component.Entities.Count + component.UnspawnedCount == 0)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-empty",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }

        void SimulateInsertAmmo(EntityUid ammo, EntityUid ammoProvider, EntityCoordinates coordinates)
        {
            var evInsert = new InteractUsingEvent(args.User, ammo, ammoProvider, coordinates);
            RaiseLocalEvent(ammoProvider, evInsert);
        }

        List<(EntityUid? Entity, IShootable Shootable)> ammo = new();
        var evTakeAmmo = new TakeAmmoEvent(1, ammo, Transform(uid).Coordinates, args.User);
        RaiseLocalEvent(uid, evTakeAmmo);

        foreach (var (ent, _) in ammo)
        {
            if (ent == null)
                continue;

            if (_whitelistSystem.IsWhitelistFail(target.Whitelist, ent.Value))
            {
                Popup(
                    Loc.GetString("gun-ballistic-transfer-invalid",
                        ("ammoEntity", ent.Value),
                        ("targetEntity", args.Target.Value)),
                    uid,
                    args.User);

                SimulateInsertAmmo(ent.Value, uid, Transform(uid).Coordinates);
            }
            else
            {
                // play sound to be cool
                Audio.PlayPredicted(component.SoundInsert, uid, args.User);
                SimulateInsertAmmo(ent.Value, args.Target.Value, Transform(args.Target.Value).Coordinates);
                component.Cycled = true; // Make sure when loading shells in shotguns, that the first round is chambered.
            }

            if (IsClientSide(ent.Value))
                Del(ent.Value);
        }

        // repeat if there is more space in the target and more ammo to fill it
        var moreSpace = target.Entities.Count + target.UnspawnedCount < target.Capacity;
        var moreAmmo = component.Entities.Count + component.UnspawnedCount > 0;
        args.Repeat = moreSpace && moreAmmo;
    }

    private void AddInteractionVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<InteractionVerb> args) // WWDP
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null) // WWDP
            return;

        if (component.Cycleable)
        {
            args.Verbs.Add(new InteractionVerb() // WWDP
            {
                Text = Loc.GetString("gun-ballistic-cycle"),
                Disabled = GetBallisticShots(component) == 0,
                Act = () => ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User),
            });

        }
    }

    // WWDP edit alt-verb to extract ammunition
    private void AddAlternativeVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text =  Loc.GetString("gun-ballistic-extract"),
            Disabled = GetBallisticShots(component) == 0,
            Act = () => ExtractAction(uid, Transform(uid).MapPosition, component, args.User),
        });
    }
    // WWDP edit end

    private void OnBallisticVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !component.Cycleable)
            return;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("gun-ballistic-cycle"),
            Disabled = GetBallisticShots(component) == 0,
            Act = () => ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User),
        });
    }

    private void OnBallisticExamine(EntityUid uid, BallisticAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // WWDP edit; better examine, no ammo count on guns

        if (!HasComp<GunComponent>(uid)) // Magazines & Ammo boxes
        {
            args.PushMarkup(Loc.GetString("gun-ammocount-examine", ("color", AmmoExamineColor), ("count", GetBallisticShots(component))));

            // Show top round
            if (component.Entities.Count > 0)
            {
                var round = Name(component.Entities[-1]);

                args.PushMarkup(
                    Loc.GetString("ammo-top-round-examine", ("color", ModeExamineColor), ("round", round)));
            }
            else if (component.UnspawnedCount > 0)
            {
                if (!_proto.TryIndex(component.Proto, out var round))
                    return;

                args.PushMarkup(
                    Loc.GetString("ammo-top-round-examine", ("color", ModeExamineColor), ("round", round.Name)));
            }

            return;
        }

        if (component.Entities.Count > 0 && TryComp<MetaDataComponent>(component.Entities[^1], out var cartridgeMetaData))
        {
            args.PushMarkup(Loc.GetString("gun-chamber-examine", ("color", ModeExamineColor),
                ("cartridge", cartridgeMetaData.EntityName)), -1);
        }
        else if (component.UnspawnedCount > 0 && component.Proto != null)
        {
            var cartridge = _proto.Index<EntityPrototype>(component.Proto);
            args.PushMarkup(Loc.GetString("gun-chamber-examine", ("color", ModeExamineColor),
                ("cartridge", cartridge.Name)), -1);
        }
        else
        {
            args.PushMarkup(Loc.GetString("gun-chamber-examine-empty", ("color", ModeExamineBadColor)), -1);
        }

        if (!component.AutoCycle)
        {
            if (component.Racked)
            {
                args.PushMarkup(Loc.GetString("gun-racked-examine", ("color", ModeExamineColor)), -1);
            }
            else
            {
                args.PushMarkup(Loc.GetString("gun-racked-examine-not", ("color", ModeExamineBadColor)), -1);
            }
        }

        // WWDP edit end
    }

    // WWDP manual ammo extraction
    private void ExtractAction(EntityUid uid, MapCoordinates coordinates, BallisticAmmoProviderComponent component, EntityUid user)
    {
        Extract(uid, coordinates, component, user);

        Audio.PlayPredicted(component.SoundInsert, uid, user);
        UpdateBallisticAppearance(uid, component);
        UpdateAmmoCount(uid);
    }
    // WWDP edit end

    protected abstract void Extract(EntityUid uid, MapCoordinates coordinates, BallisticAmmoProviderComponent component,
        EntityUid user); // WWDP

    private void ManualCycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates, EntityUid? user = null, GunComponent? gunComp = null)
    {
        if (!component.Cycleable)
            return;

        // Reset shotting for cycling
        if (Resolve(uid, ref gunComp, false)
            && gunComp is { FireRateModified: > 0f }
            && !Paused(uid))
            gunComp.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1 / gunComp.FireRateModified);


        Dirty(uid, component);
        Audio.PlayPredicted(component.SoundRack, uid, user);

        var shots = GetBallisticShots(component);
        component.Cycled = true;
        Cycle(uid, component, coordinates, gunComp);

        var text = Loc.GetString(shots == 0 ? "gun-ballistic-cycled-empty" : "gun-ballistic-cycled");

        component.Racked = true; // WWDP

        Popup(text, uid, user);
        UpdateBallisticAppearance(uid, component);
        UpdateAmmoCount(uid);
    }

    protected abstract void Cycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates, GunComponent? gunComponent = null);

    private void OnBallisticInit(EntityUid uid, BallisticAmmoProviderComponent component, ComponentInit args)
    {
        component.Container = Containers.EnsureContainer<Container>(uid, "ballistic-ammo");
        // TODO: This is called twice though we need to support loading appearance data (and we need to call it on MapInit
        // to ensure it's correct).
        UpdateBallisticAppearance(uid, component);
    }

    private void OnBallisticMapInit(EntityUid uid, BallisticAmmoProviderComponent component, MapInitEvent args)
    {
        // TODO this should be part of the prototype, not set on map init.
        // Alternatively, just track spawned count, instead of unspawned count.
        if (component.Proto != null)
        {
            component.UnspawnedCount = Math.Max(0, component.Capacity - component.Container.ContainedEntities.Count);
            UpdateBallisticAppearance(uid, component);
            Dirty(uid, component);
        }
    }

    protected int GetBallisticShots(BallisticAmmoProviderComponent component) => component.Entities.Count + component.UnspawnedCount;

    private void OnBallisticTakeAmmo(EntityUid uid, BallisticAmmoProviderComponent component, TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            if (!component.Cycled)
                break;

            EntityUid entity;

            if (component.Entities.Count > 0)
            {
                entity = component.Entities[^1];

                args.Ammo.Add((entity, EnsureShootable(entity)));

                // WWDP edit; support internal caseless ammo in hand-cycled guns
                if (TryComp<CartridgeAmmoComponent>(entity, out var cartridge) && cartridge.DeleteOnSpawn)
                {
                    component.Entities.RemoveAt(component.Entities.Count - 1);
                    Containers.Remove(entity, component.Container);
                    component.Racked = false;
                    break;
                }
                // WWDP edit end

                // if entity in container it can't be ejected, so shell will remain in gun and block next shoot
                if (!component.AutoCycle)
                {
                    component.Racked = false; // WWDP
                    break;
                }
                component.Entities.RemoveAt(component.Entities.Count - 1);
                Containers.Remove(entity, component.Container);
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                entity = Spawn(component.Proto, args.Coordinates);
                args.Ammo.Add((entity, EnsureShootable(entity)));

                // Put it back in if it doesn't auto-cycle
                if (Timing.IsFirstTimePredicted && TryComp<CartridgeAmmoComponent>(entity, out var cartridge) && !component.AutoCycle) // WD EDIT
                {
                    // WD EDIT START
                    component.Racked = false;
                    if (cartridge.DeleteOnSpawn)
                        break;
                    // WD EDIT END

                    component.Entities.Add(entity);
                    Containers.Insert(entity, component.Container);
                }
            }

            if (!component.AutoCycle)
                component.Cycled = false;
        }

        UpdateBallisticAppearance(uid, component);
        Dirty(uid, component);
    }

    // WD EDIT START
    private void OnForceSpawnAmmo(EntityUid uid, BallisticAmmoProviderComponent component, ForceSpawnAmmoEvent args)
    {
        while(component.UnspawnedCount > 0)
        {
            var ent = Spawn(component.Proto, MapCoordinates.Nullspace);
            component.Entities.Add(ent);
            Containers.Insert(ent, component.Container);
            component.UnspawnedCount--;
        }
        Dirty(uid, component);
    }
    // WD EDIT END
    private void OnBallisticAmmoCount(EntityUid uid, BallisticAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = GetBallisticShots(component);
        args.Capacity = component.Capacity;
    }

    public void UpdateBallisticAppearance(EntityUid uid, BallisticAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var count = GetBallisticShots(component); // WD EDIT

        Appearance.SetData(uid, AmmoVisuals.AmmoCount, count, appearance); // WD EDIT
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.Capacity, appearance);
        Appearance.SetData(uid, AmmoVisuals.HasAmmo, count != 0, appearance); // WD EDIT
    }

    // WD EDIT START
    protected virtual EntityUid GetStackEntity(EntityUid uid, StackComponent stack)
    {
        return uid;
    }
    // WD EDIT END
}

/// <summary>
/// DoAfter event for filling one ballistic ammo provider from another.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AmmoFillDoAfterEvent : SimpleDoAfterEvent { }
