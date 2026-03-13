using mmex.net.core.Enums;

namespace mmex.net.core.Entities;

public class Currency
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PrefixSymbol { get; set; }
    public string? SuffixSymbol { get; set; }
    public string? DecimalPoint { get; set; }
    public string? GroupSeparator { get; set; }
    public string? UnitName { get; set; }
    public string? CentName { get; set; }
    /// <summary>Smallest unit: 100 = 2 decimal places, 1 = no decimals.</summary>
    public int? Scale { get; set; }
    /// <summary>Rate to convert to base currency.</summary>
    public decimal? BaseConvRate { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public CurrencyType CurrencyType { get; set; }

    // Navigation
    public ICollection<CurrencyHistory> History { get; set; } = new List<CurrencyHistory>();
}
