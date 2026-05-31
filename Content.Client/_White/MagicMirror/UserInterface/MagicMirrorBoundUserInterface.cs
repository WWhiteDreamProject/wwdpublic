using Content.Client._White.Humanoid;
using Content.Shared._White.MagicMirror.Systems;
using Robust.Client.UserInterface;

namespace Content.Client._White.MagicMirror.UserInterface;

public sealed class MagicMirrorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MagicMirrorWindow? _window;

    private readonly MarkingsViewModel _model = new();

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MagicMirrorWindow>();
        _window.MarkingsPicker.SetModel(_model);

        _model.MarkingsChanged += _ =>
        {
            SendMessage(new MagicMirrorSelectMessage(_model.Markings));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MagicMirrorUiState data)
            return;

        _model.MarkingsData = data.MarkingsData;
        _model.AppearanceData = data.AppearanceData;
        _model.Markings = data.Markings;
    }
}

