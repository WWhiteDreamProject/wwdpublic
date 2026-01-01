using System.Linq;
using System.Numerics;
using Content.Shared._White.CharacterEditor;
using Content.Shared.CCVar;
using Content.Shared.Clothing.Loadouts.Prototypes;
using Content.Shared.Clothing.Loadouts.Systems;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Client.Lobby.UI;


public sealed partial class HumanoidProfileEditor
{
    [ValidatePrototypeId<CharacterMenuRootPrototype>]
    public readonly ProtoId<CharacterMenuRootPrototype> MainCharacterMenuRoot = "root";
    private ICharacterMenuEntry? _currentCharacterMenuEntry;
    private bool LoadoutsEnabled => _cfgManager.GetCVar(CCVars.GameLoadoutsEnabled);

    public ICharacterMenuEntry CurrentCharacterMenuEntry
    {
        get => _currentCharacterMenuEntry!;
        set
            {
                _currentCharacterMenuEntry?.Exit(this, CharacterEditButtonContainer);
                CharacterEditContainerScroll.Visible = false;
                _currentCharacterMenuEntry = value;
                CharacterEditButtonContainer.Children.Clear();
                CharacterEditBackButton.Visible = _currentCharacterMenuEntry.Parent != null;
                _currentCharacterMenuEntry.Act(this, CharacterEditButtonContainer);
            }
    }

    private void InitializeCharacterMenu()
    {
        PreviewRotateRightButton.OnPressed += args =>
            CharacterSpriteView.OverrideDirection =
                (Direction)(((int)(CharacterSpriteView.OverrideDirection ?? Direction.South) + 2) % 8);

        PreviewRotateLeftButton.OnPressed += args =>
            CharacterSpriteView.OverrideDirection =
                (Direction)(((int)(CharacterSpriteView.OverrideDirection ?? Direction.South) + 6) % 8);

        CharacterEditButtonContainer.Children.Clear();
        CharacterEditBackButton.OnPressed += CharacterEditBackButtonPressed;

        Loadouts.OnLoadoutsChanged += OnLoadoutsChange;
    }

    private void CharacterEditBackButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        if (CurrentCharacterMenuEntry.Parent != null)
            CurrentCharacterMenuEntry = CurrentCharacterMenuEntry.Parent;
    }

    private void CharacterMenuUpdateRequired()
    {
        var path = new Stack<string>();

        if (_currentCharacterMenuEntry is null)
        {
            CurrentCharacterMenuEntry = BuildMenuGroup(MainCharacterMenuRoot);
            return;
        }

        var currMenu = CurrentCharacterMenuEntry;
        do
        {
            path.Push(currMenu.Label);
        } while ((currMenu = currMenu.Parent) != null);

        currMenu = BuildMenuGroup(MainCharacterMenuRoot);

        path.TryPop(out _); // Popup root

        while (path.TryPop(out var t1))
        {
            if(currMenu is not CharacterContainerMenuEntry containerMenuEntry)
                 break;
            var nextMenu = containerMenuEntry.Children.FirstOrDefault(p => p.Label == t1);
            if(nextMenu is null)
                break;
            currMenu = nextMenu;
        }

        CurrentCharacterMenuEntry = currMenu;
    }

    private void UpdateLoadouts()
    {
        if (Profile == null)
            return;

        var highJob = _controller.GetPreferredJob(Profile);

        Loadouts.SetData(
            Profile.LoadoutPreferencesList,
            new(
                highJob,
                Profile,
                _requirements.GetRawPlayTimeTrackers(),
                _requirements.IsWhitelisted()
                )
            );
    }

    private void CheckpointLoadouts()
    {
        if (Profile == null)
            return;
        Loadouts.SetCheckpoint();
    }

    private void OnLoadoutsChange(List<Loadout> loadouts)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithLoadoutPreference(loadouts);
        ReloadProfilePreview();
        ReloadClothes();
        UpdateLoadouts();
    }

    private ICharacterMenuEntry BuildMenuGroup(ProtoId<CharacterMenuRootPrototype> protoId)
    {
        if(!_prototypeManager.TryIndex(protoId, out var prototype))
            throw new Exception($"Cannot load prototype {protoId}");
        return BuildMenuGroup(prototype.Root, this).Item1;
    }

    private (ICharacterMenuEntry, int) BuildMenuGroup(CharacterMenuGroup group, HumanoidProfileEditor editor)
    {
        var weight = 0;
        var entry = new CharacterContainerMenuEntry(group.Name);

        foreach (var category in group.Categories)
        {
            if(!editor.Markings.IsCategoryValid(category))
                continue;

            entry.AddChild(new MarkShowMenuEntry(category));
            weight++;
        }

        if (LoadoutsEnabled)
        {
            foreach (var loadoutCategory in group.LoadoutCategories)
            {
                if(!editor.Loadouts.IsCategoryValid(loadoutCategory))
                    continue;

                entry.AddChild(new LoadoutShowMenuEntry(loadoutCategory));
                weight++;
            }
        }

        foreach (var subGroup in group.SubGroups)
        {
            var (childEntry, weightChild) = BuildMenuGroup(subGroup, editor);
            if(weightChild == 0)
                continue;

            entry.AddChild(childEntry);
            weight += weightChild;
        }


        return weight == 1 ? (entry.Children[0], weight) : (entry, weight);
    }
}

