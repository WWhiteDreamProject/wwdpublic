using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings;

/// <summary>
/// Manager responsible for sharing the logic of markings between in-simulation bodies and out-of-simulation profile editing
/// </summary>
public sealed class MarkingManager : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<ProtoId<MarkingCategoryPrototype>, FrozenDictionary<string, MarkingPrototype>> _markingsByCategory = default!;
    private FrozenDictionary<string, MarkingPrototype> _markings = default!;

    public override void Initialize()
    {
        base.Initialize();

        _prototype.PrototypesReloaded += OnPrototypeReload;

        CachePrototypes();
    }

    #region Event Handling

    private void CachePrototypes()
    {
        var markingDict = new Dictionary<ProtoId<MarkingCategoryPrototype>, Dictionary<string, MarkingPrototype>>();

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingPrototype>())
        {
            if (markingDict.TryGetValue(prototype.MarkingCategory, out var markingByLayer))
            {
                markingByLayer.Add(prototype.ID, prototype);
                continue;
            }

            markingDict.Add(prototype.MarkingCategory, new() { { prototype.ID, prototype }, });
        }

        _markings = _prototype.EnumeratePrototypes<MarkingPrototype>().ToFrozenDictionary(x => x.ID);
        _markingsByCategory = markingDict.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.ToFrozenDictionary());
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingPrototype>())
            CachePrototypes();
    }

    #endregion

    #region Private API

    private bool CanBeApplied(MarkingsGroupPrototype group, Sex sex, MarkingPrototype prototype, bool whitelisted)
    {
        if (prototype.GroupWhitelist == null)
        {
            if (whitelisted)
                return false;
        }
        else
        {
            if (!prototype.GroupWhitelist.Contains(group))
                return false;
        }

        return prototype.SexRestriction == null || prototype.SexRestriction == sex;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Determines if a marking prototype can be applied to something with the given markings group and sex.
    /// </summary>
    /// <param name="group">The markings group to test</param>
    /// <param name="sex">The sex to test</param>
    /// <param name="prototype">The prototype to reference against</param>
    /// <returns>True if a marking with the prototype could be applied</returns>
    public bool CanBeApplied(ProtoId<MarkingsGroupPrototype> group, Sex sex, MarkingPrototype prototype)
    {
        var groupProto = _prototype.Index(group);
        var whitelisted = groupProto.Limits.GetValueOrDefault(prototype.MarkingCategory)?.OnlyGroupWhitelisted ?? groupProto.OnlyGroupWhitelisted;

        return CanBeApplied(groupProto, sex, prototype, whitelisted);
    }

    /// <summary>
    /// Gets the marking prototype associated with the marking.
    /// </summary>
    public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingPrototype)
    {
        return _markings.TryGetValue(marking.MarkingId, out markingPrototype);
    }

    /// <summary>
    /// Gets the marking prototypes associated with the category.
    /// </summary>
    public bool TryGetMarkingsByCategory(ProtoId<MarkingCategoryPrototype> category, [NotNullWhen(true)] out FrozenDictionary<string, MarkingPrototype>? markingByCategory)
    {
        return _markingsByCategory.TryGetValue(category, out markingByCategory);
    }

    /// <summary>
    /// Markings by category, species and sex.
    /// </summary>
    /// <remarks>
    /// This is done per category, as enumerating over every single marking by group isn't useful.
    /// Please make a pull request if you find a use case for that behavior.
    /// </remarks>
    public bool TryGetMarkingsByCategoryAndGroupAndSex(
        ProtoId<MarkingCategoryPrototype> category,
        ProtoId<MarkingsGroupPrototype> group,
        Sex sex,
        [NotNullWhen(true)] out FrozenDictionary<string, MarkingPrototype>? markingByCategoryAndGroup
    )
    {
        markingByCategoryAndGroup = null;
        if (!TryGetMarkingsByCategory(category, out var markingByLayer))
            return false;

        var groupPrototype = _prototype.Index(group);
        var whitelisted = groupPrototype.Limits.GetValueOrDefault(category)?.OnlyGroupWhitelisted ?? groupPrototype.OnlyGroupWhitelisted;
        var markings = new Dictionary<string, MarkingPrototype>();

        foreach (var (key, marking) in markingByLayer)
        {
            if (!CanBeApplied(groupPrototype, sex, marking, whitelisted))
                continue;

            markings.Add(key, marking);
        }

        markingByCategoryAndGroup = markings.ToFrozenDictionary();
        return markings.Count > 0;
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> have a valid amount of color.
    /// </summary>
    public void EnsureValidColors(Dictionary<Enum, List<Marking>> markingSets)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (!TryGetMarking(markings[i], out var marking))
                {
                    markings.RemoveAt(i);
                    continue;
                }

                if (marking.Markings.Count == markings[i].MarkingColors.Count)
                    continue;

                markings[i] = new (marking.ID, marking.Markings.Count);
            }
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markingSets"/> are valid per the constraints on <see cref="group"/> and <see cref="sex"/>
    /// </summary>
    public void EnsureValidGroupAndSex(Dictionary<Enum, List<Marking>> markingSets, ProtoId<MarkingsGroupPrototype> group, Sex sex)
    {
        foreach (var markings in markingSets.Values)
        {
            for (var i = markings.Count - 1; i >= 0; i--)
            {
                if (TryGetMarking(markings[i], out var marking) && CanBeApplied(group, sex, marking))
                    continue;

                markings.RemoveAt(i);
            }
        }
    }

    #endregion
}
