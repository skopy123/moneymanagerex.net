using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Services;
using mmex.net.winform.Controls;
using mmex.net.winform.Panels;

namespace mmex.net.winform.Forms;

public class AccountDialog : Form
{
    private readonly ICurrencyService _currencyService;
    private readonly AttachmentPanel _attachmentPanel;

    // Main tab
    private readonly TextBox _txtName;
    private readonly ComboBox _cboType;
    private readonly ComboBox _cboStatus;
    private readonly ComboBox _cboCurrency;
    private readonly CurrencyTextBox _txtInitialBalance;
    private readonly DateTimePicker _dtpOpeningDate;
    private readonly CheckBox _chkFavorite;

    // Notes tab
    private readonly TextBox _txtNotes;

    // Other tab
    private readonly TextBox _txtAccountNum;
    private readonly TextBox _txtHeldAt;
    private readonly TextBox _txtWebsite;
    private readonly TextBox _txtContactInfo;
    private readonly TextBox _txtAccessInfo;

    // Statement tab
    private readonly CheckBox _chkStatementLocked;
    private readonly CheckBox _chkStatementDate;
    private readonly DateTimePicker _dtpStatementDate;
    private readonly CurrencyTextBox _txtMinimumBalance;

    // Credit tab
    private readonly CurrencyTextBox _txtCreditLimit;
    private readonly TextBox _txtInterestRate;
    private readonly CheckBox _chkPaymentDueDate;
    private readonly DateTimePicker _dtpPaymentDueDate;
    private readonly CurrencyTextBox _txtMinimumPayment;

    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    private readonly long? _existingId;
    public Account? Result { get; private set; }

    public AccountDialog(
        ICurrencyService currencyService,
        IAttachmentService attachmentService,
        string attachmentFolder,
        Account? existing = null)
    {
        _currencyService = currencyService;
        _existingId = existing?.Id;
        _attachmentPanel = new AttachmentPanel(attachmentService, attachmentFolder) { Dock = DockStyle.Fill };

        Text = existing == null ? "New Account" : "Edit Account";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(490, 460);

        // Main
        _txtName = new TextBox { Dock = DockStyle.Fill };
        _cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboStatus = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboCurrency = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _txtInitialBalance = new CurrencyTextBox { Dock = DockStyle.Fill };
        _dtpOpeningDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
        _chkFavorite = new CheckBox { Text = "Favorite account", Dock = DockStyle.Fill, AutoSize = false };

        // Notes
        _txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };

        // Other
        _txtAccountNum = new TextBox { Dock = DockStyle.Fill };
        _txtHeldAt = new TextBox { Dock = DockStyle.Fill };
        _txtWebsite = new TextBox { Dock = DockStyle.Fill };
        _txtContactInfo = new TextBox { Dock = DockStyle.Fill };
        _txtAccessInfo = new TextBox { Dock = DockStyle.Fill };

