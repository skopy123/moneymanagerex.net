namespace mmex.net.core.Entities;

/// <summary>Maps BUDGETSPLITTRANSACTIONS_V1 — splits for scheduled transactions.</summary>
public class BudgetSplitTransaction
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ScheduledTransaction? ScheduledTransaction { get; set; }
    public Category? Category { get; set; }
}
