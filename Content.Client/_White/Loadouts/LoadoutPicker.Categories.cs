using System.Linq;
using Content.Shared.Clothing.Loadouts.Prototypes;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;


namespace Content.Client._White.Loadouts;


public sealed partial class LoadoutPicker
{
    private List<LoadoutCategoryPrototype> _rootCategories = [];

    public IEnumerable<LoadoutCategoryPrototype> GetRootCategories()
    {
        return _rootCategories;
    }

    private ILoadoutMenuEntry? _currentEntry;

    public ILoadoutMenuEntry CurrentEntry
    {
        get => _currentEntry ?? throw new InvalidOperationException();
        set
        {
            _currentEntry?.Exit(Loadouts, this);
            ClearupEdit();
            ClearLoadoutCategoryButtons();
            _currentEntry = value;
            EntryBackButton.Visible = _currentEntry.Parent != null;
            _currentEntry.Act(Loadouts, this);
        }
    }


    private void CacheRootCategories()
    {
        _rootCategories =
            _prototypeManager.EnumeratePrototypes<LoadoutCategoryPrototype>().Where(p => p.Root)
                .ToList();
    }

    private void InitializeCategories()
    {
        EntryBackButton.OnPressed += EntryBackButtonPressed;
        var rootEntry = new LoadoutEntriesContainerMenuEntry("root");
        foreach (var category in GetRootCategories())
        {
            rootEntry.AddChild(BuildMenuGroup(category.ID).Item1);
        }

        CurrentEntry = rootEntry;
    }

    private void EntryBackButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        if (!string.IsNullOrEmpty(LoadoutSearch.Text))
        {
            LoadoutSearch.Clear();
            Populate("");
            return;
        }

        if (CurrentEntry.Parent != null)
        {
            _selectedLoadoutCategory = null;
            CurrentEntry = CurrentEntry.Parent;
        }
    }

    private (ILoadoutMenuEntry, int) BuildMenuGroup(ProtoId<LoadoutCategoryPrototype> categoryPrototypeId)
    {
        var weight = 0;

        if (!_prototypeManager.TryIndex(categoryPrototypeId, out var categoryPrototype))
            throw new Exception($"Cannot load prototype {categoryPrototypeId}");

        if (categoryPrototype.SubCategories.Count == 0)
        {
            if (!_loadoutCache.ContainsKey(categoryPrototypeId) ||
            _loadoutCache[categoryPrototypeId].Count == 0)
                return (new LoadoutCategoryShowMenuEntry(categoryPrototypeId), 0);

            return (new LoadoutCategoryShowMenuEntry(categoryPrototypeId), 1);
        }

        var entry = new LoadoutEntriesContainerMenuEntry(categoryPrototypeId);

        foreach (var category in categoryPrototype.SubCategories)
        {
            var child = BuildMenuGroup(category);
            if (child.Item2 == 0) continue;
            entry.AddChild(child.Item1);
            weight += child.Item2;
        }

        return (entry, weight);
    }

    /// <summary>
    /// Checks if the category has available loadouts (taking ShowUnusable into account)
    /// </summary>
    private bool HasVisibleLoadouts(ProtoId<LoadoutCategoryPrototype> categoryId)
    {
        if (!_loadoutCache.TryGetValue(categoryId, out var loadouts))
            return false;

        if (CharacterRequirementsArgs == null)
            return loadouts.Count > 0;

        foreach (var loadoutProto in loadouts)
        {
            if (_showUnusable)
                return true;

            var canWear = CheckLoadoutWearable(loadoutProto);
            if (canWear)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether the character can wear the given loadout
    /// </summary>
    private bool CheckLoadoutWearable(LoadoutPrototype prototype)
    {
        if (CharacterRequirementsArgs == null)
            return true;

        if (prototype.Cost > LoadoutPoint && !_selectedLoadouts.ContainsKey(prototype.ID))
            return false;

        foreach (var requirement in prototype.Requirements)
        {
            if (!CharacterRequirementsArgs.IsValid(requirement, prototype, out _))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively checks whether a category or its subcategories have available loadouts
    /// </summary>
    private bool IsCategoryVisible(ProtoId<LoadoutCategoryPrototype> categoryId)
    {
        if (!_prototypeManager.TryIndex(categoryId, out var categoryPrototype))
            return false;

        if (categoryPrototype.SubCategories.Count == 0)
        {
            return HasVisibleLoadouts(categoryId);
        }

        foreach (var subCategory in categoryPrototype.SubCategories)
        {
            if (IsCategoryVisible(subCategory))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the visibility of all categories in the current menu
    /// </summary>
    private void UpdateCategoriesVisibility()
    {
        if (_currentEntry is LoadoutEntriesContainerMenuEntry container)
        {
            container.UpdateChildrenVisibility(this);
        }
    }
}
