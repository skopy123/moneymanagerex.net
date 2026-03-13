namespace mmex.net.core.Entities;

public class CustomField
{
    public int Id { get; set; }
    /// <summary>"Transaction", "Stock", "Asset", "Bank Account", "Repeating Transaction", "Payee".</summary>
    public string RefType { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>"String", "Integer", "Decimal", "Boolean", "Date", "Time", "SingleChoice", "MultiChoice".</summary>
    public string Type { get; set; } = string.Empty;
    public string Properties { get; set; } = string.Empty;
}
