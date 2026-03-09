using Content.Shared._NC.Decryption.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Decryption.UI;

[UsedImplicitly]
public sealed class DecryptionBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DecryptionWindow? _window;

    public DecryptionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new DecryptionWindow();
        _window.OnClose += Close;
        _window.OnStart += () => SendMessage(new DecryptionStartMessage());
        _window.OnEject += () => SendMessage(new DecryptionEjectCarrierMessage());
        _window.OnMatrixClick += index => SendMessage(new DecryptionMatrixClickMessage(index));

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not DecryptionBoundUiState cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
