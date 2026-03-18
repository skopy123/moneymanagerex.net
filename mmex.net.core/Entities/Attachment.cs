namespace mmex.net.core.Entities;

public class Attachment
{
    public long Id { get; set; }
    /// <summary>"Transaction", "Stock", "Asset", "Bank Account", "Repeating Transaction", "Payee".</summary>
    public string RefType { get; set; } = string.Empty;
    public long RefId { get; set; }
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
}
