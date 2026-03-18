namespace mmex.net.core.Entities;

public class Payee
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public string? Number { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    /// <summary>0 = inactive, 1 = active.</summary>
    public int Active { get; set; } = 1;
    public string Pattern { get; set; } = string.Empty;

    // Navigation
    public Category? Category { get; set; }
}
