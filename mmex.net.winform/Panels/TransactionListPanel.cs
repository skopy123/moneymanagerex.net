using mmex.net.core.Entities;
using mmex.net.core.Services;

namespace mmex.net.winform.Panels;

/// <summary>Right content panel — DataGridView of transactions for the selected account.</summary>
public class TransactionListPanel : UserControl
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly DataGridView _grid;
    private int _currentAccountId;

    public event EventHandler<Transaction>? TransactionDoubleClicked;

    public TransactionListPanel(ITransactionService transactionService, IAccountService accountService)
    {
        _transactionService = transactionService;
        _accountService = accountService;

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window
        };
        _grid.CellDoubleClick += OnCellDoubleClick;

        SetupColumns();
        Controls.Add(_grid);
    }

    private void SetupColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Payee", HeaderText = "Payee", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    public async Task LoadAccountAsync(int accountId)
    {
        _currentAccountId = accountId;

        var rows = await _transactionService.GetRunningBalanceAsync(accountId);

        _grid.Rows.Clear();

        // Show in reverse chronological order (newest first)
        foreach (var (trx, balance) in rows.Reverse())
        {
            var categoryName = trx.Category?.Name ?? (trx.Splits.Count > 0 ? "[Split]" : "");
            var payeeName = trx.Payee?.Name ?? "";

            // Determine display amount (negative for withdrawals)
            var amount = trx.Type == mmex.net.core.Enums.TransactionType.Withdrawal
                ? -trx.Amount
                : trx.Amount;

            var rowIndex = _grid.Rows.Add(
                trx.Date ?? string.Empty,
                payeeName,
                categoryName,
                trx.Notes ?? string.Empty,
                amount.ToString("N2"),
                trx.Status.ToString(),
                balance.ToString("N2")
            );

            _grid.Rows[rowIndex].Tag = trx;

            // Color-code voids
            if (trx.Status == mmex.net.core.Enums.TransactionStatus.Void)
                _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
        }
    }

    private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].Tag is Transaction trx)
            TransactionDoubleClicked?.Invoke(this, trx);
    }
}
