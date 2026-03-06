using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server._White.Body.Organs.Stomach;

public sealed class StomachSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public bool TryTransferSolution(Entity<StomachComponent?> stomach, Solution solution)
    {
        if (!Resolve(stomach, ref stomach.Comp)
            || !_solutionContainer.ResolveSolution(stomach.Owner, stomach.Comp.SolutionName, ref stomach.Comp.Solution))
            return false;

        return _solutionContainer.TryAddSolution(stomach.Comp.Solution.Value, solution);
    }

    public void SetSpecialDigestible(Entity<StomachComponent?> stomach, EntityWhitelist? whitelist)
    {
        if (!Resolve(stomach, ref stomach.Comp))
            return;

        stomach.Comp.SpecialDigestible = whitelist;
    }
}
