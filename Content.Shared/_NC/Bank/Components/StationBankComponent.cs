using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Bank.Components
{
    /// <summary>
    /// Компонент, который хранит бюджеты департаментов на станции.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StationBankComponent : Component
    {
        [DataField("accounts")]
        public Dictionary<SectorBankAccount, StationBankAccountInfo> Accounts = new();

        /// <summary>
        /// Таймер для начисления пассивного дохода корпорациям.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float SecondsSinceLastIncrease = 0.0f;
    }

    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class StationBankAccountInfo
    {
        [DataField("balance")]
        public int Balance;

        [DataField("increasePerSecond")]
        public int IncreasePerSecond;

        [DataField("logs")]
        public List<BankTransaction> Logs = new();
    }

    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class BankTransaction
    {
        [DataField] public TimeSpan Time;
        [DataField] public string Player = string.Empty;
        [DataField] public BankTransactionType Type;
        [DataField] public int Amount;
        [DataField] public string Reason = string.Empty;

        public BankTransaction(TimeSpan time, string player, BankTransactionType type, int amount, string reason)
        {
            Time = time;
            Player = player;
            Type = type;
            Amount = amount;
            Reason = reason;
        }

        // Default constructor for serialization
        public BankTransaction() { }
    }

    [Serializable, NetSerializable]
    public enum BankTransactionType : byte
    {
        Deposit,
        Withdraw
    }
}
