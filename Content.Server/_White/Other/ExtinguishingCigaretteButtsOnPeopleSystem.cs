using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Smoking;
using Robust.Shared.Random;

namespace Content.Server._White.Other;

public sealed class ExtinguishingCigaretteButtsOnPeopleSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokableComponent, AfterInteractEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, SmokableComponent component, AfterInteractEvent args)
    {
        if (args.Handled ||
        args.Target == null ||
        !args.CanReach ||
        component.State != SmokableState.Burnt ||
        component.IsExtinguishedOnSomeone)
            return;

        var target = args.Target.Value;
        var user = args.User;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        var dmg = new DamageSpecifier();
        dmg.DamageDict.Add("Heat", 1);

        var targetParts = new[] { TargetBodyPart.Arms, TargetBodyPart.Hands };
        _damage.TryChangeDamage(target, dmg, true, targetPart: Random.Shared.Pick<TargetBodyPart>(targetParts));
        component.IsExtinguishedOnSomeone = true;

        var loc = target == user ? "extinguishing-cigarette-butt-self" : "extinguishing-cigarette-butt-other";

        _popup.PopupEntity(
            Loc.GetString(loc, ("name", target), ("smokeable", uid)),
            target,
            PopupType.Small
        );

        args.Handled = true;
    }
}
