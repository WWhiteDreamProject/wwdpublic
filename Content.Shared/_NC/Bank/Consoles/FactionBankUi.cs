using Robust.Shared.Serialization;
using Content.Shared._NC.Bank.Components; // Added for BankTransaction

namespace Content.Shared._NC.Bank.Consoles
{
    [Serializable, NetSerializable]
    public enum FactionBankConsoleUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class FactionBankConsoleState : BoundUserInterfaceState
    {
        public int Balance;
        public string Title;
        public List<BankTransaction> Logs;

        public FactionBankConsoleState(int balance, string title, List<BankTransaction> logs)
        {
            Balance = balance;
            Title = title;
            Logs = logs;
        }
    }

    [Serializable, NetSerializable]
    public sealed class FactionBankWithdrawMessage : BoundUserInterfaceMessage
    {
        public int Amount;
        public string Description;

        public FactionBankWithdrawMessage(int amount, string description)
        {
            Amount = amount;
            Description = description;
        }
    }

    [Serializable, NetSerializable]
    public sealed class FactionBankDepositMessage : BoundUserInterfaceMessage
    {
        public int Amount;
        public string Description;

        public FactionBankDepositMessage(int amount, string description)
        {
            Amount = amount;
            Description = description;
        }
    }
}
