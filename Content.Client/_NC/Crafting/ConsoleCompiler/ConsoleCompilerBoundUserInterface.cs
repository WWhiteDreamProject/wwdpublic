using Content.Shared._NC.Crafting.ConsoleCompiler;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Crafting.ConsoleCompiler;

/// <summary>
/// Клиентский BUI для консоли-компилятора (Техно-Принтер).
/// </summary>
[UsedImplicitly]
public sealed class ConsoleCompilerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ConsoleCompilerWindow? _window;

    public ConsoleCompilerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new ConsoleCompilerWindow();
        _window.OnClose += Close;

        // Подписки на кнопки → отправка BUI Messages
        _window.OnDigitize += () => SendMessage(new ConsoleCompilerDigitizeMessage());
        _window.OnEjectReceiver += () => SendMessage(new ConsoleCompilerEjectReceiverMessage());
        _window.OnEjectMaster += () => SendMessage(new ConsoleCompilerEjectMasterMessage());
        _window.OnPrint += isBlueprint => SendMessage(new ConsoleCompilerPrintMessage(isBlueprint));

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not ConsoleCompilerBoundUiState cast)
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
