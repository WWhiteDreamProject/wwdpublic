using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;


    protected virtual void InitializeBallistic()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentInit>(OnBallisticInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, MapInitEvent>(OnBallisticMapInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, TakeAmmoEvent>(OnBallisticTakeAmmo);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ForceSpawnAmmoEvent>(OnForceSpawnAmmo); // WD EDIT

        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetAmmoCountEvent>(OnBallisticAmmoCount);

        SubscribeLocalEvent<BallisticAmmoProviderComponent, ExaminedEvent>(OnBallisticExamine);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<Verb>>(OnBallisticVerb);
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
        if (args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Used))
            return;

        if (GetBallisticShots(component) >= component.Capacity)
            return;

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
        UpdateBallisticAppearance(uid, component);
        Dirty(uid, component);
    }

    private void OnBallisticAfterInteract(EntityUid uid, BallisticAmmoProviderComponent component, AfterInteractEvent args)
    {
        if (args.Handled ||
            !component.MayTransfer ||
            !Timing.IsFirstTimePredicted ||
            args.Target == null ||
            args.Used == args.Target ||
            Deleted(args.Target) ||
            !TryComp<BallisticAmmoProviderComponent>(args.Target, out var targetComponent) ||
            targetComponent.Whitelist == null)
        {
            return;
        }

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
        if (Deleted(args.Target) ||
            !TryComp<BallisticAmmoProviderComponent>(args.Target, out var target) ||
            target.Whitelist == null)
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
            }

            if (IsClientSide(ent.Value))
                Del(ent.Value);
        }

        // repeat if there is more space in the target and more ammo to fill it
        var moreSpace = target.Entities.Count + target.UnspawnedCount < target.Capacity;
        var moreAmmo = component.Entities.Count + component.UnspawnedCount > 0;
        args.Repeat = moreSpace && moreAmmo;
    }

    private void OnBallisticVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !component.Cycleable)
            return;

        if (component.Cycleable)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("gun-ballistic-cycle"),
                Disabled = GetBallisticShots(component) == 0,
                Act = () => ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User),
            });

        }
    }

    private void OnBallisticExamine(EntityUid uid, BallisticAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", GetBallisticShots(component))));
    }

    private void ManualCycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates, EntityUid? user = null, GunComponent? gunComp = null)
    {
        if (!component.Cycleable)
            return;

        // Reset shotting for cycling
        if (Resolve(uid, ref gunComp, false) &&
            gunComp is { FireRateModified: > 0f } &&
            !Paused(uid))
        {
            gunComp.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1 / gunComp.FireRateModified);
        }

        Dirty(uid, component);
        Audio.PlayPredicted(component.SoundRack, uid, user);

        var shots = GetBallisticShots(component);
        Cycle(uid, component, coordinates);

        var text = Loc.GetString(shots == 0 ? "gun-ballistic-cycled-empty" : "gun-ballistic-cycled");

        Popup(text, uid, user);
        UpdateBallisticAppearance(uid, component);
        UpdateAmmoCount(uid);
    }

    protected abstract void Cycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates);

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

    protected int GetBallisticShots(BallisticAmmoProviderComponent component)
    {
        return component.Entities.Count + component.UnspawnedCount;
    }

    private void OnBallisticTakeAmmo(EntityUid uid, BallisticAmmoProviderComponent component, TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            EntityUid entity;

            if (component.Entities.Count > 0)
            {
                entity = component.Entities[^1];

                args.Ammo.Add((entity, EnsureShootable(entity)));

                if (component.AutoCycle) // WD EDIT
                {
                    component.Entities.RemoveAt(component.Entities.Count - 1);
                    Containers.Remove(entity, component.Container);
                }
                else
                    break;
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                entity = Spawn(component.Proto, args.Coordinates);
                args.Ammo.Add((entity, EnsureShootable(entity)));

                 // WD EDIT START
                if (!component.AutoCycle && HasComp<CartridgeAmmoComponent>(entity))
                {
                    component.Entities.Add(entity);
                    Containers.Insert(entity, component.Container);
                    break;
                }
                // WD EDIT END
            }
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
public sealed partial class AmmoFillDoAfterEvent : SimpleDoAfterEvent
{
}
