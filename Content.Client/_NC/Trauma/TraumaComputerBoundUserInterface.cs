using Content.Shared._NC.Trauma;
using Robust.Client.GameObjects;

namespace Content.Client._NC.Trauma
{
    public sealed class TraumaComputerBoundUserInterface : BoundUserInterface
    {
        private TraumaComputerWindow? _window;

        public TraumaComputerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        // Вызывается при открытии интерфейса
        protected override void Open()
        {
            base.Open();
            _window = new TraumaComputerWindow();
            _window.OnClose += Close; // Закрыть BUI при закрытии окна

            // Подписываемся на событие изменения в окне
            _window.OnSubscriptionChanged += (entity, tier) =>
            {
                // Отправляем сообщение на сервер
                // Отправляем сообщение на сервер
                SendMessage(new TraumaChangeSubscriptionMsg(entity, tier));
            };

            _window.OnDispatch += (entity) =>
            {
                SendMessage(new TraumaDispatchMsg(entity));
            };

            _window.OpenCentered();
        }

        // Вызывается, когда сервер присылает новые данные
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            // Если пришел нужный нам пакет данных
            if (state is TraumaComputerState castState)
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