        // Statement
        _chkStatementLocked = new CheckBox { Text = "Lock statement", Dock = DockStyle.Fill, AutoSize = false };
        _chkStatementDate = new CheckBox { Text = "Reconciled date:", Dock = DockStyle.Fill, AutoSize = false };
        _dtpStatementDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Enabled = false };
        _chkStatementDate.CheckedChanged += (_, _) => _dtpStatementDate.Enabled = _chkStatementDate.Checked;
        _txtMinimumBalance = new CurrencyTextBox { Dock = DockStyle.Fill };

        // Credit
        _txtCreditLimit = new CurrencyTextBox { Dock = DockStyle.Fill };
        _txtInterestRate = new TextBox { Dock = DockStyle.Fill };
        _chkPaymentDueDate = new CheckBox { Text = "Payment due date:", Dock = DockStyle.Fill, AutoSize = false };
        _dtpPaymentDueDate = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Enabled = false };
        _chkPaymentDueDate.CheckedChanged += (_, _) => _dtpPaymentDueDate.Enabled = _chkPaymentDueDate.Checked;
        _txtMinimumPayment = new CurrencyTextBox { Dock = DockStyle.Fill };

        foreach (var at in Enum.GetValues<AccountType>()) _cboType.Items.Add(at);
        foreach (var s in Enum.GetValues<AccountStatus>()) _cboStatus.Items.Add(s);

        // Type cannot be changed for existing accounts (matches C++ behavior)
        if (existing != null) _cboType.Enabled = false;

        var tabAttachments = new TabPage("Attachments");
        tabAttachments.Controls.Add(_attachmentPanel);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildMainTab());
        tabs.TabPages.Add(BuildNotesTab());
        tabs.TabPages.Add(BuildOtherTab());
        tabs.TabPages.Add(BuildStatementTab());
        tabs.TabPages.Add(BuildCreditTab());
        tabs.TabPages.Add(tabAttachments);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 38,
            Padding = new Padding(4)
        };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        _btnOk.Click += OnOkClick;
        btnPanel.Controls.AddRange(new Control[] { _btnCancel, _btnOk });

        Controls.Add(tabs);
        Controls.Add(btnPanel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        if (existing != null)
            PopulateFrom(existing);
        else
        {
            _cboType.SelectedIndex = 0;
            _cboStatus.SelectedIndex = 0;
        }

        Load += async (_, _) =>
        {
            await LoadCurrenciesAsync(existing?.CurrencyId);
            if (_existingId.HasValue)
                await _attachmentPanel.LoadAsync("BankAccount", _existingId.Value);
        };
    }

    private TabPage BuildMainTab()
    {
        var page = new TabPage("Main");
        var layout = MakeLayout(7);
        int r = 0;
        AddRow(layout, "Name:", _txtName, r++);
        AddRow(layout, "Type:", _cboType, r++);
        AddRow(layout, "Status:", _cboStatus, r++);
        AddRow(layout, "Currency:", _cboCurrency, r++);
        AddRow(layout, "Opening balance:", _txtInitialBalance, r++);
        AddRow(layout, "Opening date:", _dtpOpeningDate, r++);
        layout.Controls.Add(new Label(), 0, r);
        layout.Controls.Add(_chkFavorite, 1, r);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildNotesTab()
    {
        var page = new TabPage("Notes");
        page.Controls.Add(_txtNotes);
        return page;
    }

    private TabPage BuildOtherTab()
    {
        var page = new TabPage("Other");
        var layout = MakeLayout(5);
        int r = 0;
        AddRow(layout, "Account number:", _txtAccountNum, r++);
        AddRow(layout, "Held at:", _txtHeldAt, r++);
        AddRow(layout, "Website:", _txtWebsite, r++);
        AddRow(layout, "Contact:", _txtContactInfo, r++);
        AddRow(layout, "Access info:", _txtAccessInfo, r++);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildStatementTab()
    {
        var page = new TabPage("Statement");
        var layout = MakeLayout(4);
        int r = 0;
        layout.Controls.Add(new Label(), 0, r);
        layout.Controls.Add(_chkStatementLocked, 1, r++);
        layout.Controls.Add(_chkStatementDate, 0, r);
        layout.Controls.Add(_dtpStatementDate, 1, r++);
        AddRow(layout, "Minimum balance:", _txtMinimumBalance, r++);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildCreditTab()
    {
        var page = new TabPage("Credit");
        var layout = MakeLayout(4);
        int r = 0;
        AddRow(layout, "Credit limit:", _txtCreditLimit, r++);
        AddRow(layout, "Interest rate (%):", _txtInterestRate, r++);
        layout.Controls.Add(_chkPaymentDueDate, 0, r);
        layout.Controls.Add(_dtpPaymentDueDate, 1, r++);
        AddRow(layout, "Minimum payment:", _txtMinimumPayment, r++);
        page.Controls.Add(layout);
        return page;
    }

    private static TableLayoutPanel MakeLayout(int rows)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            ColumnCount = 2,
            RowCount = rows
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control, int row)
    {
        layout.Controls.Add(
            new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private async Task LoadCurrenciesAsync(int? selectedId)
    {
        var currencies = await _currencyService.GetAllAsync();
        _cboCurrency.DisplayMember = "Symbol";
        _cboCurrency.ValueMember = "Id";
        _cboCurrency.DataSource = currencies.ToList();
        if (selectedId.HasValue)
            _cboCurrency.SelectedValue = selectedId.Value;
    }

    private void PopulateFrom(Account a)
    {
        _txtName.Text = a.Name;
        _cboType.SelectedItem = a.Type;
        _cboStatus.SelectedItem = a.Status;
        _txtInitialBalance.Value = a.InitialBalance;
        _chkFavorite.Checked = a.IsFavorite;

        if (DateOnly.TryParse(a.InitialDate, out var openDate))
            _dtpOpeningDate.Value = openDate.ToDateTime(TimeOnly.MinValue);

        _txtNotes.Text = a.Notes ?? string.Empty;

        _txtAccountNum.Text = a.AccountNum ?? string.Empty;
        _txtHeldAt.Text = a.HeldAt ?? string.Empty;
        _txtWebsite.Text = a.Website ?? string.Empty;
        _txtContactInfo.Text = a.ContactInfo ?? string.Empty;
        _txtAccessInfo.Text = a.AccessInfo ?? string.Empty;

        _chkStatementLocked.Checked = a.StatementLocked == 1;
        if (DateOnly.TryParse(a.StatementDate, out var stmtDate))
        {
            _chkStatementDate.Checked = true;
            _dtpStatementDate.Value = stmtDate.ToDateTime(TimeOnly.MinValue);
        }
        _txtMinimumBalance.Value = a.MinimumBalance ?? 0m;

        _txtCreditLimit.Value = a.CreditLimit ?? 0m;
        _txtInterestRate.Text = a.InterestRate?.ToString("G") ?? string.Empty;
        if (DateOnly.TryParse(a.PaymentDueDate, out var dueDate))
        {
            _chkPaymentDueDate.Checked = true;
            _dtpPaymentDueDate.Value = dueDate.ToDateTime(TimeOnly.MinValue);
        }
        _txtMinimumPayment.Value = a.MinimumPayment ?? 0m;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Name is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Result = new Account
        {
            Name = _txtName.Text.Trim(),
            Type = (AccountType)(_cboType.SelectedItem ?? AccountType.Checking),
            Status = (AccountStatus)(_cboStatus.SelectedItem ?? AccountStatus.Open),
            CurrencyId = (int?)_cboCurrency.SelectedValue ?? 1,
            InitialBalance = _txtInitialBalance.Value,
            InitialDate = DateOnly.FromDateTime(_dtpOpeningDate.Value).ToString("yyyy-MM-dd"),
            IsFavorite = _chkFavorite.Checked,
            Notes = NullIfEmpty(_txtNotes.Text),
            AccountNum = NullIfEmpty(_txtAccountNum.Text),
            HeldAt = NullIfEmpty(_txtHeldAt.Text),
            Website = NullIfEmpty(_txtWebsite.Text),
            ContactInfo = NullIfEmpty(_txtContactInfo.Text),
            AccessInfo = NullIfEmpty(_txtAccessInfo.Text),
            StatementLocked = _chkStatementLocked.Checked ? 1 : 0,
            StatementDate = _chkStatementDate.Checked
                ? DateOnly.FromDateTime(_dtpStatementDate.Value).ToString("yyyy-MM-dd")
                : null,
            MinimumBalance = _txtMinimumBalance.Value != 0m ? _txtMinimumBalance.Value : null,
            CreditLimit = _txtCreditLimit.Value != 0m ? _txtCreditLimit.Value : null,
            InterestRate = decimal.TryParse(_txtInterestRate.Text, out var rate) ? rate : null,
            PaymentDueDate = _chkPaymentDueDate.Checked
                ? DateOnly.FromDateTime(_dtpPaymentDueDate.Value).ToString("yyyy-MM-dd")
                : null,
            MinimumPayment = _txtMinimumPayment.Value != 0m ? _txtMinimumPayment.Value : null,
        };
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Writes buffered attachment changes for the given account ID.
    /// Call this after the account record has been created/updated in the DB.
    /// </summary>
    public Task CommitAttachmentsAsync(long accountId) =>
        _attachmentPanel.CommitAsync("BankAccount", accountId);
}
