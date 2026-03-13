namespace mmex.net.core.Entities;

/// <summary>Maps SHAREINFO_V1 — share details for investment transactions.</summary>
public class ShareInfo
{
    public int Id { get; set; }
    public int CheckingAccountId { get; set; }
    public decimal? ShareNumber { get; set; }
    public decimal? SharePrice { get; set; }
    public decimal? ShareCommission { get; set; }
    public string? ShareLot { get; set; }

    // Navigation
    public Transaction? Transaction { get; set; }
}
