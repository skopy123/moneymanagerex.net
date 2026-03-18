namespace mmex.net.core.Entities;

/// <summary>Maps BUDGETSPLITTRANSACTIONS_V1 — splits for scheduled transactions.</summary>
public class BudgetSplitTransaction
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public long? CategoryId { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ScheduledTransaction? ScheduledTransaction { get; set; }
    public Category? Category { get; set; }
}
