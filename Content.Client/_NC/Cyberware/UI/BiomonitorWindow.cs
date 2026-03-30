using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._NC.Cyberware.UI;

// NC - Окно биомонитора: отображает состояние терапии пациента
public sealed class BiomonitorWindow : DefaultWindow
{
    private readonly Label _wordsLabel;
    private readonly Label _humanityLabel;

    public BiomonitorWindow()
    {
        Title = "Биомонитор";
        MinSize = new System.Numerics.Vector2(240, 120);

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8
        };

        _wordsLabel = new Label();
        _humanityLabel = new Label();

        vbox.AddChild(_wordsLabel);
        vbox.AddChild(_humanityLabel);

        Contents.AddChild(vbox);
    }

    public void Update(int healing, int trauma, float current, float max)
    {
        _wordsLabel.Text = $"Слова: лечебных {healing}, болевых {trauma}";
        _humanityLabel.Text = $"Человечность: {current:0}/{max:0}";
    }
}
