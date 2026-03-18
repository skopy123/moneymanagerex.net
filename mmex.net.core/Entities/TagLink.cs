namespace mmex.net.core.Entities;

public class TagLink
{
    public long Id { get; set; }
    /// <summary>e.g. "Transaction", "Stock", "Asset".</summary>
    public string RefType { get; set; } = string.Empty;
    public long RefId { get; set; }
    public long TagId { get; set; }

    // Navigation
    public Tag? Tag { get; set; }
}
