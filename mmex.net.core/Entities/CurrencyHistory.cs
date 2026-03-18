namespace mmex.net.core.Entities;

public class CurrencyHistory
{
    public long Id { get; set; }
    /// <summary>FK to CURRENCYFORMATS_V1 — seed IDs (1-168), safe as int.</summary>
    public int CurrencyId { get; set; }
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int? UpdateType { get; set; }

    // Navigation
    public Currency? Currency { get; set; }
}
