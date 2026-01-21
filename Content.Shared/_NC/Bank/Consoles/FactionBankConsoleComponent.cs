using Robust.Shared.GameStates;
using Content.Shared._NC.Bank;

namespace Content.Shared._NC.Bank.Consoles
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class FactionBankConsoleComponent : Component
    {
        [DataField("bankAccount")]
        public SectorBankAccount BankAccount = SectorBankAccount.Invalid;
    }
}
