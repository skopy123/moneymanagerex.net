using System.Diagnostics;
using Microsoft.Extensions.Logging;
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
    private readonly IAttachmentService _attachmentService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<MainForm> _logger;

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
        ICurrencyService currencyService,
        IAttachmentService attachmentService,
        AppSettings appSettings,
        ILogger<MainForm> logger)
    {
        _accountService = accountService;
        _transactionService = transactionService;
        _scheduledService = scheduledService;
        _payeeService = payeeService;
        _categoryService = categoryService;
        _currencyService = currencyService;
        _attachmentService = attachmentService;
        _appSettings = appSettings;
        _logger = logger;

        // Eliminate resize flicker. WinForms paints into an off-screen buffer
        // and blits the result, instead of painting directly to the window DC.
        DoubleBuffered = true;

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
        _accountList.AccountEditRequested += async (_, a) => await OnEditAccountAsync(a);

        _transactionList = new TransactionListPanel(_transactionService, _accountService);
        _transactionList.TransactionAddRequested      += OnNewTransaction;
        _transactionList.TransactionEditRequested     += OnTransactionEditRequested;
        _transactionList.TransactionDeleteConfirmed   += OnTransactionDeleteConfirmed;
        _transactionList.TransactionDuplicateRequested += OnTransactionDuplicateRequested;

        _scheduledList = new ScheduledListPanel(_scheduledService);
        _scheduledList.ExecuteRequested += OnExecuteScheduled;
        _scheduledList.Visible = false;

        // Outer split: nav | content
        // Do NOT set Panel1MinSize, Panel2MinSize, or SplitterDistance here —
        // the container has no width yet and setting them triggers an internal
        // set_SplitterDistance call that throws InvalidOperationException.
        // All of those are applied in the Load event below.
        _split = new SplitContainer { Dock = DockStyle.Fill };
        _accountList.Dock = DockStyle.Fill;
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

        var accountMenu = new ToolStripMenuItem("&Account");
        accountMenu.DropDownItems.Add(new ToolStripMenuItem("&New Account\tCtrl+Shift+N", null,
            async (_, _) => await OnNewAccountAsync()));
        accountMenu.DropDownItems.Add(new ToolStripMenuItem("&Edit Account", null,
            async (_, _) => { if (_selectedAccount != null) await OnEditAccountAsync(_selectedAccount); }));

        var viewMenu = new ToolStripMenuItem("&View");
        var showScheduled = new ToolStripMenuItem("Scheduled Transactions", null, OnToggleScheduled) { CheckOnClick = true };
        viewMenu.DropDownItems.Add(showScheduled);

        var trxMenu = new ToolStripMenuItem("&Transaction");
        trxMenu.DropDownItems.Add(new ToolStripMenuItem("&New Transaction\tCtrl+N", null, OnNewTransaction));

        menu.Items.AddRange(new ToolStripItem[] { fileMenu, accountMenu, viewMenu, trxMenu });
        return menu;
    }

    private async Task LoadAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            SetStatus("Loading accounts...");
            await _accountList.LoadAsync();
            _logger.LogInformation("Account list UI loaded in {Elapsed}ms", sw.ElapsedMilliseconds);

            await _scheduledList.LoadAsync();
            _logger.LogInformation("Scheduled list loaded in {Elapsed}ms", sw.ElapsedMilliseconds);

            SetStatus("Ready");
            _logger.LogInformation("LoadAsync complete in {Elapsed}ms total", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadAsync failed after {Elapsed}ms", sw.ElapsedMilliseconds);
            SetStatus($"Error: {ex.Message}");
            MessageBox.Show($"Failed to open database:\n{ex.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnAccountSelected(object? sender, Account account)
    {
        _selectedAccount = account;
        SetStatus($"Loading {account.Name}...");
        var sw = Stopwatch.StartNew();
        try
        {
            await _transactionList.LoadAccountAsync(account.Id);
            _logger.LogInformation("Transaction list for '{Account}' loaded in {Elapsed}ms", account.Name, sw.ElapsedMilliseconds);

            var balance = await _accountService.GetBalanceAsync(account.Id);
            SetStatus($"{account.Name}  |  Balance: {balance:N2} {account.Currency?.Symbol}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnAccountSelected failed for '{Account}' after {Elapsed}ms", account.Name, sw.ElapsedMilliseconds);
            SetStatus($"Error: {ex.Message}");
        }
    }

    private async void OnTransactionEditRequested(object? sender, Transaction trx)
    {
        if (_selectedAccount == null) return;
        using var dlg = new TransactionDialog(
            _payeeService, _categoryService, _accountService,
            _attachmentService, _appSettings.AttachmentFolder,
            _selectedAccount.Id, trx);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            dlg.Result.Id = trx.Id;
            await _transactionService.UpdateAsync(dlg.Result, dlg.ResultSplits);
            await dlg.CommitAttachmentsAsync(trx.Id);
            await _transactionList.LoadAccountAsync(_selectedAccount.Id);
        }
    }

    private async void OnTransactionDeleteConfirmed(object? sender, Transaction trx)
    {
        try
        {
            await _transactionService.SoftDeleteAsync(trx.Id);
            if (_selectedAccount != null)
            {
                await _transactionList.LoadAccountAsync(_selectedAccount.Id);
                var balance = await _accountService.GetBalanceAsync(_selectedAccount.Id);
                SetStatus($"{_selectedAccount.Name}  |  Balance: {balance:N2} {_selectedAccount.Currency?.Symbol}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete transaction {Id}", trx.Id);
            MessageBox.Show($"Failed to delete transaction:\n{ex.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnTransactionDuplicateRequested(object? sender, Transaction source)
    {
        if (_selectedAccount == null) return;

        // Build a copy with no ID so it is saved as a new record.
        var copy = new Transaction
        {
            AccountId   = source.AccountId,
            Date        = source.Date,
            Type        = source.Type,
            Amount      = source.Amount,
            ToAmount    = source.ToAmount,
            ToAccountId = source.ToAccountId,
            PayeeId     = source.PayeeId,
            CategoryId  = source.CategoryId,
            Status      = mmex.net.core.Enums.TransactionStatus.None,
            Number      = source.Number,
            Notes       = source.Notes,
        };

        using var dlg = new TransactionDialog(
            _payeeService, _categoryService, _accountService,
            _attachmentService, _appSettings.AttachmentFolder,
            _selectedAccount.Id, copy);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            var newTrx = await _transactionService.CreateAsync(dlg.Result, dlg.ResultSplits);
            await dlg.CommitAttachmentsAsync(newTrx.Id);
            await _transactionList.LoadAccountAsync(_selectedAccount.Id);
            var balance = await _accountService.GetBalanceAsync(_selectedAccount.Id);
            SetStatus($"{_selectedAccount.Name}  |  Balance: {balance:N2} {_selectedAccount.Currency?.Symbol}");
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
            _payeeService, _categoryService, _accountService,
            _attachmentService, _appSettings.AttachmentFolder,
            _selectedAccount.Id);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            var newTrx = await _transactionService.CreateAsync(dlg.Result, dlg.ResultSplits);
            await dlg.CommitAttachmentsAsync(newTrx.Id);
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

    private async Task OnNewAccountAsync()
    {
        using var dlg = new AccountDialog(_currencyService, _attachmentService, _appSettings.AttachmentFolder);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            var newAcct = await _accountService.CreateAsync(dlg.Result);
            await dlg.CommitAttachmentsAsync(newAcct.Id);
            await _accountList.LoadAsync();
            SetStatus($"Account '{dlg.Result.Name}' created.");
        }
    }

    private async Task OnEditAccountAsync(Account account)
    {
        using var dlg = new AccountDialog(_currencyService, _attachmentService, _appSettings.AttachmentFolder, account);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            dlg.Result.Id = account.Id;
            await _accountService.UpdateAsync(dlg.Result);
            await dlg.CommitAttachmentsAsync(account.Id);
            await _accountList.LoadAsync();
            // Reload transactions if this was the selected account
            if (_selectedAccount?.Id == account.Id)
            {
                _selectedAccount = dlg.Result;
                var balance = await _accountService.GetBalanceAsync(account.Id);
                SetStatus($"{dlg.Result.Name}  |  Balance: {balance:N2} {dlg.Result.Currency?.Symbol}");
            }
        }
    }

    private void SetStatus(string text) => _statusLabel.Text = text;
}
