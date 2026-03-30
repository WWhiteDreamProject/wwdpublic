using System;
using Content.Shared._NC.Cyberware.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NC.Cyberware.UI;

[UsedImplicitly]
public sealed class BiomonitorBoundUserInterface : BoundUserInterface
{
    private BiomonitorWindow? _window;

    public BiomonitorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new BiomonitorWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _window?.Dispose();
        _window = null;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not BiomonitorBoundUserInterfaceState s)
            return;

        _window.Update(s.HealingCount, s.TraumaCount, s.CurrentHumanity, s.MaxHumanity);
    }
}
