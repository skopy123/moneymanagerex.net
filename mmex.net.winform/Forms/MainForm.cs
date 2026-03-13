using mmex.net.core.Entities;
using mmex.net.core.Services;
using mmex.net.winform.Panels;

namespace mmex.net.winform.Forms;

public class MainForm : Form
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly IScheduledTransactionService _scheduledService;
    private readonly IPayeeService _payeeService;
    private readonly ICategoryService _categoryService;
    private readonly ICurrencyService _currencyService;

    private readonly SplitContainer _split;
    private readonly AccountListPanel _accountList;
    private readonly TransactionListPanel _transactionList;
    private readonly ScheduledListPanel _scheduledList;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;

    private Account? _selectedAccount;

    public MainForm(
        IAccountService accountService,
        ITransactionService transactionService,
        IScheduledTransactionService scheduledService,
        IPayeeService payeeService,
        ICategoryService categoryService,
        ICurrencyService currencyService)
    {
        _accountService = accountService;
        _transactionService = transactionService;
        _scheduledService = scheduledService;
        _payeeService = payeeService;
        _categoryService = categoryService;
        _currencyService = currencyService;

        Text = "MoneyManager .NET";
        Size = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;

        // Status bar
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_statusLabel);

        // Menu
        var menu = BuildMenu();

        // Panels
        _accountList = new AccountListPanel(_accountService);
        _accountList.AccountSelected += OnAccountSelected;

        _transactionList = new TransactionListPanel(_transactionService, _accountService);
        _transactionList.TransactionDoubleClicked += OnTransactionDoubleClicked;

        _scheduledList = new ScheduledListPanel(_scheduledService);
        _scheduledList.ExecuteRequested += OnExecuteScheduled;
        _scheduledList.Visible = false;

        // Outer split: nav | content
        // Do NOT set Panel1MinSize, Panel2MinSize, or SplitterDistance here —
        // the container has no width yet and setting them triggers an internal
        // set_SplitterDistance call that throws InvalidOperationException.
        // All of those are applied in the Load event below.
        _split = new SplitContainer { Dock = DockStyle.Fill };
        _split.Panel1.Controls.Add(_accountList);
        _split.Panel2.Controls.Add(_transactionList);
        _split.Panel2.Controls.Add(_scheduledList);

        _transactionList.Dock = DockStyle.Fill;
        _scheduledList.Dock = DockStyle.Fill;

        Controls.Add(_split);
        Controls.Add(_statusStrip);
        Controls.Add(menu);

        Load += async (_, _) =>
        {
            _split.Panel1MinSize = 150;
            _split.Panel2MinSize = 300;
            _split.SplitterDistance = 220;
            await LoadAsync();
        };
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Application.Exit()));

        var viewMenu = new ToolStripMenuItem("&View");
        var showScheduled = new ToolStripMenuItem("Scheduled Transactions", null, OnToggleScheduled) { CheckOnClick = true };
        viewMenu.DropDownItems.Add(showScheduled);

        var trxMenu = new ToolStripMenuItem("&Transaction");
        trxMenu.DropDownItems.Add(new ToolStripMenuItem("&New Transaction\tCtrl+N", null, OnNewTransaction));

        menu.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, trxMenu });
        return menu;
    }

    private async Task LoadAsync()
    {
        try
        {
            SetStatus("Loading accounts...");
            await _accountList.LoadAsync();
            await _scheduledList.LoadAsync();
            SetStatus("Ready");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            MessageBox.Show($"Failed to open database:\n{ex.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAccountSelected(object? sender, Account account)
    {
        _selectedAccount = account;
        SetStatus($"Loading {account.Name}...");
        try
        {
            await _transactionList.LoadAccountAsync(account.Id);
            var balance = await _accountService.GetBalanceAsync(account.Id);
            SetStatus($"{account.Name}  |  Balance: {balance:N2} {account.Currency?.Symbol}");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
    }

    private async void OnTransactionDoubleClicked(object? sender, Transaction trx)
    {
        if (_selectedAccount == null) return;
        using var dlg = new TransactionDialog(
            _payeeService, _categoryService, _accountService, _selectedAccount.Id, trx);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            dlg.Result.Id = trx.Id;
            await _transactionService.UpdateAsync(dlg.Result, dlg.ResultSplits);
            await _transactionList.LoadAccountAsync(_selectedAccount.Id);
        }
    }

    private async void OnNewTransaction(object? sender, EventArgs e)
    {
        if (_selectedAccount == null)
        {
            MessageBox.Show("Please select an account first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new TransactionDialog(
            _payeeService, _categoryService, _accountService, _selectedAccount.Id);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            await _transactionService.CreateAsync(dlg.Result, dlg.ResultSplits);
            await _transactionList.LoadAccountAsync(_selectedAccount.Id);
            var balance = await _accountService.GetBalanceAsync(_selectedAccount.Id);
            SetStatus($"{_selectedAccount.Name}  |  Balance: {balance:N2}");
        }
    }

    private async void OnExecuteScheduled(object? sender, mmex.net.core.Entities.ScheduledTransaction sched)
    {
        var confirm = MessageBox.Show(
            $"Post scheduled transaction '{sched.Payee?.Name}' for {sched.Amount:N2}?",
            "Post Transaction", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            await _scheduledService.ExecuteAsync(sched.Id);
            await _scheduledList.LoadAsync();
            if (_selectedAccount != null)
                await _transactionList.LoadAccountAsync(_selectedAccount.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error posting transaction:\n{ex.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnToggleScheduled(object? sender, EventArgs e)
    {
        var show = (sender as ToolStripMenuItem)?.Checked ?? false;
        _scheduledList.Visible = show;
        _transactionList.Visible = !show;
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
