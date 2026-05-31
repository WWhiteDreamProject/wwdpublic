using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.Appearance;
using Content.Shared._White.Body;
using Content.Shared._White.Humanoid.Markings;
using Content.Shared._White.Humanoid.Markings.Managers;
using Content.Shared._White.Humanoid.Markings.Prototypes;
using Content.Shared._White.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._White.Humanoid;

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
    private Dictionary<BodyProviderType, BodyAppearanceData> _appearanceData = new();

    /// <summary>
    /// The currently applied set of markings.
    /// </summary>
    private Dictionary<ProtoId<MarkingCategoryPrototype>, List<Marking>> _markings = new();

    /// <summary>
    /// The marking data the view model is concerned with.
    /// </summary>
    private Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> _markingsData = new();

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
    public Dictionary<BodyProviderType, BodyAppearanceData> AppearanceData
    {
        get => _appearanceData;
        set
        {
            _appearanceData = value.ShallowClone();
            BodyAppearanceDataChanged?.Invoke();
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

    /// <inheritdoc cref="_markingsData"/>
    /// <seealso cref="MarkingsDataChanged" />
    public Dictionary<ProtoId<MarkingCategoryPrototype>, MarkingsData> MarkingsData
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
        if (!_marking.TryGetMarking(id, out var markingPrototype))
            return false;

        if (markingPrototype.ForcedColoring)
            return false;

        if (!_markingsData.TryGetValue(markingPrototype.Category, out var markingsData))
            return false;

        if (!_prototype.TryIndex(markingsData.Group, out var groupPrototype))
            return false;

        if (!groupPrototype.Appearances.TryGetValue(markingPrototype.Category, out var appearance))
            return true;

        return !appearance.MatchSkin;
    }

    /// <summary>
    /// Attempts to remove a marking from the current set of markings.
    /// </summary>
    /// <param name="id">The marking ID to deselect.</param>
    /// <returns>Whether the marking was successfully removed from the set of markings.</returns>
    public bool TryDeselectMarking(ProtoId<MarkingPrototype> id)
    {
        if (!_marking.TryGetMarking(id, out var markingPrototype))
            return false;

        if (!_markingsData.TryGetValue(markingPrototype.Category, out var markingsData))
            return false;

        if (!_prototype.TryIndex(markingsData.Group, out var groupPrototype))
            return false;

        _markings[markingPrototype.Category] = _markings.GetValueOrDefault(markingPrototype.Category) ?? [];
        var markings = _markings[markingPrototype.Category];

        var count = GetMarkingCategoryCount(markingPrototype.Category);
        if (count == 0)
            return false;

        var limits = groupPrototype.Limits.GetValueOrDefault(markingPrototype.Category);
        if (EnforceLimits && limits is not null && limits.Required && count <= 1)
            return false;

        var removedMarkingColors = new HashSet<Color>();
        foreach (var marking in markings.ToList())
        {
            if (marking.Id != id)
                continue;

            removedMarkingColors.Add(marking.Color);
            markings.Remove(marking);
        }

        _previousColors[id] = removedMarkingColors.ToList();
        MarkingsChanged?.Invoke(markingPrototype.Category);

        return true;
    }

    /// <summary>
    /// Returns the currently applied markings by its ID.
    /// </summary>
    /// <returns>The markings currently applied if it exists, otherwise null</returns>
    public bool TryGetMarkings(ProtoId<MarkingPrototype> id, [NotNullWhen(true)] out List<Marking>? markings)
    {
        markings = null;
        if (!_marking.TryGetMarking(id, out var markingPrototype))
            return false;

        if (!_markings.TryGetValue(markingPrototype.Category, out var markingsSet))
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
    /// Returns the currently applied marking by category.
    /// </summary>
    /// <returns>The markings currently applied if it exists, otherwise null</returns>
    public bool TryGetSelectedMarkings(ProtoId<MarkingCategoryPrototype> category, [NotNullWhen(true)] out List<Marking>? markings)
    {
        return _markings.TryGetValue(category, out markings);
    }

    /// <summary>
    /// Attempts to add a marking to the current set of markings.
    /// </summary>
    /// <param name="id">The marking ID to select.</param>
    /// <returns>Whether the marking was successfully added to the set of markings.</returns>
    public bool TrySelectMarking(ProtoId<MarkingPrototype> id)
    {
        if (!_marking.TryGetMarking(id, out var markingPrototype))
            return false;

        if (!_prototype.TryIndex(markingPrototype.Category, out var categoryPrototype))
            return false;

        if (!_appearanceData.TryGetValue(categoryPrototype.Type, out var appearanceData))
            return false;

        if (!_markingsData.TryGetValue(markingPrototype.Category, out var markingsData))
            return false;

        if (!_prototype.TryIndex(markingsData.Group, out var groupPrototype))
            return false;

        if (EnforceGroupAndSexRestrictions && !_marking.CanBeApplied(markingPrototype, markingsData.Group, appearanceData.Sex))
            return false;

        _markings[markingPrototype.Category] = _markings.GetValueOrDefault(markingPrototype.Category) ?? [];
        var markings = _markings[markingPrototype.Category];

        var limits = groupPrototype.Limits.GetValueOrDefault(markingPrototype.Category);
        var count = GetMarkingCategoryCount(markingPrototype.Category);
        if (limits is not null && limits.Limit == 1 && count == 1)
        {
            markings.Clear();
            count = 0;
        }

        if (limits is not null && EnforceLimits && count >= limits.Limit)
            return false;

        var colors = _previousColors.GetValueOrDefault(id) ?? Marking.GetMarkingColors(markingPrototype, appearanceData.BodyColoration, markings);
        for (var i = markingPrototype.Markings.Count - 1; i >= 0; i--)
        {
            var data =  markingPrototype.Markings[i];

            var marking = new Marking(data.Layer, id, data.Sprite, colors[i])
            {
                Forced = AnyEnforcementsLifted,
            };

            markings.Add(marking);
        }

        MarkingsChanged?.Invoke(markingPrototype.Category);
        return true;
    }

    /// <summary>
    /// Attempts to set the color of the specified marking at the given index.
    /// </summary>
    /// <param name="id">The marking ID to select.</param>
    /// <param name="index">The index within the marking's color array to set.</param>
    /// <param name="color">The new color to set</param>
    /// <returns>Whether the marking was successfully set color.</returns>
    public bool TrySetMarkingColor(ProtoId<MarkingPrototype> id, int index, Color color)
    {
        if (!_marking.TryGetMarking(id, out var markingPrototype))
            return false;

        if (!_markings.TryGetValue(markingPrototype.Category, out var markings))
            return false;

        var markingData = markingPrototype.Markings[index];
        var markingIndex = markings.FindIndex(marking => marking.Id == id && Equals(marking.Layer, markingData.Layer) && marking.Sprite == markingData.Sprite);
        if (markingIndex == -1)
            return false;

        markings[markingIndex] = markings[markingIndex].WithColor(color);
        MarkingsChanged?.Invoke(markingPrototype.Category);
        return true;
    }

    /// <summary>
    /// Calculates the number of unique marking prototypes present within a specified category.
    /// </summary>
    /// <param name="category">The category to check for unique markings within.</param>
    /// <returns>The total count of distinct marking prototypes found in the given category.</returns>
    public int GetMarkingCategoryCount(ProtoId<MarkingCategoryPrototype> category)
    {
        _markings[category] = _markings.GetValueOrDefault(category) ?? [];
        var markings = _markings[category];

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
    /// <param name="category">The category to reorder the markings of.</param>
    /// <param name="id">The marking to reorder.</param>
    /// <param name="position">Whether the marking should be moved to before or after the given index.</param>
    /// <param name="positionIndex">The new position index of the marking.</param>
    public void ChangeMarkingOrder(
        ProtoId<MarkingCategoryPrototype> category,
        ProtoId<MarkingPrototype> id,
        CandidatePosition position,
        int positionIndex
    )
    {
        if (!_markings.TryGetValue(category, out var markings))
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

        MarkingsChanged?.Invoke(category);
    }

    /// <summary>
    /// Gets the status data for a marking category.
    /// </summary>
    /// <param name="category">The category to check for unique markings within.</param>
    /// <param name="isRequired">Whether this layer requires at least one marking to be selected.</param>
    /// <param name="count">The maximum number of markings that can be selected.</param>
    /// <param name="selected">The currently selected number of markings.</param>
    public void GetMarkingStatus(ProtoId<MarkingCategoryPrototype> category, out bool isRequired, out int count, out int selected)
    {
        isRequired = false;
        count = -1;
        selected = 0;

        if (!_markingsData.TryGetValue(category, out var markingsData))
            return;

        if (!_prototype.TryIndex(markingsData.Group, out var groupPrototype))
            return;

        if (!groupPrototype.Limits.TryGetValue(category, out var limits))
            return;

        isRequired = limits.Required;
        count = limits.Limit;

        if (!_markings.TryGetValue(category, out var markings))
            return;

        selected = markings.Count;
    }

    /// <summary>
    /// Sets the body type of all body appearance in the view model.
    /// </summary>
    /// <param name="bodyType">The new body type</param>
    public void SetBodyType(ProtoId<BodyTypePrototype> bodyType)
    {
        foreach (var (type, data) in _appearanceData)
        {
            _appearanceData[type] = data with { BodyType = bodyType };
        }
        BodyAppearanceDataChanged?.Invoke();
    }

    /// <summary>
    /// Sets the body color of all body appearances in the view model.
    /// </summary>
    /// <param name="coloration">The body coloration color changes.</param>
    /// <param name="color">The new color</param>
    public void SetColor(ProtoId<BodyColorationPrototype> coloration, Color color)
    {
        foreach (var (type, data) in _appearanceData)
        {
            var bodyColoration = data.BodyColoration.ShallowClone();
            bodyColoration[coloration] = color;

            _appearanceData[type] = data with { BodyColoration = bodyColoration };
        }
        BodyAppearanceDataChanged?.Invoke();
    }

    /// <summary>
    /// Sets the sex of all body appearance in the view model.
    /// </summary>
    /// <param name="sex">The new sex</param>
    public void SetSex(Sex sex)
    {
        foreach (var (type, data) in _appearanceData)
        {
            _appearanceData[type] = data with { Sex = sex };
        }
        BodyAppearanceDataChanged?.Invoke();
    }

    /// <summary>
    /// Ensures the markings within the model are valid.
    /// </summary>
    public void ValidateMarkings()
    {
        foreach (var (category, markingsData) in _markingsData)
        {
            if (!_prototype.TryIndex(category, out var categoryPrototype))
            {
                _markings.Remove(category);
                continue;
            }

            if (!_appearanceData.TryGetValue(categoryPrototype.Type, out var appearanceData))
            {
                _markings.Remove(category);
                continue;
            }

            var markings = _markings.GetValueOrDefault(category)?.ShallowClone() ?? [];

            _marking.EnsureValidColors(markings, markingsData.Group, appearanceData.BodyColoration);
            _marking.EnsureValidGroupAndSex(markings, markingsData.Group, appearanceData.Sex);
            _marking.EnsureValidLimits(markings, markingsData.Group, appearanceData.BodyColoration);

            _markings[category] = markings;
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
