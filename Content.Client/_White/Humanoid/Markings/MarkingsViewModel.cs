using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Appearance;
using Content.Shared._White.Appearance.Prototypes;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._White.Humanoid.Markings;

public sealed class MarkingsViewModel
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public MarkingsViewModel()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Whether the markings view model will enforce restrictions on the group and sex.
    /// </summary>
    private bool _enforceGroupAndSexRestrictions = true;

    /// <summary>
    /// Whether the markings view model will enforce limitations on how many markings can have
    /// </summary>
    private bool _enforceLimits = true;

    private bool AnyEnforcementsLifted => !_enforceLimits || !_enforceGroupAndSexRestrictions;

    /// <summary>
    /// The appearance data this view model is concerned with.
    /// </summary>
    private Dictionary<Enum, BodyAppearanceData> _appearanceData = new();

    /// <summary>
    /// The marking data the view model is concerned with.
    /// </summary>
    private Dictionary<Enum, MarkingData> _markingsData = new();

    /// <summary>
    /// The currently applied set of markings.
    /// </summary>
    private Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> _markings = new();

    private readonly Dictionary<ProtoId<MarkingPrototype>, List<Color>> _previousColors = new();

    /// <inheritdoc cref="_enforceGroupAndSexRestrictions"/>
    /// <seealso cref="EnforcementsChanged" />
    public bool EnforceGroupAndSexRestrictions
    {
        get => _enforceGroupAndSexRestrictions;
        set
        {
            if (_enforceGroupAndSexRestrictions == value)
                return;

            _enforceGroupAndSexRestrictions = value;
            EnforcementsChanged?.Invoke();
        }
    }

    /// <inheritdoc cref="_enforceLimits"/>
    /// <seealso cref="EnforcementsChanged" />
    public bool EnforceLimits
    {
        get => _enforceLimits;
        set
        {
            if (_enforceLimits == value)
                return;

            _enforceLimits = value;
            EnforcementsChanged?.Invoke();
        }
    }

    /// <inheritdoc cref="_appearanceData"/>
    /// <seealso cref="BodyAppearanceDataChanged" />
    public Dictionary<Enum, BodyAppearanceData> AppearanceData
    {
        get => _appearanceData;
        set
        {
            _appearanceData = value.ShallowClone();
            BodyAppearanceDataChanged?.Invoke();
        }
    }

    /// <inheritdoc cref="_markingsData"/>
    /// <seealso cref="MarkingsDataChanged" />
    public Dictionary<Enum, MarkingData> MarkingsData
    {
        get => _markingsData;
        set
        {
            if (_markingsData == value)
                return;

            _markingsData = value;
            _previousColors.Clear();
            MarkingsDataChanged?.Invoke();
        }
    }

    /// <inheritdoc cref="_markings"/>
    /// <seealso cref="MarkingsReset" />
    public Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> Markings
    {
        get => _markings;
        set
        {
            _markings = value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ShallowClone());
            MarkingsReset?.Invoke();
        }
    }

    /// <summary>
    /// Raised whenever the body appearance data changes.
    /// The boolean value represents whether the set of possible markings may have changed.
    /// </summary>
    /// <seealso cref="AppearanceData" />
    /// <seealso cref="SetBodyType" />
    /// <seealso cref="SetColor" />
    /// <seealso cref="SetSex" />
    public event Action? BodyAppearanceDataChanged;

    /// <summary>
    /// Raised whenever the view model is enforcing a different set of constraints on possible markings than before.
    /// </summary>
    /// <seealso cref="EnforceLimits" />
    /// <seealso cref="EnforceGroupAndSexRestrictions" />
    public event Action? EnforcementsChanged;

    /// <summary>
    /// Raised whenever the markings data within the view model is changed.
    /// </summary>
    public event Action? MarkingsDataChanged;

    /// <summary>
    /// Raised whenever the set of markings has fully changed and requires a UI reload.
    /// </summary>
    public event Action? MarkingsReset;

    /// <summary>
    /// Raised whenever a specific layer's markings have changed
    /// </summary>
    public event Action<ProtoId<MarkingCategoryPrototype>>? MarkingsChanged;

    /// <summary>
    /// Returns whether the marking at the given location can have its color customized by the user.
    /// </summary>
    /// <param name="id">The marking ID to check for</param>
    /// <returns>Whether the marking is capable of having its color customized by the user.</returns>
    public bool IsMarkingColorCustomizable(ProtoId<MarkingPrototype> id)
    {
        if (!_marking.TryGetMarking(id, out var prototype))
            return false;

        if (prototype.ForcedColoring)
            return false;

        foreach (var definition in prototype.Definitions)
        {
            if (!_markingsData.TryGetValue(definition.Layer, out var markingData))
                continue;

            if (!_prototype.TryIndex(markingData.Group, out var group))
                continue;

            if (!group.CategoriesData.TryGetValue(prototype.Category, out var categoryData))
                return true;

            if (categoryData.Coloring != null)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove a marking from the current set of markings.
    /// </summary>
    /// <param name="id">The marking ID to deselect.</param>
    /// <returns>Whether the marking was successfully removed from the set of markings.</returns>
    public bool TryDeselectMarking(ProtoId<MarkingPrototype> id)
    {
        if (!_marking.TryGetMarking(id, out var prototype))
            return false;

        _markings[prototype.Category] = _markings.GetValueOrDefault(prototype.Category) ?? [];
        var markings = _markings[prototype.Category];

        var count = GetMarkingCategoryCount(prototype.Category);
        if (count == 0)
            return false;

        foreach (var definition in prototype.Definitions)
        {
            if (!EnforceLimits)
                continue;

            if (!_markingsData.TryGetValue(definition.Layer, out var markingsData))
                return false;

            if (!_prototype.TryIndex(markingsData.Group, out var group))
                return false;

            if (!group.CategoriesData.TryGetValue(prototype.Category, out var categoryData))
                return false;

            if (!categoryData.Required || count > 1)
                continue;

            return false;
        }

        var removedMarkingColors = new HashSet<Color>();
        foreach (var marking in markings.ToList())
        {
            if (marking.Id != id)
                continue;

            removedMarkingColors.Add(marking.Color);
            markings.Remove(marking);
        }

        _previousColors[id] = removedMarkingColors.ToList();
        MarkingsChanged?.Invoke(prototype.Category);

        return true;
    }

    /// <summary>
    /// Returns the currently applied markings by its ID.
    /// </summary>
    /// <returns>The markings currently applied if it exists, otherwise null</returns>
    public bool TryGetMarkings(ProtoId<MarkingPrototype> id, [NotNullWhen(true)] out List<Marking>? markings)
    {
        markings = null;
        if (!_marking.TryGetMarking(id, out var prototype))
            return false;

        if (!_markings.TryGetValue(prototype.Category, out var markingsSet))
            return false;

        markings = new();
        foreach (var marking in markingsSet)
        {
            if (marking.Id != id)
                continue;

            markings.Add(marking);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the currently applied marking for a given marking category.
    /// </summary>
    /// <param name="categoryId">The marking category id whose markings we want to get.</param>
    /// <param name="markings">The returned markings.</param>
    /// <returns>True if the markings is successfully returned, false otherwise.</returns>
    public bool TryGetMarkings(ProtoId<MarkingCategoryPrototype> categoryId, [NotNullWhen(true)] out List<Marking>? markings)
    {
        return _markings.TryGetValue(categoryId, out markings);
    }

    /// <summary>
    /// Attempts to add a marking to the current set of markings.
    /// </summary>
    /// <param name="id">The marking ID to select.</param>
    /// <returns>Whether the marking was successfully added to the set of markings.</returns>
    public bool TrySelectMarking(ProtoId<MarkingPrototype> id)
    {
        if (!_marking.TryGetMarking(id, out var prototype))
            return false;

        _markings[prototype.Category] = _markings.GetValueOrDefault(prototype.Category) ?? [];
        var markings = _markings[prototype.Category];

        var count = GetMarkingCategoryCount(prototype.Category);

        foreach (var definition in prototype.Definitions)
        {
            if (!_appearanceData.TryGetValue(definition.Layer, out var appearanceData))
                return false;

            if (!_markingsData.TryGetValue(definition.Layer, out var markingsData))
                return false;

            if (!_prototype.TryIndex(markingsData.Group, out var group))
                return false;

            if (EnforceGroupAndSexRestrictions && !_marking.CanBeApplied(prototype, markingsData.Group, appearanceData.Sex))
                return false;

            if (!group.CategoriesData.TryGetValue(prototype.Category, out var categoryData))
                return false;

            if (categoryData.Limit == 1 && count == 1)
            {
                markings.Clear();
                count = 0;
            }

            if (!EnforceLimits || count < categoryData.Limit)
                continue;

            return false;
        }

        var previousColors = _previousColors.GetValueOrDefault(id);
        for (var i = prototype.Definitions.Count - 1; i >= 0; i--)
        {
            var definition =  prototype.Definitions[i];

            if (!_appearanceData.TryGetValue(definition.Layer, out var appearanceData))
                continue;

            var color = previousColors?[i]
                ?? _marking.GetMarkingColor(
                    prototype,
                    definition.Index,
                    appearanceData.ColorGroups,
                    markings);

            var marking = new Marking(definition.OverrideAppearance, definition.Layer, prototype.Category, id, definition.Sprite, definition.Index, color)
            {
                Forced = AnyEnforcementsLifted,
            };

            markings.Add(marking);
        }

        MarkingsChanged?.Invoke(prototype.Category);
        return true;
    }

    /// <summary>
    /// Attempts to set the color of the specified marking at the given index.
    /// </summary>
    /// <param name="id">The marking ID to select.</param>
    /// <param name="index">The index within the marking's color array to set.</param>
    /// <param name="color">The new color to set</param>
    /// <returns>Whether the marking was successfully set color.</returns>
    public bool TrySetMarkingColor(ProtoId<MarkingPrototype> id, string index, Color color)
    {
        if (!_marking.TryGetMarking(id, out var prototype))
            return false;

        if (!TryGetMarkings(prototype, out var markings))
            return false;

        var contains = false;
        for (var i = 0; i < markings.Count; i++)
        {
            var marking = markings[i];
            if (marking.Index != index)
                continue;

            markings[i] = markings[i].WithColor(color);
            contains = true;
        }

        if (!contains)
            return false;

        MarkingsChanged?.Invoke(prototype.Category);
        return true;
    }

    /// <summary>
    /// Calculates the number of unique marking prototypes present within a specified category.
    /// </summary>
    /// <param name="categoryId">The category to check for unique markings within.</param>
    /// <returns>The total count of distinct marking prototypes found in the given category.</returns>
    public int GetMarkingCategoryCount(ProtoId<MarkingCategoryPrototype> categoryId)
    {
        _markings[categoryId] = _markings.GetValueOrDefault(categoryId) ?? [];
        var markings = _markings[categoryId];

        var count = 0;
        var processed = new HashSet<ProtoId<MarkingPrototype>>();
        foreach (var marking in markings)
        {
            if (!processed.Add(marking.Id))
                continue;

            count++;
        }

        return count;
    }

    /// <summary>
    /// Reorders the specified marking ID to the index and position relative to its index.
    /// </summary>
    /// <param name="categoryId">The category to reorder the markings of.</param>
    /// <param name="id">The marking to reorder.</param>
    /// <param name="position">Whether the marking should be moved to before or after the given index.</param>
    /// <param name="positionIndex">The new position index of the marking.</param>
    public void ChangeMarkingOrder(
        ProtoId<MarkingCategoryPrototype> categoryId,
        ProtoId<MarkingPrototype> id,
        CandidatePosition position,
        int positionIndex
    )
    {
        if (!_markings.TryGetValue(categoryId, out var markings))
            return;

        var currentIndex = markings.FindIndex(marking => marking.Id == id);
        var currentMarking = markings[currentIndex];

        var insertionIndex = 0;
        if (position == CandidatePosition.Before)
        {
            insertionIndex = currentIndex < positionIndex ? positionIndex - 1 : positionIndex;
        }
        else if (position == CandidatePosition.After)
        {
            insertionIndex = currentIndex > positionIndex ? positionIndex + 1 : positionIndex;
        }

        markings.RemoveAt(currentIndex);
        markings.Insert(insertionIndex, currentMarking);

        MarkingsChanged?.Invoke(categoryId);
    }

    /// <summary>
    /// Gets the status data for a marking category.
    /// </summary>
    /// <param name="categoryId">The category to check for unique markings within.</param>
    /// <param name="isRequired">Whether this layer requires at least one marking to be selected.</param>
    /// <param name="count">The maximum number of markings that can be selected.</param>
    /// <param name="selected">The currently selected number of markings.</param>
    public void GetMarkingStatus(ProtoId<MarkingCategoryPrototype> categoryId, out bool isRequired, out int count, out int selected)
    {
        isRequired = false;
        count = -1;
        selected = 0;

        foreach (var layers in _marking.GetLayers(categoryId))
        {
            if (!_markingsData.TryGetValue(layers, out var markingsData))
                continue;

            if (!_prototype.TryIndex(markingsData.Group, out var group))
                continue;

            if (!group.CategoriesData.TryGetValue(categoryId, out var categoryData))
                continue;

            isRequired &= categoryData.Required;
            count = int.Min(count, categoryData.Limit);
        }

        if (!_markings.TryGetValue(categoryId, out var markings))
            return;

        selected = markings.Count;
    }

    /// <summary>
    /// Sets the body type of all body appearance in the view model.
    /// </summary>
    /// <param name="bodyTypeId">The new body type id.</param>
    public void SetBodyType(ProtoId<BodyTypePrototype> bodyTypeId)
    {
        foreach (var (layer, data) in _appearanceData)
        {
            _appearanceData[layer] = data with { BodyType = bodyTypeId };
        }
        BodyAppearanceDataChanged?.Invoke();
    }

    /// <summary>
    /// Sets the body color of all body appearances in the view model.
    /// </summary>
    /// <param name="bodyColorGroupId">The body coloration id which color we want to change.</param>
    /// <param name="color">The new color.</param>
    public void SetColor(ProtoId<BodyColorGroupPrototype> bodyColorGroupId, Color color)
    {
        foreach (var (layer, data) in _appearanceData)
        {
            var colorGroups = data.ColorGroups.ShallowClone();
            colorGroups[bodyColorGroupId] = color;

            _appearanceData[layer] = data with { ColorGroups = colorGroups };
        }
        BodyAppearanceDataChanged?.Invoke();
    }

    /// <summary>
    /// Sets the sex of all body appearance in the view model.
    /// </summary>
    /// <param name="sex">The new sex.</param>
    public void SetSex(Sex sex)
    {
        foreach (var (layer, data) in _appearanceData)
        {
            _appearanceData[layer] = data with { Sex = sex };
        }
        BodyAppearanceDataChanged?.Invoke();
    }

    /// <summary>
    /// Ensures the markings within the model are valid.
    /// </summary>
    public void ValidateMarkings()
    {
        foreach (var (layer, markingsData) in _markingsData)
        {
            if (!_appearanceData.TryGetValue(layer, out var appearanceData))
                continue;

            foreach (var category in _marking.GetCategories(layer))
            {
                var markings = _markings.GetValueOrDefault(category)?.ShallowClone() ?? [];

                _marking.EnsureValidColors(markings, markingsData.Group, appearanceData.ColorGroups);
                _marking.EnsureValidGroupAndSex(markings, markingsData.Group, appearanceData.Sex);
                _marking.EnsureValidLimits(markings, markingsData.Group, appearanceData.ColorGroups);

                _markings[category] = markings;
            }
        }

        MarkingsReset?.Invoke();
    }
}

/// <summary>
/// Specifies whether an item in a list will be moved to before or after a corresponding index.
/// </summary>
public enum CandidatePosition
{
    Before,
    After,
}
