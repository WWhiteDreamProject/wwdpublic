using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared._NC.Bank.Consoles;

namespace Content.Client._NC.Bank.Consoles
{
    [UsedImplicitly]
    public sealed class FactionBankConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private FactionBankConsoleWindow? _window;

        public FactionBankConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new FactionBankConsoleWindow();
            _window.OnClose += Close;

            _window.OnWithdraw += (amount, desc) => SendMessage(new FactionBankWithdrawMessage(amount, desc));
            _window.OnDeposit += (amount, desc) => SendMessage(new FactionBankDepositMessage(amount, desc));

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is FactionBankConsoleState cast)
            {
                _window?.UpdateState(cast.Balance, cast.Title, cast.Logs);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
