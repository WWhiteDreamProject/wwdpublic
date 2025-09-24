using Content.Shared._White.BloodCult.BloodCultist;
using Content.Shared._White.BloodCult.Runes.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared._White.BloodCult.Runes;

public abstract class SharedBloodCultRuneSystem : EntitySystem
{
    [Dependency] protected readonly EntityWhitelistSystem EntityWhitelist = default!;

    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultRuneComponent, ExamineAttemptEvent>(OnRuneExamineAttempt);
        SubscribeLocalEvent<BloodCultRuneComponent, InRangeOverrideEvent>(CheckInRange);
    }

    private void OnRuneExamineAttempt(Entity<BloodCultRuneComponent> ent, ref ExamineAttemptEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner) && EntityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Examiner))
            args.Cancel();
    }

    private void CheckInRange(Entity<BloodCultRuneComponent> rune, ref InRangeOverrideEvent args)
    {
        if (!TryComp(args.Target, out TransformComponent? transform))
            return;

        args.InRange = _interaction.InRangeUnobstructed(args.User, args.Target, transform.Coordinates, transform.LocalRotation, rune.Comp.RuneActivationRange);
        args.Handled = true;
    }

    /// <summary>
    ///     Gets all cultists near rune.
    /// </summary>
    public HashSet<EntityUid> GatherCultists(EntityUid rune, float range)
    {
        var runeTransform = Transform(rune);
        var entities = _entityLookup.GetEntitiesInRange(runeTransform.Coordinates, range);
        entities.RemoveWhere(entity => !HasComp<BloodCultistComponent>(entity));
        return entities;
    }

    /// <summary>
    ///     Gets all the humanoids near rune.
    /// </summary>
    /// <param name="rune">The rune itself.</param>
    /// <param name="range">Radius for a lookup.</param>
    /// <param name="exlude">Filter to exlude from return.</param>
    public HashSet<Entity<HumanoidAppearanceComponent>> GetTargetsNearRune(
        EntityUid rune,
        float range,
        Predicate<Entity<HumanoidAppearanceComponent>>? exlude = null
    )
    {
        var runeTransform = Transform(rune);
        var possibleTargets = _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(runeTransform.Coordinates, range);
        if (exlude != null)
            possibleTargets.RemoveWhere(exlude);

        return possibleTargets;
    }

    /// <summary>
    ///     Is used to stop target from pulling/being pulled before teleporting them.
    /// </summary>
    public void StopPulling(EntityUid target)
    {
        if (TryComp(target, out PullableComponent? pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(target, pullable, ignoreGrab: true);

        if (TryComp<PullerComponent>(target, out var pullerComp)
            && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
            _pulling.TryStopPull(target, subjectPulling, ignoreGrab: true);
    }
}
