using Robust.Shared.GameStates;

namespace Content.Shared._NC.Trauma.Components
{
    // Компонент для планшета Trauma Team, который получает уведомления
    [RegisterComponent, NetworkedComponent]
    public sealed partial class TraumaTabletComponent : Component
    {
        [DataField]
        public NetEntity? ActivePatient;

        /// <summary>
        /// Флаг: оперативник нажал "Выполнено", ожидаем подтверждения диспетчера.
        /// </summary>
        public bool IsPendingCompletion;
    }
}
