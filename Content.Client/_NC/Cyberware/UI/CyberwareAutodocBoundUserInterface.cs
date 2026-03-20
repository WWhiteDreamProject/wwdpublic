using Content.Shared._NC.Cyberware;
using Content.Shared._NC.Cyberware.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Cyberware.UI;

/// <summary>
///     Связывает данные сервера с XAML окном Автодока.
/// </summary>
[UsedImplicitly]
public sealed class CyberwareAutodocBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CyberwareAutodocWindow? _window;

    public CyberwareAutodocBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new CyberwareAutodocWindow();

        if (State != null)
            UpdateState(State);

        _window.OpenCentered();
        _window.OnClose += Close;

        // Когда рипердок нажимает ИНТЕГРИРОВАТЬ (выбран имплант из dropdown)
        _window.OnInstallPressed += (slot, implant) =>
        {
            if (implant.HasValue)
                SendMessage(new AutodocInstallBuiMsg(implant.Value, slot));
        };

        // Когда рипердок нажимает ИЗВЛЕЧЬ (×) на конкретном подслоте
        _window.OnRemovePressed += (slot) =>
        {
            SendMessage(new AutodocRemoveBuiMsg(slot));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AutodocBoundUserInterfaceState autodocState)
            return;

        _window?.UpdateState(autodocState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}