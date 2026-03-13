namespace mmex.net.core.Entities;

public class CurrencyHistory
{
    public int Id { get; set; }
    public int CurrencyId { get; set; }
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int? UpdateType { get; set; }

    // Navigation
    public Currency? Currency { get; set; }
}
