using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.UserInterface.Controls;
using Content.Shared._NC.Bank.Components;

namespace Content.Client._NC.Bank.Consoles
{
    public sealed partial class FactionBankConsoleWindow : DefaultWindow
    {
        public event Action<int, string>? OnWithdraw;
        public event Action<int, string>? OnDeposit;

        private readonly Label _lblBalance;
        private readonly Label _lblTitle;
        private readonly LineEdit _leAmount;
        private readonly LineEdit _leDescription;
        private readonly Button _btnWithdraw;
        private readonly Button _btnDeposit;
        private readonly ItemList _logList;

        public FactionBankConsoleWindow()
        {
            RobustXamlLoader.Load(this);

            _lblBalance = FindControl<Label>("LblBalance");
            _lblTitle = FindControl<Label>("LblTitle");
            _leAmount = FindControl<LineEdit>("LeAmount");
            _leDescription = FindControl<LineEdit>("LeDescription");
            _btnWithdraw = FindControl<Button>("BtnWithdraw");
            _btnDeposit = FindControl<Button>("BtnDeposit");
            _logList = FindControl<ItemList>("LogList");

            _btnWithdraw.OnPressed += _ =>
            {
                if (int.TryParse(_leAmount.Text, out var amount))
                    OnWithdraw?.Invoke(amount, _leDescription.Text);
            };

            _btnDeposit.OnPressed += _ =>
            {
                if (int.TryParse(_leAmount.Text, out var amount))
                    OnDeposit?.Invoke(amount, _leDescription.Text);
            };
        }

        public void UpdateState(int balance, string title, List<BankTransaction> logs)
        {
            _lblBalance.Text = $"{balance} $";
            _lblTitle.Text = title;

            _logList.Clear();
            // Show newlogs first
            for (int i = logs.Count - 1; i >= 0; i--)
            {
                var log = logs[i];
                var typeStr = log.Type == BankTransactionType.Deposit ? "+" : "-";
                // Format: [12:00] Player: +100 (Desc)
                var timeStr = log.Time.ToString(@"hh\:mm");
                var text = $"[{timeStr}] {log.Player}: {typeStr}{log.Amount} ({log.Reason})";
                _logList.AddItem(text);
            }
        }
    }
}
