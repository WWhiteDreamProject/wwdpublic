using Content.Shared._NC.Bank.Components; // <--- ИСПРАВЛЕНО
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._NC.Bank.ATM
{
    public sealed class AtmBoundUserInterface : BoundUserInterface
    {
        private AtmWindow? _window;

        public AtmBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new AtmWindow();
            
            _window.OnWithdraw += amount => SendMessage(new AtmWithdrawMessage(amount));
            _window.OnDeposit += () => SendMessage(new AtmDepositMessage());

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not AtmBoundUserInterfaceState castState)
                return;

            _window?.UpdateState(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _window?.Dispose();
        }
    }
}