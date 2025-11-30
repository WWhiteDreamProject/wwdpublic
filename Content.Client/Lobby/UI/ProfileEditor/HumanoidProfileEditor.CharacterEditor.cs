using Robust.Client.UserInterface.Controls;


namespace Content.Client.Lobby.UI;


public sealed partial class HumanoidProfileEditor
{
    private ICharacterMenuEntry _currentCharacterMenuEntry = default!;
    public ICharacterMenuEntry CurrentCharacterMenuEntry
    {
        get => _currentCharacterMenuEntry;
        set
            {
                _currentCharacterMenuEntry = value;
                CharacterEditButtonContainer.Children.Clear();
                CharacterEditBackButton.Visible = _currentCharacterMenuEntry.Parent != null;
                _currentCharacterMenuEntry.Act(this, CharacterEditButtonContainer);
            }
    }

    private void InitializeCharacterMenu()
    {
        CharacterEditButtonContainer.Children.Clear();
        CharacterEditBackButton.OnPressed += CharacterEditBackButtonPressed;

        var root = new CharacterContainerMenuEntry("root");

        var head = new CharacterContainerMenuEntry("head");
        var torso = new CharacterContainerMenuEntry("torso");
        var hips = new CharacterContainerMenuEntry("hips");
        var legs = new CharacterContainerMenuEntry("legs");

        var headreal  = new CharacterContainerMenuEntry("head");
        var layout = new CharacterContainerMenuEntry("layout");
        var eyes = new CharacterContainerMenuEntry("eyes");

        head.AddChild(headreal, layout, eyes);

        root.AddChild(head, torso, hips, legs);
        CurrentCharacterMenuEntry = root;
    }

    private void CharacterEditBackButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        if (CurrentCharacterMenuEntry.Parent != null)
            CurrentCharacterMenuEntry = CurrentCharacterMenuEntry.Parent;

    }
}

public interface ICharacterMenuEntry
{
    public ICharacterMenuEntry? Parent { get; set; }
    public string Label { get; }

    public void Act(HumanoidProfileEditor editor, BoxContainer characterSettingsContainer);
}

public sealed class CharacterContainerMenuEntry : ICharacterMenuEntry
{
    public ICharacterMenuEntry? Parent { get; set;}
    public string Label { get; }

    private readonly List<ICharacterMenuEntry> _children = [];

    public CharacterContainerMenuEntry(string label)
    {
        Label = label;
    }

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
        foreach (var menuEntry in _children)
        {
            var button = new Button()
            {
                Text = menuEntry.Label
            };
            button.OnPressed += args => editor.CurrentCharacterMenuEntry = menuEntry;
            characterSettingsContainer.AddChild(button);
        }
    }
}

