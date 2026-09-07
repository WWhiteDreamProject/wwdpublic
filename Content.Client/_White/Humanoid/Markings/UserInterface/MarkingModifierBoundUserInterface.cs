using Content.Shared._White.Appearance.Systems;
using Robust.Client.UserInterface;

namespace Content.Client._White.Humanoid.Markings.UserInterface;

public sealed class MarkingModifierBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MarkingModifierWindow? _window;

    private readonly MarkingsViewModel _model = new();

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<MarkingModifierWindow>();
        _window.MarkingPickerWidget.SetModel(_model);
        _window.RespectLimits.OnPressed += args => _model.EnforceLimits = args.Button.Pressed;
        _window.RespectGroupSex.OnPressed += args => _model.EnforceGroupAndSexRestrictions = args.Button.Pressed;

        _model.MarkingsChanged += _ => SendMarkingSet();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not MarkingModifierState cast)
            return;

        _model.AppearanceData = cast.AppearanceData;
        _model.MarkingsData = cast.MarkingsData;
        _model.Markings = cast.Markings;
    }

    private void SendMarkingSet()
    {
        SendMessage(new MarkingModifierMarkingSetMessage(_model.Markings));
    }
}
