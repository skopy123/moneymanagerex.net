namespace mmex.net.core.Entities;

public class SplitTransaction
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public long? CategoryId { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Transaction? Transaction { get; set; }
    public Category? Category { get; set; }
}
