using Content.Server._White.Body;
using Content.Shared.Clothing;
using Content.Shared.EntityEffects;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Server._White.Xenomorphs.FaceHugger;

public sealed class FaceHuggerSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FaceHuggerComponent, MeleeHitEvent>(OnMeleeHitEvent);
        SubscribeLocalEvent<FaceHuggerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<FaceHuggerComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<FaceHuggerComponent, ClothingGotEquippedEvent>(OnGotEquipped);
    }

    private void OnMeleeHitEvent(EntityUid uid, FaceHuggerComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.FirstOrNull() is not {} target)
            return;

        TryEquipFaceHugger(uid, target);
    }

    private void OnStartCollide(EntityUid uid, FaceHuggerComponent component, StartCollideEvent args)
    {
        TryEquipFaceHugger(uid, args.OtherEntity);
    }

    private void OnGotEquippedHand(EntityUid uid, FaceHuggerComponent component, GotEquippedHandEvent args)
    {
        TryEquipFaceHugger(uid, args.User);
    }

    private void OnGotEquipped(EntityUid uid, FaceHuggerComponent component, ClothingGotEquippedEvent args)
    {
        if (args.Slot != "mask" || HasComp<InfectedBodyComponent>(args.Equipee))
            return;

        var effectsArgs = new EntityEffectBaseArgs(args.Equipee, EntityManager);
        foreach (var effect in component.Effects)
            effect.Effect(effectsArgs);
    }

    private void TryEquipFaceHugger(EntityUid uid, EntityUid target)
    {
        if (_mobState.IsDead(uid)
            || HasComp<InfectedBodyComponent>(target)
            || HasComp<GhostComponent>(target)
            || _inventory.TryGetSlotEntity(target, "head", out var headItem)
                && TryComp<IngestionBlockerComponent>(headItem, out var ingestionBlocker)
                && ingestionBlocker.Enabled)
            return;

        _inventory.TryUnequip(target, "mask", true, true);
        _inventory.TryEquip(target, uid, "mask", true, true);
    }
}
