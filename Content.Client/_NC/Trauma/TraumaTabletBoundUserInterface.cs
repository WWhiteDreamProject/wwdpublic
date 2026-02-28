using Content.Shared._NC.Trauma;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Trauma
{
    public sealed class TraumaTabletBoundUserInterface : BoundUserInterface
    {
        private TraumaTabletWindow? _window;

        public TraumaTabletBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new TraumaTabletWindow();
            _window.OnClose += Close;

            _window.OnOpenMap += (netTarget) =>
            {
                SendMessage(new TraumaOpenMapMsg(netTarget));
            };

            // Оперативник нажал "Выполнено"
            _window.OnCompleteMission += (netTarget) =>
            {
                SendMessage(new TraumaCompleteMissionMsg(netTarget));
            };

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is TraumaTabletState castState)
            {
                _window?.UpdateState(castState.ActivePatient, castState.IsPendingCompletion);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _window?.Dispose();
        }
    }
}
