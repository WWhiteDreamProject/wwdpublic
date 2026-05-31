using Content.Server.EntityEffects.Effects;
using Content.Shared._White.Nutrition.Components;
using Content.Shared._White.Nutrition.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Nutrition.Systems;

public sealed class IngestionSystem : SharedIngestionSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    #region Public API

    /// <summary>
    /// Gets the total metabolizable hydration from an entity, assumes we can eat and metabolize it.
    /// </summary>
    /// <param name="ent">The entity being ingested.</param>
    /// <returns>The amount of hydration the ingestible is worth.</returns>
    public float TotalHydration(Entity<IngestibleComponent?> ent)
    {
        if (!IngestibleQuery.Resolve(ent, ref ent.Comp))
            return 0f;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _prototype.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            foreach (var entry in reagent.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is not SatiateThirst thirst)
                        continue;

                    total += thirst.HydrationFactor * quantity.Quantity.Float();
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Gets the total metabolizable nutrition from an entity, assumes we can eat and metabolize it.
    /// </summary>
    /// <param name="ent">The entity being ingested.</param>
    /// <returns>The amount of nutrition the ingestible is worth.</returns>
    public float TotalNutrition(Entity<IngestibleComponent?> ent)
    {
        if (!IngestibleQuery.Resolve(ent, ref ent.Comp))
            return 0f;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return 0f;

        var total = 0f;
        foreach (var quantity in solution.Contents)
        {
            var reagent = _prototype.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            foreach (var entry in reagent.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is not SatiateHunger hunger)
                        continue;

                    total += hunger.NutritionFactor * quantity.Quantity.Float();
                }
            }
        }

        return total;
    }

    #endregion
}
