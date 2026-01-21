using Robust.Shared.GameStates;

namespace Content.Shared._NC.Trauma.Components
{
    /// <summary>
    /// Вешается на объект компьютера (консоль). Маркер для системы.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class TraumaComputerComponent : Component
    {
        [DataField]
        public List<TraumaLogEntry> Logs = new();
    }
}
