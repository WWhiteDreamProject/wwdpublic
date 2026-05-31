using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Humanoid.Markings.Managers;

/// <summary>
/// Manager responsible for sharing the logic of markings between in-simulation bodies and out-of-simulation profile editing
/// </summary>
public sealed class MarkingManager
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<ProtoId<MarkingCategoryPrototype>, FrozenDictionary<string, MarkingPrototype>> _markingsByCategory = default!;
    private FrozenDictionary<string, MarkingPrototype> _markings = default!;

    public void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypeReload;

        CachePrototypes();
    }

    #region Event Handling

    private void CachePrototypes()
    {
        var markingDict = new Dictionary<ProtoId<MarkingCategoryPrototype>, Dictionary<string, MarkingPrototype>>();

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingPrototype>())
        {
            if (markingDict.TryGetValue(prototype.Category, out var markingByLayer))
            {
                markingByLayer.Add(prototype.ID, prototype);
                continue;
            }

            markingDict.Add(prototype.Category, new() { { prototype.ID, prototype }, });
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

    #region Public API

    /// <summary>
    /// Determines if a marking prototype can be applied to something with the given markings group and sex.
    /// </summary>
    /// <param name="group">The markings group to test</param>
    /// <param name="sex">The sex to test</param>
    /// <param name="prototype">The prototype to reference against</param>
    /// <returns>True if a marking with the prototype could be applied</returns>
    public bool CanBeApplied(MarkingPrototype prototype, ProtoId<MarkingGroupPrototype> group, Sex sex)
    {
        var groupPrototype = _prototype.Index(group);
        var whitelisted = groupPrototype.Limits.GetValueOrDefault(prototype.Category)?.OnlyGroupWhitelisted ?? groupPrototype.OnlyGroupWhitelisted;

        return CanBeApplied(prototype, whitelisted, groupPrototype, sex);
    }

    /// <summary>
    /// Gets the marking prototype associated with the marking.
    /// </summary>
    public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingPrototype)
    {
        return _markings.TryGetValue(marking.Id, out markingPrototype);
    }

    /// <summary>
    /// Gets the marking prototype associated with the marking id.
    /// </summary>
    public bool TryGetMarking(string id, [NotNullWhen(true)] out MarkingPrototype? markingPrototype)
    {
        return _markings.TryGetValue(id, out markingPrototype);
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
        ProtoId<MarkingGroupPrototype> group,
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
            if (!CanBeApplied(marking, whitelisted, groupPrototype, sex))
                continue;

            markings.Add(key, marking);
        }

        markingByCategoryAndGroup = markings.ToFrozenDictionary();
        return markings.Count > 0;
    }

    /// <summary>
    /// Ensures that the <see cref="markings"/> have a valid amount of color.
    /// </summary>
    public void EnsureValidColors(List<Marking> markings, ProtoId<MarkingGroupPrototype> group, Dictionary<ProtoId<BodyColorationPrototype>, Color> bodyColoration)
    {
        var groupPrototype = _prototype.Index(group);

        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (!TryGetMarking(markings[i], out var marking))
            {
                markings.RemoveAt(i);
                continue;
            }

            if (!marking.ForcedColoring && groupPrototype.Appearances.GetValueOrDefault(marking.Category)?.MatchSkin != true)
                continue;

            var color = marking.Coloring.Default.GetColor(bodyColoration, markings);

            var name = markings[i].Sprite switch
            {
                SpriteSpecifier.Rsi rsi => rsi.RsiState,
                SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
                _ => string.Empty,
            };

            if (marking.Coloring.Layers != null && marking.Coloring.Layers.TryGetValue(name, out var layerColoring))
                color = layerColoring.GetColor(bodyColoration, markings);

            markings[i] = markings[i].WithColor(color);
        }
    }

    /// <summary>
    /// Ensures that the <see cref="markings"/> are valid per the constraints on <see cref="group"/> and <see cref="sex"/>
    /// </summary>
    public void EnsureValidGroupAndSex(List<Marking> markings, ProtoId<MarkingGroupPrototype> group, Sex sex)
    {
        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (TryGetMarking(markings[i], out var marking) && CanBeApplied(marking, group, sex))
                continue;

            markings.RemoveAt(i);
        }
    }

    /// <summary>
    /// Ensures the list of <see cref="markings"/> is valid per the limits of the <see cref="group"/>.
    /// </summary>
    public void EnsureValidLimits(List<Marking> markings, ProtoId<MarkingGroupPrototype> group, Dictionary<ProtoId<BodyColorationPrototype>, Color> bodyColoration)
    {
        var groupPrototype = _prototype.Index(group);
        var counts = new Dictionary<ProtoId<MarkingCategoryPrototype>, int>();
        var processed = new HashSet<ProtoId<MarkingPrototype>>();

        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (processed.Contains(markings[i].Id))
                continue;

            if (!TryGetMarking(markings[i], out var marking))
            {
                markings.RemoveAt(i);
                continue;
            }

            processed.Add(markings[i].Id);

            if (!groupPrototype.Limits.TryGetValue(marking.Category, out var limit))
                continue;

            var count = counts.GetValueOrDefault(marking.Category);
            if (count >= limit.Limit)
            {
                markings.RemoveAt(i);
                continue;
            }

            counts[marking.Category] = count + 1;
        }

        foreach (var (category, count) in counts)
        {
            if (count > 0)
                continue;

            if (!groupPrototype.Limits.TryGetValue(category, out var limit))
                continue;

            if (!limit.Required)
                continue;

            foreach (var marking in limit.Default)
            {
                if (!_markings.TryGetValue(marking, out var markingPrototype))
                    continue;

                var colors = Marking.GetMarkingColors(markingPrototype, bodyColoration, markings);
                for (var i = markingPrototype.Markings.Count - 1; i >= 0; i--)
                {
                    var data =  markingPrototype.Markings[i];
                    markings.Add(new(data.Layer, marking, data.Sprite, colors[i]));
                }
            }
        }
    }

    #endregion

    #region Private API

    private bool CanBeApplied(MarkingPrototype prototype, bool whitelisted, ProtoId<MarkingGroupPrototype> group, Sex sex)
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
}
