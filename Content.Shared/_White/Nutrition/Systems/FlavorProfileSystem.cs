using System.Linq;
using Content.Shared._White.Nutrition.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Nutrition.Systems;

/// <summary>
/// Deals with flavor profiles when you eat something.
/// </summary>
public sealed class FlavorProfileSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private int _flavorLimit;

    private EntityQuery<FlavorProfileComponent> _flavorProfileQuery;

    private const string BackupFlavorMessage = "flavor-profile-unknown";

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, CCVars.FlavorLimit, value => { _flavorLimit = value; }, true);

        _flavorProfileQuery = GetEntityQuery<FlavorProfileComponent>();
    }

    #region Public API

    /// <summary>
    /// Gets a localized message describing the combined flavors of an item and its solution.
    /// </summary>
    /// <param name="ent">The entity that has flavors.</param>
    /// <param name="user">The entity experiencing the flavor.</param>
    /// <param name="solution">The solution of reagents to extract flavors from.</param>
    /// <returns>A localized string describing the flavors.</returns>
    public string GetLocalizedFlavorsMessage(Entity<FlavorProfileComponent?> ent, EntityUid user, Solution? solution)
    {
        HashSet<ProtoId<FlavorPrototype>> flavors = new();
        HashSet<ProtoId<ReagentPrototype>>? ignored = null;

        if (_flavorProfileQuery.Resolve(ent, ref ent.Comp, false))
        {
            flavors = ent.Comp.Flavors;
            ignored = ent.Comp.Ignored;
        }

        if (solution != null)
            flavors.UnionWith(GetFlavorsFromReagents(solution, _flavorLimit - flavors.Count, ignored));

        var modifierEv = new FlavorProfileModifierEvent(user, flavors);
        RaiseLocalEvent(ent, ref modifierEv);

        if (flavors.Count == 0)
            return Loc.GetString(BackupFlavorMessage);

        return FlavorsToFlavorMessage(flavors);
    }

    /// <summary>
    /// Gets a localized message describing the combined flavors of a solution.
    /// </summary>
    /// <param name="user">The entity experiencing the flavor.</param>
    /// <param name="solution">The solution of reagents to extract flavors from.</param>
    /// <returns>A localized string describing the flavors from the solution.</returns>
    public string GetLocalizedFlavorsMessage(EntityUid user, Solution solution)
    {
        var flavors = GetFlavorsFromReagents(solution, _flavorLimit);

        return FlavorsToFlavorMessage(flavors);
    }

    #endregion

    #region Private API

    private string FlavorsToFlavorMessage(HashSet<ProtoId<FlavorPrototype>> flavorsIds)
    {
        var flavors = new List<FlavorPrototype>();
        foreach (var flavorId in flavorsIds)
        {
            if (!_prototype.TryIndex(flavorId, out var flavor))
                continue;

            flavors.Add(flavor);
        }

        flavors.Sort((a, b) => a.FlavorType.CompareTo(b.FlavorType));

        if (flavors.Count == 1 && !string.IsNullOrEmpty(flavors[0].FlavorDescription))
        {
            return Loc.GetString("flavor-profile", ("flavor", Loc.GetString(flavors[0].FlavorDescription)));
        }

        if (flavors.Count > 1)
        {
            var lastFlavor = Loc.GetString(flavors[^1].FlavorDescription);
            var allFlavors = string.Join(", ", flavors.GetRange(0, flavors.Count - 1).Select(i => Loc.GetString(i.FlavorDescription)));
            return Loc.GetString("flavor-profile-multiple", ("flavors", allFlavors), ("lastFlavor", lastFlavor));
        }

        return Loc.GetString(BackupFlavorMessage);
    }

    private HashSet<ProtoId<FlavorPrototype>> GetFlavorsFromReagents(Solution solution, int amount, HashSet<ProtoId<ReagentPrototype>>? ignored = null)
    {
        var flavors = new HashSet<ProtoId<FlavorPrototype>>();
        foreach (var (reagent, quantity) in solution.GetReagentPrototypes(_prototype))
        {
            if (ignored != null && ignored.Contains(reagent.ID))
                continue;

            if (flavors.Count == amount)
                break;

            if (quantity < reagent.FlavorMinimum)
                continue;

            if (reagent.Flavor == null)
                continue;

            flavors.Add(reagent.Flavor.Value);
        }

        return flavors;
    }

    #endregion
}

/// <summary>
/// An event raised when flavor modifiers are being calculated.
/// </summary>
/// <param name="User">The entity experiencing the flavor.</param>
/// <param name="Flavors">The set of flavor prototype IDs to be modified. This can be added to.</param>
[ByRefEvent]
public record struct FlavorProfileModifierEvent(EntityUid User, HashSet<ProtoId<FlavorPrototype>> Flavors);
