namespace mmex.net.core.Entities;

public class TagLink
{
    public int Id { get; set; }
    /// <summary>e.g. "Transaction", "Stock", "Asset".</summary>
    public string RefType { get; set; } = string.Empty;
    public int RefId { get; set; }
    public int TagId { get; set; }

    // Navigation
    public Tag? Tag { get; set; }
}
