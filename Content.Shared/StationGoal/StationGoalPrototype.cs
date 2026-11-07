using Robust.Shared.Prototypes;

namespace Content.Shared.StationGoal;

[Prototype]
public sealed partial class StationGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    public string Text => Loc.GetString($"station-goal-{ID.ToLower()}");
}
