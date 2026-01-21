using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Bank.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class BankAccountComponent : Component
    {
        /// <summary>
        /// Текущий баланс. 
        /// Атрибут AutoNetworkedField сам отправит значение клиенту, когда мы изменим его на сервере.
        /// </summary>
        [DataField, AutoNetworkedField]
        public int Balance = 0;
    }
}