public interface ICharacterMenuEntry
{
    public ICharacterMenuEntry? Parent { get; set; }
    public string Label { get; }
    public ResPath IconPath { get; }

    public void Act(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer);
    public void Exit(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer);
}

public sealed class LoadoutShowMenuEntry : ICharacterMenuEntry
{
    private readonly ProtoId<LoadoutCategoryPrototype> _loadoutCategory;
    public ICharacterMenuEntry? Parent { get; set; }
    public string Label { get; }
    public ResPath IconPath { get; } = new ResPath("/Textures/Interface/inventory.svg.192dpi.png");

    public LoadoutShowMenuEntry(ProtoId<LoadoutCategoryPrototype> loadoutCategory)
    {
        _loadoutCategory = loadoutCategory;
        Label = Loc.GetString($"loadout-category-{loadoutCategory}");
    }

    public void Act(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer)
    {
        editor.Loadouts.Visible = editor.Loadouts.LoadCategoryButtons(_loadoutCategory);
    }

    public void Exit(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer)
    {
        editor.Loadouts.Visible = false;
    }
}

public sealed class MarkShowMenuEntry : ICharacterMenuEntry
{
    public ICharacterMenuEntry? Parent { get; set; }
    public string Label { get; }
    public ResPath IconPath { get; } = new ResPath("/Textures/Interface/character.svg.192dpi.png");
    public MarkingCategories Category { get; set; }

    public MarkShowMenuEntry(MarkingCategories category)
    {
        Label = Loc.GetString($"markings-category-{category.ToString()}");
        Category = category;
    }

    public void Act(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer)
    {
        editor.Markings.Visible = editor.Markings.Select(Category);
    }

    public void Exit(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer)
    {
        editor.Markings.Visible = false;
    }
}

public sealed class CharacterContainerMenuEntry(string label) : ICharacterMenuEntry
{
    public ICharacterMenuEntry? Parent { get; set;}
    public string Label { get; } = label;
    public ResPath IconPath { get; } = new ResPath("/Textures/Interface/hamburger.svg.192dpi.png");

    private readonly List<ICharacterMenuEntry> _children = [];
    public IReadOnlyList<ICharacterMenuEntry> Children => _children;
    private readonly List<(BaseButton, Action<BaseButton.ButtonEventArgs>)> _currBrns = [];

    public void AddChild(params ICharacterMenuEntry[] children)
    {
        foreach (var child in children)
        {
            _children.Add(child);
            child.Parent = this;
        }
    }

    public void Act(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer)
    {
        if(_children.Count > 0)
            editor.CharacterEditContainerScroll.Visible = true;

        foreach (var menuEntry in _children)
        {
            var button = new Button()
            {
                Children =
                {
                    new BoxContainer()
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        SeparationOverride = 15,
                        Children =
                        {
                            new TextureRect()
                            {
                                TexturePath = menuEntry.IconPath.ToString(),
                                TextureScale = new Vector2(0.75f)
                            },
                            new Label()
                            {
                                Text = menuEntry.Label,
                            }
                        }
                    }
                }
            };

            Action<BaseButton.ButtonEventArgs> handler = (_) => editor.CurrentCharacterMenuEntry = menuEntry;
            button.OnPressed += handler;

            _currBrns.Add((button, handler));
            characterSettingsContainer.AddChild(button);
        }
    }

    public void Exit(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer)
    {
        foreach (var (button, handler) in _currBrns)
        {
            button.OnPressed -= handler;
        }
        _currBrns.Clear();
    }
}
