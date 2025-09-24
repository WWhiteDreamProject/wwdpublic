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
    private string? _cvar = string.Empty;
    [ViewVariables]
    public string? SubscribedCVar { get => _cvar; set => Subscribe(value); }

    private bool _flip = false;
    [ViewVariables]
    public bool Flip { get => _flip; set { _flip = value; Refresh(); } }

    private void UpdateVisibility(bool value) => Visible = value ^ _flip;

    private void Subscribe(string? newCVar)
    {
        if(SubscribedCVar is not null)
            IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged<bool>(SubscribedCVar, UpdateVisibility);
        if(newCVar is not null)
            IoCManager.Resolve<IConfigurationManager>().OnValueChanged<bool>(newCVar, UpdateVisibility, true);
        SubscribedCVar = newCVar;
    }

    private void Refresh()
    {
        if (SubscribedCVar is not null)
            UpdateVisibility(IoCManager.Resolve<IConfigurationManager>().GetCVar<bool>(SubscribedCVar));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if(SubscribedCVar is not null)
            IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged<bool>(SubscribedCVar, UpdateVisibility);
    }
}
