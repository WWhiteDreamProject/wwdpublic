using Content.Client.Administration.Managers;
using Content.Shared._White.Humanoid.Markings.UserInterface;
using Content.Shared.Administration;
using Robust.Client.UserInterface;

namespace Content.Client._White.Humanoid.Markings.UserInterface;

public sealed class MarkingModifierBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IClientAdminManager _admin = default!;

    private MarkingModifierWindow? _window;

    private readonly MarkingsViewModel _model = new();

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<MarkingModifierWindow>();

        _window.MarkingPicker.SetModel(_model);

        _window.RespectLimits.OnPressed += args => _model.EnforceLimits = args.Button.Pressed;
        _window.RespectGroupSex.OnPressed += args => _model.EnforceGroupAndSexRestrictions = args.Button.Pressed;
        _window.RespectChecks.Visible = _admin.HasFlag(AdminFlags.Fun);

        _admin.AdminStatusUpdated += OnAdminStatusUpdated;

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

    public void OnAdminStatusUpdated()
    {
        if (_window == null)
            return;

        _window.RespectChecks.Visible = _admin.HasFlag(AdminFlags.Fun);
    }

    private void SendMarkingSet()
    {
        SendMessage(new MarkingModifierMarkingSetMessage(_model.Markings));
    }
}
