using Content.Shared._NC.Ncpd;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Ncpd
{
    public sealed class NcpdTabletBoundUserInterface : BoundUserInterface
    {
        private NcpdTabletWindow? _window;

        public NcpdTabletBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new NcpdTabletWindow();
            _window.OnClose += Close;
            _window.OnCallSelected += callId => SendMessage(new NcpdTabletSelectCallMsg(callId));
            _window.OnCallCleared += callId => SendMessage(new NcpdTabletClearCallMsg(callId));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not NcpdTabletState tabletState)
                return;

            _window?.UpdateState(tabletState);
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
