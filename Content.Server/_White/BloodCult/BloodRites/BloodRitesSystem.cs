using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared._White.BloodCult.BloodRites;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._White.BloodCult.BloodRites;

public sealed class BloodRitesSystem : SharedBloodRitesSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private readonly ProtoId<ReagentPrototype> _bloodProto = "Blood";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodRitesComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BloodRitesComponent, BloodRitesExtractDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<BloodRitesComponent> rites, ref AfterInteractEvent args)
    {
        if (args.Handled
            || args.Target is not {} target
            || !TryComp<BloodCultistComponent>(args.User, out var bloodCultist))
            return;

        if (HasComp<BloodCultistComponent>(target))
        {
            if ((!TryComp(target, out BloodstreamComponent? bloodstream)
                    || !RestoreBloodLevel(rites, (args.User, bloodCultist), (target, bloodstream)))
                && (!TryComp(target, out DamageableComponent? damageable)
                    || !Heal(rites, (args.User, bloodCultist), (target, damageable))))
                return;

            _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
            args.Handled = true;

            return;
        }

        if (HasComp<BloodstreamComponent>(target))
        {
            var ev = new BloodRitesExtractDoAfterEvent();
            var time = rites.Comp.BloodExtractionTime;
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, time, ev, rites, target)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            if (!_doAfter.TryStartDoAfter(doAfterArgs))
                return;

            args.Handled = true;
            return;
        }

        if (HasComp<PuddleComponent>(target))
            args.Handled = ConsumePuddles(target, (args.User, bloodCultist), rites);
        else if (TryComp(target, out SolutionContainerManagerComponent? solutionContainer))
            args.Handled = ConsumeBloodFromSolution((target, solutionContainer), (args.User, bloodCultist));

        if (args.Handled)
            _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
    }

    private void OnDoAfter(Entity<BloodRitesComponent> rites, ref BloodRitesExtractDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target is not { } target
            || !TryComp(target, out BloodstreamComponent? bloodstream)
            || bloodstream.BloodSolution is not { } solution
            || !TryComp<BloodCultistComponent>(args.User, out var bloodCultist))
            return;

        var extracted = solution.Comp.Solution.RemoveReagent(_bloodProto, rites.Comp.BloodExtractionAmount, ignoreReagentData: true);

        bloodCultist.StoredBloodAmount += extracted;
        Dirty(args.User, bloodCultist);

        _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
        args.Handled = true;
    }

    private bool Heal(Entity<BloodRitesComponent> rites, Entity<BloodCultistComponent> user, Entity<DamageableComponent> target)
    {
        if (target.Comp.TotalDamage == 0)
            return false;

        if (TryComp(target, out MobStateComponent? mobState) && mobState.CurrentState == MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-heal-dead"), target, user);
            return false;
        }

        if (!HasComp<BloodstreamComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-heal-no-bloodstream"), target, user);
            return false;
        }

        var bloodCost = rites.Comp.HealingCost;
        if (target.Owner == user.Owner)
            bloodCost *= rites.Comp.SelfHealRatio;

        if (bloodCost >= user.Comp.StoredBloodAmount)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-not-enough-blood"), rites, user);
            return false;
        }

        var healingLeft = rites.Comp.TotalHealing;

        foreach (var (type, value) in target.Comp.Damage.DamageDict)
        {
            // somehow?
            if (!_protoManager.TryIndex(type, out DamageTypePrototype? damageType))
                continue;

            var toHeal = value;
            if (toHeal > healingLeft)
                toHeal = healingLeft;

            _damageable.TryChangeDamage(target, new DamageSpecifier(damageType, -toHeal));

            healingLeft -= toHeal;
            if (healingLeft == 0)
                break;
        }

        user.Comp.StoredBloodAmount -= bloodCost;
        Dirty(user);
        return true;
    }

    private bool RestoreBloodLevel(
        Entity<BloodRitesComponent> rites,
        Entity<BloodCultistComponent> user,
        Entity<BloodstreamComponent> target
    )
    {
        if (target.Comp.BloodSolution is null)
            return false;

        _bloodstream.FlushChemicals(target, string.Empty, 10);
        var missingBlood = target.Comp.BloodSolution.Value.Comp.Solution.AvailableVolume;
        if (missingBlood == 0)
            return false;

        var bloodCost = missingBlood * rites.Comp.BloodRegenerationRatio;
        if (target.Owner == user.Owner)
            bloodCost *= rites.Comp.SelfHealRatio;

        if (bloodCost > user.Comp.StoredBloodAmount)
        {
            _popup.PopupEntity("blood-rites-no-blood-left", rites, user);
            bloodCost = user.Comp.StoredBloodAmount;
        }

        _bloodstream.TryModifyBleedAmount(target, -3);
        _bloodstream.TryModifyBloodLevel(target, bloodCost / rites.Comp.BloodRegenerationRatio);

        user.Comp.StoredBloodAmount -= bloodCost;
        Dirty(user);
        return true;
    }

    private bool ConsumePuddles(EntityUid origin, Entity<BloodCultistComponent> user, Entity<BloodRitesComponent> rites)
    {
        var coords = Transform(origin).Coordinates;

        var lookup = _lookup.GetEntitiesInRange<PuddleComponent>(
            coords,
            rites.Comp.PuddleConsumeRadius,
            LookupFlags.Uncontained);

        var result = false;
        foreach (var puddle in lookup)
        {
            if (!TryComp(puddle, out SolutionContainerManagerComponent? solutionContainer))
                continue;

            result = ConsumeBloodFromSolution((puddle, solutionContainer), user);
        }

        return result;
    }

    private bool ConsumeBloodFromSolution(
        Entity<SolutionContainerManagerComponent?> ent,
        Entity<BloodCultistComponent> user
    )
    {
        foreach (var (_, solution) in _solutionContainer.EnumerateSolutions(ent))
        {
            var reagentVolume = solution.Comp.Solution.RemoveReagent(_bloodProto, 1000, ignoreReagentData: true);
            if (reagentVolume == FixedPoint2.Zero)
                continue;

            user.Comp.StoredBloodAmount += reagentVolume;
            Dirty(user);

            _solutionContainer.UpdateChemicals(solution);
            return true;
        }

        return false;
    }
}
