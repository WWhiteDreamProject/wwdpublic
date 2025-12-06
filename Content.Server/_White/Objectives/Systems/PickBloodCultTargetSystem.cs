using Content.Server._White.GameTicking.Rules;
using Content.Server._White.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server._White.Objectives.Systems;

public sealed class PickBloodCultTargetSystem : EntitySystem
{
    [Dependency] private readonly BloodCultRuleSystem _cultRule = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PickBloodCultTargetComponent, ObjectiveAssignedEvent>(OnObjectiveAssigned);
    }

    private void OnObjectiveAssigned(Entity<PickBloodCultTargetComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var targetObjective)
            || _cultRule.GetBloodCultOfferingTarget() is not { } target)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent, target, targetObjective);
    }
}
