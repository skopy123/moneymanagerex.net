using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Services;
using mmex.net.winform.Controls;
using OokiiDlg = Ookii.Dialogs.WinForms;
using System.Collections.Generic;

namespace mmex.net.winform.Panels;

/// <summary>Right content panel — toolbar + filters + DataGridView of transactions for the selected account.</summary>
public class TransactionListPanel : UserControl
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly IAttachmentService _attachmentService;

    // ── Toolbar ──────────────────────────────────────────────────────────────
    private readonly ToolStrip _toolbar;
    private readonly ToolStripButton _btnNew;
    private readonly ToolStripButton _btnEdit;
    private readonly ToolStripButton _btnDelete;
    private readonly ToolStripButton _btnDuplicate;

    // ── Filter bar ───────────────────────────────────────────────────────────
    private readonly ComboBox _cboQuickDate;
    private readonly DateTimePicker _dtpFrom;
    private readonly DateTimePicker _dtpTo;
    private readonly ComboBox _cboFilterPayee;
    private readonly ComboBox _cboFilterStatus;

    // ── Grid ─────────────────────────────────────────────────────────────────
    private readonly DataGridView _grid;

    // ── State ────────────────────────────────────────────────────────────────
    private long _currentAccountId;
    private IList<(Transaction Trx, decimal Balance)> _allRows = [];
    private Dictionary<long, string> _accountNames = [];
    private Dictionary<long, int> _attachmentCounts = [];
    private bool _suppressFilter;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Raised when the New button is clicked.</summary>
    public event EventHandler? TransactionAddRequested;
    /// <summary>Raised when Edit button is clicked or a row is double-clicked.</summary>
    public event EventHandler<Transaction>? TransactionEditRequested;
    /// <summary>Raised after the user confirms deletion in the task dialog.</summary>
    public event EventHandler<Transaction>? TransactionDeleteConfirmed;
    /// <summary>Raised when the Duplicate button is clicked.</summary>
    public event EventHandler<Transaction>? TransactionDuplicateRequested;

    public Transaction? SelectedTransaction =>
        _grid.CurrentRow?.Tag as Transaction;

    public TransactionListPanel(ITransactionService transactionService, IAccountService accountService, IAttachmentService attachmentService)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        _attachmentService = attachmentService;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        // ── Toolbar ──────────────────────────────────────────────────────────
        _btnNew = new ToolStripButton("New") { ToolTipText = "New transaction (Ctrl+N)" };
        _btnNew.Click += (_, _) => TransactionAddRequested?.Invoke(this, EventArgs.Empty);

        _btnEdit = new ToolStripButton("Edit") { ToolTipText = "Edit selected transaction" };
        _btnEdit.Click += (_, _) => RequestEdit();

        _btnDelete = new ToolStripButton("Delete") { ToolTipText = "Delete selected transaction" };
        _btnDelete.Click += (_, _) => RequestDelete();

        _btnDuplicate = new ToolStripButton("Duplicate") { ToolTipText = "Duplicate selected transaction" };
        _btnDuplicate.Click += (_, _) => RequestDuplicate();

        _toolbar = new ToolStrip { Dock = DockStyle.Top };
        _toolbar.Items.AddRange(new ToolStripItem[]
        {
            _btnNew, _btnEdit, _btnDelete, new ToolStripSeparator(), _btnDuplicate
        });

        // ── Filter bar ───────────────────────────────────────────────────────
        _cboQuickDate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        _cboQuickDate.Items.AddRange(new object[]
        {
            "All", "This Month", "Last Month",
            "Last 3 Months", "Last 6 Months",
            "This Year", "Last Year"
        });
        _cboQuickDate.SelectedIndex = 0;
        _cboQuickDate.SelectedIndexChanged += OnQuickDateChanged;

        _dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100, Enabled = false };
        _dtpTo   = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100, Value = DateTime.Today, Enabled = false };
        _dtpFrom.ValueChanged += (_, _) => { if (!_suppressFilter) ApplyFilter(); };
        _dtpTo.ValueChanged   += (_, _) => { if (!_suppressFilter) ApplyFilter(); };

        _cboFilterPayee = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140
        };
        _cboFilterPayee.SelectedIndexChanged += (_, _) => { if (!_suppressFilter) ApplyFilter(); };

        _cboFilterStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        _cboFilterStatus.SelectedIndexChanged += (_, _) => { if (!_suppressFilter) ApplyFilter(); };
        _suppressFilter = true;
        InitStatusFilter();
        _suppressFilter = false;

        var btnClear = new Button { Text = "Clear", AutoSize = true, Height = 23 };
        btnClear.Click += OnClearFilter;

        var filterBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(3, 3, 3, 0),
            WrapContents = false,
            AutoScroll = false
        };
        filterBar.Controls.AddRange(new Control[]
        {
            MakeLabel("Period:"), _cboQuickDate,
            MakeLabel("From:"), _dtpFrom,
            MakeLabel("To:"), _dtpTo,
            MakeLabel("Payee:"), _cboFilterPayee,
            MakeLabel("Status:"), _cboFilterStatus,
            btnClear
        });

        // ── Grid ─────────────────────────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
            MultiSelect = false
        };
        _grid.CellDoubleClick += OnCellDoubleClick;
        _grid.SelectionChanged += (_, _) => UpdateButtonStates();
        _grid.EnableDoubleBuffering();

        SetupColumns();

        // Add in reverse visual order: grid first (Fill), filter bar, toolbar last (top-most).
        Controls.Add(_grid);
        Controls.Add(filterBar);
        Controls.Add(_toolbar);

        UpdateButtonStates();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public async Task LoadAccountAsync(long accountId)
    {
        _currentAccountId = accountId;

        var allAccounts = await _accountService.GetAllAsync();
        _accountNames = allAccounts.ToDictionary(a => a.Id, a => a.Name);

        _allRows = await _transactionService.GetRunningBalanceAsync(accountId);

        var ids = _allRows.Select(r => r.Trx.Id);
        _attachmentCounts = await _attachmentService.GetCountsByRefAsync("Transaction", ids);

        RefreshPayeeFilter();
        ApplyFilter();
    }

    // ── Setup helpers ────────────────────────────────────────────────────────

    private void SetupColumns()
    {
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Date",    HeaderText = "Date",              FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Payee",   HeaderText = "Payee / Account",   FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Category",HeaderText = "Category",          FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Notes",   HeaderText = "Notes",             FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "Amount",  HeaderText = "Amount",            FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
            new DataGridViewTextBoxColumn { Name = "Status",  HeaderText = "Status",            FillWeight = 8  },
            new DataGridViewTextBoxColumn { Name = "Att",     HeaderText = "📎",               FillWeight = 5,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance",           FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }
        );
    }

    private void InitStatusFilter()
    {
        _cboFilterStatus.Items.Add("All Statuses");
        foreach (var s in Enum.GetValues<TransactionStatus>())
            _cboFilterStatus.Items.Add(s);
        _cboFilterStatus.SelectedIndex = 0;
    }

    private void RefreshPayeeFilter()
    {
        _suppressFilter = true;

        // Remember selection
        var previous = _cboFilterPayee.SelectedIndex > 0 ? _cboFilterPayee.SelectedItem as string : null;

        _cboFilterPayee.Items.Clear();
        _cboFilterPayee.Items.Add("(All Payees)");

        var names = _allRows
            .Select(r => r.Trx.Payee?.Name ?? "")
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n);

        foreach (var n in names)
            _cboFilterPayee.Items.Add(n);

        var idx = previous != null ? _cboFilterPayee.FindStringExact(previous) : -1;
        _cboFilterPayee.SelectedIndex = idx >= 0 ? idx : 0;

        _suppressFilter = false;
    }

    private static Label MakeLabel(string text) =>
        new Label { Text = text, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };

    // ── Filter logic ─────────────────────────────────────────────────────────

    private void OnQuickDateChanged(object? sender, EventArgs e)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        _suppressFilter = true;
        switch (_cboQuickDate.SelectedIndex)
        {
            case 0: // All — disable date pickers
                _dtpFrom.Enabled = false;
                _dtpTo.Enabled   = false;
                break;

            case 1: // This Month
                _dtpFrom.Value = new DateOnly(today.Year, today.Month, 1).ToDateTime(TimeOnly.MinValue);
                _dtpTo.Value   = today.ToDateTime(TimeOnly.MinValue);
                _dtpFrom.Enabled = _dtpTo.Enabled = true;
                break;

            case 2: // Last Month
                var lm = today.AddMonths(-1);
                _dtpFrom.Value = new DateOnly(lm.Year, lm.Month, 1).ToDateTime(TimeOnly.MinValue);
                _dtpTo.Value   = new DateOnly(lm.Year, lm.Month,
                    DateTime.DaysInMonth(lm.Year, lm.Month)).ToDateTime(TimeOnly.MinValue);
                _dtpFrom.Enabled = _dtpTo.Enabled = true;
                break;

            case 3: // Last 3 Months
                _dtpFrom.Value = today.AddMonths(-3).ToDateTime(TimeOnly.MinValue);
                _dtpTo.Value   = today.ToDateTime(TimeOnly.MinValue);
                _dtpFrom.Enabled = _dtpTo.Enabled = true;
                break;

            case 4: // Last 6 Months
                _dtpFrom.Value = today.AddMonths(-6).ToDateTime(TimeOnly.MinValue);
                _dtpTo.Value   = today.ToDateTime(TimeOnly.MinValue);
                _dtpFrom.Enabled = _dtpTo.Enabled = true;
                break;

            case 5: // This Year
                _dtpFrom.Value = new DateOnly(today.Year, 1, 1).ToDateTime(TimeOnly.MinValue);
                _dtpTo.Value   = today.ToDateTime(TimeOnly.MinValue);
                _dtpFrom.Enabled = _dtpTo.Enabled = true;
                break;

            case 6: // Last Year
                _dtpFrom.Value = new DateOnly(today.Year - 1, 1,  1).ToDateTime(TimeOnly.MinValue);
                _dtpTo.Value   = new DateOnly(today.Year - 1, 12, 31).ToDateTime(TimeOnly.MinValue);
                _dtpFrom.Enabled = _dtpTo.Enabled = true;
                break;
        }

        _suppressFilter = false;
        ApplyFilter();
    }

    private void OnClearFilter(object? sender, EventArgs e)
    {
        _suppressFilter = true;
        _cboQuickDate.SelectedIndex    = 0;
        _cboFilterPayee.SelectedIndex  = 0;
        _cboFilterStatus.SelectedIndex = 0;
        _dtpFrom.Enabled = _dtpTo.Enabled = false;
        _suppressFilter = false;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var useDateFilter = _cboQuickDate.SelectedIndex > 0;
        DateOnly? from = useDateFilter ? DateOnly.FromDateTime(_dtpFrom.Value) : null;
        DateOnly? to   = useDateFilter ? DateOnly.FromDateTime(_dtpTo.Value)   : null;

        var payeeFilter  = _cboFilterPayee.SelectedIndex  > 0 ? _cboFilterPayee.SelectedItem  as string              : null;
        var statusFilter = _cboFilterStatus.SelectedIndex > 0 && _cboFilterStatus.SelectedItem is TransactionStatus ts
                           ? ts : (TransactionStatus?)null;

        _grid.SuspendLayout();
        _grid.Rows.Clear();

        // _allRows is ordered oldest→newest; reverse for display (newest first).
        foreach (var (trx, balance) in _allRows.Reverse())
        {
            if (from.HasValue && DateOnly.TryParse(trx.Date, out var d))
                if (d < from.Value || d > to!.Value) continue;

            if (payeeFilter != null)
            {
                var name = trx.Payee?.Name ?? "";
                if (!name.Equals(payeeFilter, StringComparison.OrdinalIgnoreCase)) continue;
            }

            if (statusFilter.HasValue && trx.Status != statusFilter.Value) continue;

            AddGridRow(trx, balance);
        }

        _grid.ResumeLayout();
        UpdateButtonStates();
    }

    private void AddGridRow(Transaction trx, decimal balance)
    {
        string payeeName;
        if (trx.Type == TransactionType.Transfer)
        {
            if (trx.AccountId == _currentAccountId)
            {
                var dest = trx.ToAccountId.HasValue
                    ? _accountNames.GetValueOrDefault(trx.ToAccountId.Value, "?") : "?";
                payeeName = $"> {dest}";
            }
            else
            {
                payeeName = $"< {_accountNames.GetValueOrDefault(trx.AccountId, "?")}";
            }
        }
        else
        {
            payeeName = trx.Payee?.Name ?? "";
        }

        decimal amount;
        if (trx.Type == TransactionType.Transfer)
            amount = trx.AccountId == _currentAccountId ? -trx.Amount : (trx.ToAmount ?? trx.Amount);
        else
            amount = trx.Type == TransactionType.Withdrawal ? -trx.Amount : trx.Amount;

        var categoryName = trx.Category?.Name ?? (trx.Splits.Count > 0 ? "[Split]" : "");

        var attCount = _attachmentCounts.GetValueOrDefault(trx.Id, 0);
        var idx = _grid.Rows.Add(
            trx.Date ?? "",
            payeeName,
            categoryName,
            trx.Notes ?? "",
            amount.ToString("N2"),
            trx.Status.ToString(),
            attCount > 0 ? attCount.ToString() : "",
            balance.ToString("N2")
        );

        _grid.Rows[idx].Tag = trx;

        if (trx.Status == TransactionStatus.Void)
            _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Gray;
    }

    // ── Button state ─────────────────────────────────────────────────────────

    private void UpdateButtonStates()
    {
        var hasSel = SelectedTransaction != null;
        _btnEdit.Enabled      = hasSel;
        _btnDelete.Enabled    = hasSel;
        _btnDuplicate.Enabled = hasSel;
    }

    // ── Action handlers ───────────────────────────────────────────────────────

    private void RequestEdit()
    {
        if (SelectedTransaction is { } trx)
            TransactionEditRequested?.Invoke(this, trx);
    }

    private void RequestDelete()
    {
        if (SelectedTransaction is not { } trx) return;

        using var dlg = new OokiiDlg.TaskDialog();
        dlg.WindowTitle = "Confirm Delete";
        dlg.MainInstruction = "Delete this transaction?";
        dlg.Content = $"Date: {trx.Date}   Amount: {trx.Amount:N2}   Payee: {trx.Payee?.Name ?? ""}";
        dlg.MainIcon = OokiiDlg.TaskDialogIcon.Warning;
        dlg.ButtonStyle = OokiiDlg.TaskDialogButtonStyle.Standard;

        var yesBtn = new OokiiDlg.TaskDialogButton(OokiiDlg.ButtonType.Yes);
        var noBtn  = new OokiiDlg.TaskDialogButton(OokiiDlg.ButtonType.No);
        noBtn.Default = true;
        dlg.Buttons.Add(yesBtn);
        dlg.Buttons.Add(noBtn);

        if (dlg.ShowDialog(FindForm()) == yesBtn)
            TransactionDeleteConfirmed?.Invoke(this, trx);
    }

    private void RequestDuplicate()
    {
        if (SelectedTransaction is { } trx)
            TransactionDuplicateRequested?.Invoke(this, trx);
    }

    private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].Tag is Transaction trx)
            TransactionEditRequested?.Invoke(this, trx);
    }
}
