namespace mmex.net.core.Entities;

/// <summary>Maps TRANSLINK_V1 — links checking transactions to assets or stocks.</summary>
public class TransactionLink
{
    public int Id { get; set; }
    public int CheckingAccountId { get; set; }
    /// <summary>"Asset" or "Stock".</summary>
    public string LinkType { get; set; } = string.Empty;
    public int LinkRecordId { get; set; }

    // Navigation
    public Transaction? Transaction { get; set; }
}
