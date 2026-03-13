using mmex.net.core.Entities;
using mmex.net.core.Services;

namespace mmex.net.winform.Panels;

/// <summary>Panel showing upcoming scheduled/bill transactions.</summary>
public class ScheduledListPanel : UserControl
{
    private readonly IScheduledTransactionService _scheduledService;
    private readonly DataGridView _grid;

    public event EventHandler<ScheduledTransaction>? ExecuteRequested;

    public ScheduledListPanel(IScheduledTransactionService scheduledService)
    {
        _scheduledService = scheduledService;

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

        var executeBtn = new DataGridViewButtonColumn
        {
            Name = "Execute",
            HeaderText = "",
            Text = "Post",
            UseColumnTextForButtonValue = true,
            FillWeight = 8
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "NextDate", HeaderText = "Next Date", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Account", HeaderText = "Account", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Payee", HeaderText = "Payee", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Frequency", HeaderText = "Frequency", FillWeight = 15 },
            executeBtn
        );

        _grid.CellClick += OnCellClick;
        Controls.Add(_grid);
    }

    public async Task LoadAsync()
    {
        var all = await _scheduledService.GetAllAsync();

        _grid.Rows.Clear();
        foreach (var sched in all)
        {
            var rowIndex = _grid.Rows.Add(
                sched.NextOccurrenceDate ?? string.Empty,
                sched.Account?.Name ?? sched.AccountId.ToString(),
                sched.Payee?.Name ?? string.Empty,
                sched.Amount.ToString("N2"),
                sched.GetFrequency().ToString()
            );
            _grid.Rows[rowIndex].Tag = sched;

            // Highlight overdue
            if (sched.NextOccurrenceDate != null
                && string.Compare(sched.NextOccurrenceDate, DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")) < 0)
            {
                _grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightSalmon;
            }
        }
    }

    private void OnCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == _grid.Columns["Execute"]!.Index
            && _grid.Rows[e.RowIndex].Tag is ScheduledTransaction sched)
        {
            ExecuteRequested?.Invoke(this, sched);
        }
    }
}

// Extension so the panel can call GetFrequency without importing core internals
file static class SchedExtLocal
{
    internal static mmex.net.core.Enums.RepeatFrequency GetFrequency(
        this mmex.net.core.Entities.ScheduledTransaction s) =>
        (mmex.net.core.Enums.RepeatFrequency)(s.Repeats % 100);
}
