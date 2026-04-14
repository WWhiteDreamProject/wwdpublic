using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Flash.Components;
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Stunnable;
using Content.Shared.Traits.Assorted.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._NC.Cyberware.Systems;

/// <summary>
///     Handles server-side relaying of component properties from cyberware to host.
///     Manages regeneration, breathing immunity, and health threshold modifiers.
/// </summary>
public sealed class CyberwareRelaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CyberwareComponent>();
        while (query.MoveNext(out var uid, out var cyberware))
        {
            foreach (var implantUid in cyberware.InstalledImplants.Values)
            {
                if (!TryComp<SolutionRegenerationComponent>(implantUid, out var regen))
                    continue;

                if (_timing.CurTime < regen.NextRegenTime)
                    continue;

                regen.NextRegenTime = _timing.CurTime + regen.Duration;

                if (_solutionContainer.TryGetInjectableSolution(uid, out var solution, out _) && solution.HasValue)
                {
                    var amount = FixedPoint2.Min(solution.Value.Comp.Solution.AvailableVolume, regen.Generated.Volume);
                    if (amount <= FixedPoint2.Zero)
                        continue;

                    Solution generated;
                    if (amount == regen.Generated.Volume)
                    {
                        generated = regen.Generated;
                    }
                    else
                    {
                        generated = regen.Generated.Clone().SplitSolution(amount);
                    }

                    _solutionContainer.TryAddSolution(solution.Value, generated);
                }
            }
        }
    }

    public void RefreshCyberwareStats(EntityUid uid, CyberwareComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Capture base stats if not already done
        var stats = EnsureComp<CyberwareStatsComponent>(uid);
        if (!stats.Captured)
        {
            if (TryGetThreshold(uid, MobState.Critical, out var baseCrit))
                stats.BaseThresholds[MobState.Critical] = baseCrit;
            if (TryGetThreshold(uid, MobState.Dead, out var baseDead))
                stats.BaseThresholds[MobState.Dead] = baseDead;
            
            stats.Captured = true;
            Dirty(uid, stats);
        }

        bool hasBreathingImmunity = false;
        bool hasKnockbackImmunity = false;
        bool hasSlowImmunity = false;
        bool hasFlashImmunity = false;
        float staminaResistance = 1f;
        int critModifier = 0;
        int deadModifier = 0;

        foreach (var implantUid in component.InstalledImplants.Values)
        {
            if (HasComp<BreathingImmunityComponent>(implantUid))
                hasBreathingImmunity = true;

            if (TryComp<CritModifierComponent>(implantUid, out var crit))
            {
                critModifier += crit.CritThresholdModifier;
                deadModifier += crit.DeadThresholdModifier;
            }

            if (TryComp<SturdyComponent>(implantUid, out var sturdy))
            {
                if (sturdy.KnockbackImmunity)
                    hasKnockbackImmunity = true;
            }

            if (HasComp<CyberwareSlowImmunityComponent>(implantUid))
                hasSlowImmunity = true;

            if (HasComp<FlashImmunityComponent>(implantUid))
                hasFlashImmunity = true;

            if (TryComp<StaminaDamageResistanceComponent>(implantUid, out var stamina))
                staminaResistance *= stamina.Coefficient;
        }

        // Apply breathing immunity
        if (hasBreathingImmunity)
            EnsureComp<BreathingImmunityComponent>(uid);
        else
            RemComp<BreathingImmunityComponent>(uid);

        // Apply flash immunity
        if (hasFlashImmunity)
            EnsureComp<FlashImmunityComponent>(uid);
        else
            RemComp<FlashImmunityComponent>(uid);

        // Apply slow immunity (Custom Cyberware Logic)
        if (hasSlowImmunity)
            EnsureComp<CyberwareSlowImmunityComponent>(uid);
        else
            RemComp<CyberwareSlowImmunityComponent>(uid);

        // ALWAYS refresh movement speed if we have cyberware
        _movementSpeed.RefreshMovementSpeedModifiers(uid);

        // Apply stamina resistance
        if (staminaResistance < 1f)
        {
            var staminaComp = EnsureComp<StaminaDamageResistanceComponent>(uid);
            staminaComp.Coefficient = staminaResistance;
            Dirty(uid, staminaComp);
        }
        else
        {
            RemComp<StaminaDamageResistanceComponent>(uid);
        }

        // Apply health thresholds from BASE
        if (stats.BaseThresholds.TryGetValue(MobState.Critical, out var baseCritVal))
        {
            _mobThreshold.SetMobStateThreshold(uid, baseCritVal + critModifier, MobState.Critical);
        }

        if (stats.BaseThresholds.TryGetValue(MobState.Dead, out var baseDeadVal))
        {
            _mobThreshold.SetMobStateThreshold(uid, baseDeadVal + deadModifier, MobState.Dead);
        }

        // Apply knockback immunity by switching BodyType to KinematicController
        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            var sturdyHost = EnsureComp<SturdyComponent>(uid);
            if (hasKnockbackImmunity)
            {
                if (physics.BodyType != BodyType.KinematicController)
                {
                    sturdyHost.BaseBodyType = physics.BodyType;
                    _physics.SetBodyType(uid, BodyType.KinematicController, body: physics);
                }
            }
            else
            {
                if (physics.BodyType == BodyType.KinematicController)
                {
                    _physics.SetBodyType(uid, sturdyHost.BaseBodyType, body: physics);
                }
                
                bool hasAnySturdy = false;
                foreach (var implantUid in component.InstalledImplants.Values)
                {
                    if (HasComp<SturdyComponent>(implantUid))
                    {
                        hasAnySturdy = true;
                        break;
                    }
                }
                if (!hasAnySturdy)
                    RemComp<SturdyComponent>(uid);
            }
        }
    }

    private bool TryGetThreshold(EntityUid uid, MobState state, out FixedPoint2 threshold)
    {
        threshold = FixedPoint2.Zero;
        if (_mobThreshold.TryGetThresholdForState(uid, state, out var val))
        {
            threshold = val.Value;
            return true;
        }
        return false;
    }
}
