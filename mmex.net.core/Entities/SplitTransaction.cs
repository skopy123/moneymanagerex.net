namespace mmex.net.core.Entities;

public class SplitTransaction
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Transaction? Transaction { get; set; }
    public Category? Category { get; set; }
}
