using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared._NC.Cyberware.Components;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Traits.Assorted.Components;
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

    public override void Initialize()
    {
        base.Initialize();
        
        // No specific events to subscribe here yet as most logic is in Update or triggered from CyberwareSystem
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

                // We try to add the solution to the HOST (uid), not the implant (implantUid)
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

    /// <summary>
    ///     Refreshes stats like breathing immunity and crit thresholds when implants change.
    /// </summary>
    public void RefreshCyberwareStats(EntityUid uid, CyberwareComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        bool hasBreathingImmunity = false;
        int critModifier = 0;

        foreach (var implantUid in component.InstalledImplants.Values)
        {
            if (HasComp<BreathingImmunityComponent>(implantUid))
                hasBreathingImmunity = true;

            if (TryComp<CritModifierComponent>(implantUid, out var crit))
                critModifier += crit.CritThresholdModifier;
        }

        // Apply breathing immunity
        if (hasBreathingImmunity)
            EnsureComp<BreathingImmunityComponent>(uid);
        else
            RemComp<BreathingImmunityComponent>(uid);

        // Apply crit threshold
        // Note: This is a bit simplified. In a real system, we'd need to know the BASE threshold.
        // For now, we update the Critical threshold directly.
        if (TryGetThreshold(uid, MobState.Critical, out var baseCrit))
        {
            _mobThreshold.SetMobStateThreshold(uid, baseCrit + critModifier, MobState.Critical);
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
