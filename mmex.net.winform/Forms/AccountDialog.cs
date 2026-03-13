using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Services;
using mmex.net.winform.Controls;

namespace mmex.net.winform.Forms;

public class AccountDialog : Form
{
    private readonly ICurrencyService _currencyService;
    private readonly TextBox _txtName;
    private readonly ComboBox _cboType;
    private readonly ComboBox _cboStatus;
    private readonly ComboBox _cboCurrency;
    private readonly CurrencyTextBox _txtInitialBalance;
    private readonly TextBox _txtNotes;
    private readonly CheckBox _chkFavorite;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public Account? Result { get; private set; }

    public AccountDialog(ICurrencyService currencyService, Account? existing = null)
    {
        _currencyService = currencyService;

        Text = existing == null ? "New Account" : "Edit Account";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 380);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtName = new TextBox { Dock = DockStyle.Fill };
        _cboType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboStatus = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _cboCurrency = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _txtInitialBalance = new CurrencyTextBox { Dock = DockStyle.Fill };
        _txtNotes = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
        _chkFavorite = new CheckBox { Text = "Favorite", Dock = DockStyle.Fill };

        foreach (var at in Enum.GetValues<AccountType>())
            _cboType.Items.Add(at);
        foreach (var s in Enum.GetValues<AccountStatus>())
            _cboStatus.Items.Add(s);

        AddRow(layout, "Name:", _txtName, 0);
        AddRow(layout, "Type:", _cboType, 1);
        AddRow(layout, "Status:", _cboStatus, 2);
        AddRow(layout, "Currency:", _cboCurrency, 3);
        AddRow(layout, "Opening Balance:", _txtInitialBalance, 4);
        AddRow(layout, "Notes:", _txtNotes, 5);
        layout.Controls.Add(new Label(), 0, 6);
        layout.Controls.Add(_chkFavorite, 1, 6);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK };
        _btnOk.Click += OnOkClick;
        btnPanel.Controls.AddRange(new Control[] { _btnCancel, _btnOk });
        layout.Controls.Add(btnPanel, 1, 7);

        Controls.Add(layout);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        if (existing != null)
            PopulateFrom(existing);
        else
        {
            _cboType.SelectedIndex = 0;
            _cboStatus.SelectedIndex = 0;
        }

        Load += async (_, _) => await LoadCurrenciesAsync(existing?.CurrencyId);
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control, int row)
    {
        layout.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
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
        _txtNotes.Text = a.Notes;
        _chkFavorite.Checked = a.IsFavorite;
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
            Notes = _txtNotes.Text,
            IsFavorite = _chkFavorite.Checked
        };
    }
}
