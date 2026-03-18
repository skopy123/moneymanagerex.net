using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Services;
using mmex.net.winform.Controls;
using mmex.net.winform.Panels;

namespace mmex.net.winform.Forms;

public class TransactionDialog : Form
{
    private readonly IPayeeService _payeeService;
    private readonly ICategoryService _categoryService;
    private readonly IAccountService _accountService;

    private readonly DateTimePicker _dtpDate;
    private readonly ComboBox _cboType;
    private readonly ComboBox _cboPayee;
    private readonly ComboBox _cboCategory;
    private readonly ComboBox _cboToAccount;
    private readonly CurrencyTextBox _txtAmount;
    private readonly CurrencyTextBox _txtToAmount;
    private readonly ComboBox _cboStatus;
    private readonly TextBox _txtNumber;
    private readonly TextBox _txtNotes;
    private readonly TabControl _tabs;
    private readonly DataGridView _splitsGrid;
    private readonly Button _btnAddSplit;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;
    private readonly AttachmentPanel _attachmentPanel;

    private long _accountId;
    private readonly long? _existingId;
    private readonly Transaction? _existing;
    public Transaction? Result { get; private set; }
    public IList<SplitTransaction>? ResultSplits { get; private set; }

    public TransactionDialog(
        IPayeeService payeeService,
        ICategoryService categoryService,
        IAccountService accountService,
        IAttachmentService attachmentService,
        string attachmentFolder,
        long accountId,
        Transaction? existing = null)
    {
        _payeeService = payeeService;
        _categoryService = categoryService;
        _accountService = accountService;
        _accountId = accountId;
        _existingId = existing?.Id;
        _existing = existing;
        _attachmentPanel = new AttachmentPanel(attachmentService, attachmentFolder) { Dock = DockStyle.Fill };

        Text = existing == null ? "New Transaction" : "Edit Transaction";
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(560, 620);

        // Main fields
        _dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
        _cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboPayee = new ComboBox { Dock = DockStyle.Fill, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        _cboCategory = new ComboBox { Dock = DockStyle.Fill, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        _cboToAccount = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _txtAmount = new CurrencyTextBox { Dock = DockStyle.Fill };
        _txtToAmount = new CurrencyTextBox { Dock = DockStyle.Fill };
        _cboStatus = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _txtNumber = new TextBox { Dock = DockStyle.Fill };
        _txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };

        foreach (var tt in Enum.GetValues<TransactionType>()) _cboType.Items.Add(tt);
        foreach (var ts in Enum.GetValues<TransactionStatus>()) _cboStatus.Items.Add(ts);
        _cboType.SelectedIndex = 0;
        _cboStatus.SelectedIndex = 0;
        _cboType.SelectedIndexChanged += OnTypeChanged;

        // Splits grid
        _splitsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToDeleteRows = true
        };
        _splitsGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "SplitCategory", HeaderText = "Category", FillWeight = 50 });
        _splitsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SplitAmount", HeaderText = "Amount", FillWeight = 30 });
        _splitsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SplitNotes", HeaderText = "Notes", FillWeight = 20 });

        _btnAddSplit = new Button { Text = "Add Split", Dock = DockStyle.Bottom };
        _btnAddSplit.Click += (_, _) => _splitsGrid.Rows.Add();

        var splitsContainer = new Panel { Dock = DockStyle.Fill };
        splitsContainer.Controls.Add(_splitsGrid);
        splitsContainer.Controls.Add(_btnAddSplit);

        _tabs = new TabControl { Dock = DockStyle.Fill };
        var tabMain = new TabPage("Transaction");
        var tabSplits = new TabPage("Splits");
        tabSplits.Controls.Add(splitsContainer);
        _tabs.TabPages.Add(tabMain);
        _tabs.TabPages.Add(tabSplits);

        // Top fields (9 rows, no Notes)
        var fieldsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 8, 8, 0),
            ColumnCount = 2,
            RowCount = 9
        };
        fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        AddRow(fieldsLayout, "Date:", _dtpDate, row++);
        AddRow(fieldsLayout, "Type:", _cboType, row++);
        AddRow(fieldsLayout, "Payee:", _cboPayee, row++);
        AddRow(fieldsLayout, "Category:", _cboCategory, row++);
        AddRow(fieldsLayout, "To Account:", _cboToAccount, row++);
        AddRow(fieldsLayout, "Amount:", _txtAmount, row++);
        AddRow(fieldsLayout, "To Amount:", _txtToAmount, row++);
        AddRow(fieldsLayout, "Status:", _cboStatus, row++);
        AddRow(fieldsLayout, "Number:", _txtNumber, row++);

        // Bottom: Notes (top) above Attachments (bottom)
        var notesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 0) };
        notesPanel.Controls.Add(_txtNotes);
        notesPanel.Controls.Add(new Label { Text = "Notes:", Dock = DockStyle.Top, Height = 16 });

        var attachmentsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 0, 4, 4) };
        attachmentsPanel.Controls.Add(_attachmentPanel);
        attachmentsPanel.Controls.Add(new Label { Text = "Attachments:", Dock = DockStyle.Top, Height = 16 });

        var bottomSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 50,
            Panel2MinSize = 60
        };
        bottomSplit.Panel1.Controls.Add(notesPanel);
        bottomSplit.Panel2.Controls.Add(attachmentsPanel);

        // Main tab: top half = fields, bottom half = notes + attachments
        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 150,
            Panel2MinSize = 120
        };
        mainSplit.Panel1.Controls.Add(fieldsLayout);
        mainSplit.Panel2.Controls.Add(bottomSplit);
        tabMain.Controls.Add(mainSplit);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 35
        };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK };
        _btnOk.Click += OnOkClick;
        btnPanel.Controls.AddRange(new Control[] { _btnCancel, _btnOk });

        Controls.Add(_tabs);
        Controls.Add(btnPanel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        if (existing != null) PopulateFrom(existing);

        Load += async (_, _) =>
        {
            mainSplit.SplitterDistance = mainSplit.Height / 2;
            bottomSplit.SplitterDistance = bottomSplit.Height / 2;
            await LoadDropdownsAsync();
            if (_existingId.HasValue)
                await _attachmentPanel.LoadAsync("Transaction", _existingId.Value);
        };
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control, int row)
    {
        layout.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private async Task LoadDropdownsAsync()
    {
        var payees = await _payeeService.GetAllAsync();
        _cboPayee.DataSource = payees.ToList();
        _cboPayee.DisplayMember = "Name";
        _cboPayee.ValueMember = "Id";

        var categories = await _categoryService.GetAllAsync();
        _cboCategory.DataSource = categories.ToList();
        _cboCategory.DisplayMember = "Name";
        _cboCategory.ValueMember = "Id";

        var accounts = await _accountService.GetAllOpenAsync();
        var others = accounts.Where(a => a.Id != _accountId).ToList();
        _cboToAccount.DataSource = others;
        _cboToAccount.DisplayMember = "Name";
        _cboToAccount.ValueMember = "Id";

        // Populate splits category combo
        if (_splitsGrid.Columns["SplitCategory"] is DataGridViewComboBoxColumn splitCatCol)
        {
            splitCatCol.DataSource = categories.ToList();
            splitCatCol.DisplayMember = "Name";
            splitCatCol.ValueMember = "Id";
        }

        // Restore selections for existing transaction — must happen AFTER DataSource is set.
        // (PopulateFrom runs in the constructor before data is loaded, so it can't set these.)
        if (_existing != null)
        {
            if (_existing.PayeeId > 0)
                _cboPayee.SelectedValue = _existing.PayeeId;
            if (_existing.CategoryId.HasValue)
                _cboCategory.SelectedValue = _existing.CategoryId.Value;
            if (_existing.Type == TransactionType.Transfer && _existing.ToAccountId.HasValue)
                _cboToAccount.SelectedValue = _existing.ToAccountId.Value;
        }

        OnTypeChanged(null, EventArgs.Empty);
    }

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        var isTransfer = (TransactionType?)_cboType.SelectedItem == TransactionType.Transfer;
        _cboToAccount.Enabled = isTransfer;
        _txtToAmount.Enabled = isTransfer;
        _cboPayee.Enabled = !isTransfer;
    }

    private void PopulateFrom(Transaction t)
    {
        if (DateOnly.TryParse(t.Date, out var d))
            _dtpDate.Value = d.ToDateTime(TimeOnly.MinValue);
        _cboType.SelectedItem = t.Type;
        _cboStatus.SelectedItem = t.Status;
        _txtAmount.Value = t.Amount;
        _txtToAmount.Value = t.ToAmount ?? 0;
        _txtNumber.Text = t.Number;
        _txtNotes.Text = t.Notes;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        if (_txtAmount.Value <= 0)
        {
            MessageBox.Show("Amount must be greater than zero.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var type = (TransactionType)(_cboType.SelectedItem ?? TransactionType.Withdrawal);
        var status = (TransactionStatus)(_cboStatus.SelectedItem ?? TransactionStatus.None);

        Result = new Transaction
        {
            AccountId = _accountId,
            Date = DateOnly.FromDateTime(_dtpDate.Value).ToString("yyyy-MM-dd"),
            Type = type,
            Amount = _txtAmount.Value,
            ToAmount = type == TransactionType.Transfer ? _txtToAmount.Value : null,
            ToAccountId = type == TransactionType.Transfer && _cboToAccount.SelectedValue is long taid ? taid : (long?)null,
            PayeeId = type != TransactionType.Transfer && _cboPayee.SelectedValue is long pid ? pid : 0,
            CategoryId = _cboCategory.SelectedValue is long cid ? cid : (long?)null,
            Status = status,
            Number = _txtNumber.Text,
            Notes = _txtNotes.Text
        };

        // Collect splits
        var splits = new List<SplitTransaction>();
        foreach (DataGridViewRow row in _splitsGrid.Rows)
        {
            if (row.IsNewRow) continue;
            if (row.Cells["SplitAmount"].Value is string amtStr
                && decimal.TryParse(amtStr, out var amt))
            {
                splits.Add(new SplitTransaction
                {
                    CategoryId = row.Cells["SplitCategory"].Value is long scid ? scid : (long?)null,
                    Amount = amt,
                    Notes = row.Cells["SplitNotes"].Value?.ToString()
                });
            }
        }

        if (splits.Count > 0)
        {
            Result.CategoryId = null; // Category on parent is null when splits exist
            ResultSplits = splits;
        }
    }

    /// <summary>
    /// Writes buffered attachment changes for the given transaction ID.
    /// Call this after the transaction record has been created/updated in the DB.
    /// </summary>
    public Task CommitAttachmentsAsync(long transactionId) =>
        _attachmentPanel.CommitAsync("Transaction", transactionId);
}
