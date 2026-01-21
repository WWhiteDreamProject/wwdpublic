using Content.Shared.Containers.ItemSlots;
using Content.Shared.Tag; 
using Content.Shared.Whitelist; 
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; 
using Robust.Shared.Serialization;
using System.Collections.Generic; 

namespace Content.Shared._NC.Bank.Components 
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class AtmComponent : Component
    {
        [DataField("taxRate")]
        public float TaxRate = 0.1f;

        // === 1. Слот для ID Карты ===
        public const string IdSlotId = "atm_id_slot";

        [DataField("idSlot")]
        public ItemSlot IdSlot = new()
        {
            Name = "ID карта",
            Whitelist = new EntityWhitelist
            {
                Components = new[] { "IdCard" }
            }
        };

        // === 2. Слот для Денег ===
        public const string CashSlotId = "atm_cash_slot";

        [DataField("cashSlot")]
        public ItemSlot CashSlot = new()
        {
            Name = "Приемник купюр",
            // Locked = true, <--- УБРАЛИ (Теперь можно достать деньги руками)
            Whitelist = new EntityWhitelist
            {
                Components = new[] { "Stack" },
                // Исправили тип списка для тэгов:
                Tags = new List<ProtoId<TagPrototype>> { "SpaceCash" }
            }
        };
    }

    [Serializable, NetSerializable]
    public enum AtmUiKey : byte { Key }

    [Serializable, NetSerializable]
    public sealed class AtmBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly int BankBalance;
        public readonly string AccountName;
        public readonly bool IsCardInserted;
        public readonly float TaxRate;
        public readonly int DepositAmount;

        public AtmBoundUserInterfaceState(int bankBalance, string accountName, bool isCardInserted, float taxRate, int depositAmount)
        {
            BankBalance = bankBalance;
            AccountName = accountName;
            IsCardInserted = isCardInserted;
            TaxRate = taxRate;
            DepositAmount = depositAmount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AtmWithdrawMessage : BoundUserInterfaceMessage
    {
        public readonly int Amount;
        public AtmWithdrawMessage(int amount) { Amount = amount; }
    }

    [Serializable, NetSerializable]
    public sealed class AtmDepositMessage : BoundUserInterfaceMessage { }
}