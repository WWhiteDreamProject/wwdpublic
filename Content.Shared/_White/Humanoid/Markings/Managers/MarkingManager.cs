using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Humanoid.Markings.Managers;

/// <summary>
/// A manager that centralizes the logic for marking application, validation, and retrieval.
/// </summary>
public sealed class MarkingManager
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private FrozenDictionary<Enum, HashSet<ProtoId<MarkingCategoryPrototype>>> _categoriesByLayer = default!;
    private FrozenDictionary<ProtoId<MarkingCategoryPrototype>, HashSet<Enum>> _layersByCategory = default!;
    private FrozenDictionary<ProtoId<MarkingCategoryPrototype>, HashSet<MarkingPrototype>> _markingsByCategory = default!;
    private FrozenDictionary<ProtoId<MarkingPrototype>, MarkingPrototype> _markings = default!;

    public void Initialize()
    {
        _prototype.PrototypesReloaded += OnPrototypeReload;

        CacheCategoryPrototypes();
        CacheMarkingPrototypes();
    }

    #region Event Handling

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MarkingCategoryPrototype>())
            CacheCategoryPrototypes();

        if (args.WasModified<MarkingPrototype>())
            CacheMarkingPrototypes();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Determines if a marking prototype can be applied to something with the given markings group and sex.
    /// </summary>
    /// <param name="prototype">The marking prototype to validate.</param>
    /// <param name="whitelisted">Determines if the marking category is explicitly whitelisted for this group.</param>
    /// <param name="groupId">The marking group id.</param>
    /// <param name="sex">The sex of the character.</param>
    /// <returns>True if a marking could be applied, false otherwise.</returns>
    public bool CanBeApplied(MarkingPrototype prototype, bool whitelisted, ProtoId<MarkingGroupPrototype> groupId, Sex sex)
    {
        if (prototype.Groups == null)
        {
            if (whitelisted)
                return false;
        }
        else
        {
            if (!prototype.Groups.Contains(groupId))
                return false;
        }

        return prototype.SexRestriction == null || prototype.SexRestriction == sex;
    }

    /// <summary>
    /// Determines if a marking prototype can be applied to something with the given markings group and sex.
    /// </summary>
    /// <param name="prototype">The marking prototype to validate.</param>
    /// <param name="groupId">The ID of the character's marking group.</param>
    /// <param name="sex">The sex of the character.</param>
    /// <returns>True if a marking could be applied, false otherwise.</returns>
    public bool CanBeApplied(MarkingPrototype prototype, ProtoId<MarkingGroupPrototype> groupId, Sex sex)
    {
        var whitelisted = Whitelisted(prototype.Category, groupId);
        return CanBeApplied(prototype, whitelisted, groupId, sex);
    }

    /// <summary>
    /// Attempts to retrieve the prototype for a given marking.
    /// </summary>
    /// <param name="marking">The marking whose prototype we want to get.</param>
    /// <param name="prototype">The returned marking prototype.</param>
    /// <returns>True if the marking prototype is successfully returned, false otherwise.</returns>
    public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? prototype)
    {
        return _markings.TryGetValue(marking.Id, out prototype);
    }

    /// <summary>
    /// Attempts to retrieve the prototype for a given marking id.
    /// </summary>
    /// <param name="id">The marking id whose prototype we want to get.</param>
    /// <param name="prototype">The returned marking prototype.</param>
    /// <returns>True if the marking prototype is successfully returned, false otherwise.</returns>
    public bool TryGetMarking(ProtoId<MarkingPrototype> id, [NotNullWhen(true)] out MarkingPrototype? prototype)
    {
        return _markings.TryGetValue(id, out prototype);
    }

    /// <summary>
    /// Attempts to retrieve the all markings for a given marking category.
    /// </summary>
    /// <param name="categoryId">The marking category id whose markings we want to get.</param>
    /// <param name="markings">The returned markings.</param>
    /// <returns>True if the markings is successfully returned, false otherwise.</returns>
    public bool TryGetMarkings(
        ProtoId<MarkingCategoryPrototype> categoryId,
        [NotNullWhen(true)] out HashSet<MarkingPrototype>? markings
        )
    {
        return _markingsByCategory.TryGetValue(categoryId, out markings);
    }

    /// <summary>
    /// Attempts to retrieve the all markings for a given marking category, group and sex.
    /// </summary>
    /// <param name="categoryId">The marking category id whose markings we want to get.</param>
    /// <param name="groupId">The marking group id by which we want to get markings.</param>
    /// <param name="sex">The sex by which we want to get markings.</param>
    /// <param name="markings">The returned markings.</param>
    /// <returns>True if the markings is successfully returned, false otherwise.</returns>
    public bool TryGetMarkings(
        ProtoId<MarkingCategoryPrototype> categoryId,
        ProtoId<MarkingGroupPrototype> groupId,
        Sex sex,
        [NotNullWhen(true)] out HashSet<MarkingPrototype>? markings
    )
    {
        markings = new();
        if (!_prototype.TryIndex(groupId, out var group))
            return false;

        if (!group.CategoriesData.ContainsKey(categoryId))
            return false;

        if (!TryGetMarkings(categoryId, out var markingsByCategory))
            return false;

        var whitelisted = Whitelisted(categoryId, group);

        foreach (var marking in markingsByCategory)
        {
            if (!CanBeApplied(marking, whitelisted, group, sex))
                continue;

            markings.Add(marking);
        }

        return markings.Count > 0;
    }

    /// <summary>
    /// Checks if a specific marking category is whitelisted for a given marking group.
    /// </summary>
    /// <param name="categoryId">The marking category id to check.</param>
    /// <param name="groupId">The marking group id to check against.</param>
    /// <returns>True if the category is whitelisted for the group, false otherwise.</returns>
    public bool Whitelisted(ProtoId<MarkingCategoryPrototype> categoryId, ProtoId<MarkingGroupPrototype> groupId)
    {
        if (!_prototype.TryIndex(groupId, out var group))
            return false;

        return Whitelisted(categoryId, group);
    }

    /// <summary>
    /// Checks if a specific marking category is whitelisted for a given marking group.
    /// </summary>
    /// <param name="categoryId">The marking category id to check.</param>
    /// <param name="group">The marking group to check against.</param>
    /// <returns>True if the category is whitelisted for the group, false otherwise.</returns>
    public bool Whitelisted(ProtoId<MarkingCategoryPrototype> categoryId, MarkingGroupPrototype group)
    {
        return group.CategoriesData.GetValueOrDefault(categoryId)?.OnlyGroupWhitelisted ?? group.OnlyGroupWhitelisted;
    }

    /// <summary>
    /// Calculates and returns a color for marking layer defined in a marking prototype.
    /// </summary>
    /// <param name="prototype">The marking prototype whose color are being determined.</param>
    /// <param name="index">The marking index name whose color are being determined.</param>
    /// <param name="colorGroups">A dictionary of body color groups IDs prototypes and their applied colors.</param>
    /// <param name="otherMarkings">A list of other markings present on the entity, used for context in color calculations.</param>
    /// <returns>A color of marking layer.</returns>
    public Color GetMarkingColor(MarkingPrototype prototype, string index, Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups, List<Marking> otherMarkings)
    {
        if (prototype.Coloring.Indexes == null || !prototype.Coloring.Indexes.TryGetValue(index, out var coloring))
            return prototype.Coloring.Default.GetColor(colorGroups, otherMarkings);

        return coloring.GetColor(colorGroups, otherMarkings);
    }

    /// <summary>
    /// Organizes a collection of markings by their layer.
    /// </summary>
    /// <param name="markingsByCategory">A dictionary where keys are marking category IDs and values are lists of marking.</param>
    /// <returns> A dictionary where keys are layer and values are lists of marking</returns>
    public Dictionary<Enum, List<Marking>> GetMarkingsByLayer(IReadOnlyDictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> markingsByCategory)
    {
        var markingsByLayer = new Dictionary<Enum, List<Marking>>();

        foreach (var markings in markingsByCategory.Values)
        {
            foreach (var marking in markings)
            {
                var markingsSet = markingsByLayer.GetValueOrDefault(marking.Layer) ?? [];
                markingsSet.Add(marking);
                markingsByLayer[marking.Layer] = markingsSet;
            }
        }

        return markingsByLayer;
    }

    /// <summary>
    /// Organizes a collection of markings by their category.
    /// </summary>
    /// <param name="markingsByLayer">A dictionary where keys are layers and values are lists of marking.</param>
    /// <returns> A dictionary where keys are marking category IDs and values are lists of marking</returns>
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> GetMarkingsByCategory(IReadOnlyDictionary<Enum, List<Marking>> markingsByLayer)
    {
        var markingsByCategory = new Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>>();

        foreach (var markings in markingsByLayer.Values)
        {
            foreach (var marking in markings)
            {
                var markingsSet = markingsByCategory.GetValueOrDefault(marking.Category) ?? [];
                markingsSet.Add(marking);
                markingsByCategory[marking.Category] = markingsSet;
            }
        }

        return markingsByCategory;
    }

    /// <summary>
    /// Returns a set of layers associated with given marking categories.
    /// </summary>
    /// <param name="categoriesIds">The marking categories ids whose supported layers we want to get.</param>
    /// <returns>A set of supported layers.</returns>
    public HashSet<Enum> GetLayers(HashSet<ProtoId<MarkingCategoryPrototype>> categoriesIds)
    {
        var layers = new HashSet<Enum>();

        foreach (var categoryId in categoriesIds)
        {
            layers.UnionWith( GetLayers(categoryId));
        }

        return layers;
    }

    /// <summary>
    /// Returns a set of layers associated with given marking category.
    /// </summary>
    /// <param name="categoryId">The marking category id whose supported layers we want to get.</param>
    /// <returns>A set of supported layers.</returns>
    public HashSet<Enum> GetLayers(ProtoId<MarkingCategoryPrototype> categoryId)
    {
        if (!_layersByCategory.TryGetValue(categoryId, out var layers))
            return new();

        return layers;
    }

    /// <summary>
    /// Returns a set of marking categories associated with given layers.
    /// </summary>
    /// <param name="layers">The layers whose supported marking categories we want to get.</param>
    /// <returns>A set of supported marking categories.</returns>
    public HashSet<ProtoId<MarkingCategoryPrototype>> GetCategories(HashSet<Enum> layers)
    {
        var categories = new HashSet<ProtoId<MarkingCategoryPrototype>>();

        foreach (var layer in layers)
        {
            categories.UnionWith( GetCategories(layer));
        }

        return categories;
    }

    /// <summary>
    /// Returns a set of marking categories associated with given layer.
    /// </summary>
    /// <param name="layer">The layer whose supported marking categories we want to get.</param>
    /// <returns>A set of supported marking categories.</returns>
    public HashSet<ProtoId<MarkingCategoryPrototype>> GetCategories(Enum layer)
    {
        if (!_categoriesByLayer.TryGetValue(layer, out var categories))
            return new();

        return categories;
    }

    /// <summary>
    /// Calculates and returns a list of colors for each marking marking defined in a marking prototype.
    /// </summary>
    /// <param name="prototype">The marking prototype whose colors are being determined.</param>
    /// <param name="colorGroups">A dictionary of body color groups IDs prototypes and their applied colors.</param>
    /// <param name="otherMarkings">A list of other markings present on the entity, used for context in color calculations.</param>
    /// <returns>A list of color, where each color corresponds to a marking marking in the prototype.</returns>
    public List<Color> GetMarkingColors(MarkingPrototype prototype, Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups, List<Marking> otherMarkings)
    {
        var colors = new List<Color>();

        var defaultColor = prototype.Coloring.Default.GetColor(colorGroups, otherMarkings);

        if (prototype.Coloring.Indexes == null)
        {
            for (var i = 0; i < prototype.Definitions.Count; i++)
                colors.Add(defaultColor);

            return colors;
        }

        foreach (var definition in prototype.Definitions)
        {
            if (!prototype.Coloring.Indexes.TryGetValue(definition.Index, out var coloring))
            {
                colors.Add(defaultColor);
                continue;
            }

            var markingColor = coloring.GetColor(colorGroups, otherMarkings);
            colors.Add(markingColor);
        }

        return colors;
    }

    /// <summary>
    /// Ensures that the colors of existing markings are valid according to their prototype's coloring rules,
    /// potentially updating them if they are not. This is particularly relevant for markings with forced coloring.
    /// </summary>
    /// <param name="markings">The list of markings to validate and update colors for.</param>
    /// <param name="groupId">The marking group id the entity belongs to.</param>
    /// <param name="colorGroups">A dictionary of body color groups IDs prototypes and their applied colors.</param>
    public void EnsureValidColors(List<Marking> markings, ProtoId<MarkingGroupPrototype> groupId, Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups)
    {
        if (!_prototype.TryIndex(groupId, out var group))
            return;

        for (var i = markings.Count - 1; i >= 0; i--)
        {
            var marking = markings[i];
            if (!TryGetMarking(marking, out var prototype))
            {
                markings.RemoveAt(i);
                continue;
            }

            if (!prototype.ForcedColoring)
                continue;

            var color = prototype.Coloring.Default.GetColor(colorGroups, markings);

            if (group.CategoriesData.GetValueOrDefault(prototype.Category)?.Coloring is { } coloring
                || prototype.Coloring.Indexes != null && prototype.Coloring.Indexes.TryGetValue(marking.Index, out coloring))
                color = coloring.GetColor(colorGroups, markings);

            markings[i] = marking.WithColor(color);
        }
    }

    /// <summary>
    /// Ensures that the provided list of markings is valid for the given marking group and sex.
    /// Any markings that are not applicable are removed from the list.
    /// </summary>
    /// <param name="markings">The list of markings to validate. This list will be modified in place.</param>
    /// <param name="groupId">The marking group id the entity belongs to.</param>
    /// <param name="sex">The sex of the entity.</param>
    public void EnsureValidGroupAndSex(List<Marking> markings, ProtoId<MarkingGroupPrototype> groupId, Sex sex)
    {
        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (TryGetMarking(markings[i], out var prototype) && CanBeApplied(prototype, groupId, sex))
                continue;

            markings.RemoveAt(i);
        }
    }

    /// <summary>
    /// Ensures that the provided list of markings adheres to the quantity limits defined by the marking group.
    /// It also adds default markings if required categories are empty.
    /// </summary>
    /// <param name="markings">The list of markings to validate. This list will be modified in place.</param>
    /// <param name="groupId">The marking group id the entity belongs to.</param>
    /// <param name="colorGroups">A dictionary of body color groups IDs prototypes and their applied colors.</param>
    public void EnsureValidLimits(List<Marking> markings, ProtoId<MarkingGroupPrototype> groupId, Dictionary<ProtoId<BodyColorGroupPrototype>, Color> colorGroups)
    {
        if (!_prototype.TryIndex(groupId, out var group))
            return;

        var counts = new Dictionary<ProtoId<MarkingCategoryPrototype>, int>();
        var processed = new HashSet<ProtoId<MarkingPrototype>>();

        for (var i = markings.Count - 1; i >= 0; i--)
        {
            if (processed.Contains(markings[i].Id))
                continue;

            if (!TryGetMarking(markings[i], out var prototype))
            {
                markings.RemoveAt(i);
                continue;
            }

            processed.Add(markings[i].Id);

            if (!group.CategoriesData.TryGetValue(prototype.Category, out var categoryData))
            {
                markings.RemoveAt(i);
                continue;
            }

            var count = counts.GetValueOrDefault(prototype.Category);
            if (count >= categoryData.Limit)
            {
                markings.RemoveAt(i);
                continue;
            }

            counts[prototype.Category] = count + 1;
        }

        foreach (var (category, count) in counts)
        {
            if (count > 0)
                continue;

            if (!group.CategoriesData.TryGetValue(category, out var categoryData))
                continue;

            if (!categoryData.Required)
                continue;

            foreach (var marking in categoryData.Default)
            {
                if (!_markings.TryGetValue(marking, out var markingPrototype))
                    continue;

                var colors = GetMarkingColors(markingPrototype, colorGroups, markings);
                for (var i = markingPrototype.Definitions.Count - 1; i >= 0; i--)
                {
                    var definition =  markingPrototype.Definitions[i];
                    markings.Add(new(definition.OverrideAppearance, definition.Layer, markingPrototype.Category, marking, definition.Sprite, definition.Index, colors[i]));
                }
            }
        }
    }

    #endregion

    #region Private AP

    private void CacheCategoryPrototypes()
    {
        var categoriesByLayer = new Dictionary<Enum, HashSet<ProtoId<MarkingCategoryPrototype>>>();
        var layersByCategory = new Dictionary<ProtoId<MarkingCategoryPrototype>, HashSet<Enum>>();

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingCategoryPrototype>())
        {
            var layersSet = layersByCategory.GetValueOrDefault(prototype) ?? [];

            foreach (var layer in prototype.Layers)
            {
                var categoriesSet = categoriesByLayer.GetValueOrDefault(layer) ?? [];

                categoriesSet.Add(prototype);
                layersSet.Add(layer);

                categoriesByLayer[layer] = categoriesSet;
            }

            layersByCategory.Add(prototype, layersSet);
        }

        _categoriesByLayer = categoriesByLayer.ToFrozenDictionary();
        _layersByCategory = layersByCategory.ToFrozenDictionary();
    }

    private void CacheMarkingPrototypes()
    {
        var markingsByCategory = new Dictionary<ProtoId<MarkingCategoryPrototype>, HashSet<MarkingPrototype>>();

        foreach (var prototype in _prototype.EnumeratePrototypes<MarkingPrototype>())
        {
            var markingsSet = markingsByCategory.GetValueOrDefault(prototype.Category) ?? [];
            markingsSet.Add(prototype);
            markingsByCategory[prototype.Category] = markingsSet;
        }

        _markingsByCategory = markingsByCategory.ToFrozenDictionary();
        _markings = _prototype.EnumeratePrototypes<MarkingPrototype>().ToFrozenDictionary(x => new ProtoId<MarkingPrototype>(x.ID));
    }

    #endregion
}
