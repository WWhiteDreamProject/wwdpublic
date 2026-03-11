using Content.Shared._NC.Dispatch;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Dispatch
{
    public sealed class OverwatchConsoleBoundUserInterface : BoundUserInterface
    {
        private OverwatchConsoleWindow? _window;

        public OverwatchConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new OverwatchConsoleWindow();
            _window.OnClose += Close;
            _window.OnAlertAction += (id, action) => SendMessage(new OverwatchAlertActionMessage(id, action));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is OverwatchConsoleState castState)
            {
                _window?.UpdateState(castState);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _window?.Dispose();
        }
    }
}
