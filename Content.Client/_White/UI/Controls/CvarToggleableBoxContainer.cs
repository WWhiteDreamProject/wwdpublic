using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._White.UI.Controls;

public sealed class CvarToggleableBoxContainer : BoxContainer
{
    private string _cvar = string.Empty;
    [ViewVariables]
    public string CVar { get => _cvar; set { _cvar = value; Resub(); } }

    private bool _flip = false;
    [ViewVariables]
    public bool Flip { get => _flip; set { _flip = value; Resub(); } }

    private bool _subbed = false;
    private bool _init = false;

    private void UpdateVisibility(bool value) => Visible = value ^ _flip;

    private void Resub()
    {
        if (string.IsNullOrWhiteSpace(_cvar))
            return;

        if(_subbed)
            IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged<bool>(CVar, UpdateVisibility);
        IoCManager.Resolve<IConfigurationManager>().OnValueChanged<bool>(CVar, UpdateVisibility, true);
        _subbed = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged<bool>(CVar, UpdateVisibility);
    }
}
