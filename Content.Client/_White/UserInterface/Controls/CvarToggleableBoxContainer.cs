using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client._White.UserInterface.Controls;

public sealed class CvarToggleableBoxContainer : BoxContainer
{
    private string? _cvar;
    [ViewVariables]
    public string? CVar { get => _cvar; set => Subscribe(value); }

    private bool _flip;
    [ViewVariables]
    public bool Flip
    {
        get => _flip;
        set
        {
            _flip = value;
            Refresh();
        }
    }

    private void UpdateVisibility(bool value) => Visible = value ^ _flip;

    private void Subscribe(string? newCVar)
    {
        if (_cvar is not null)
            IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged<bool>(_cvar, UpdateVisibility);
        if (newCVar is not null)
            IoCManager.Resolve<IConfigurationManager>().OnValueChanged<bool>(newCVar, UpdateVisibility, true);
        _cvar = newCVar;
    }

    private void Refresh()
    {
        if (_cvar is not null)
            UpdateVisibility(IoCManager.Resolve<IConfigurationManager>().GetCVar<bool>(_cvar));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_cvar is not null)
            IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged<bool>(_cvar, UpdateVisibility);
    }
}
