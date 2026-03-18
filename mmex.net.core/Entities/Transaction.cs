using mmex.net.core.Enums;

namespace mmex.net.core.Entities;

public class Transaction
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public long? ToAccountId { get; set; }
    /// <summary>C++ stores -1 for "no payee". Treat &lt;= 0 as null.</summary>
    public long PayeeId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Number { get; set; }
    public string? Notes { get; set; }
    public long? CategoryId { get; set; }
    /// <summary>Stored as ISO 8601 TEXT "YYYY-MM-DD" in SQLite.</summary>
    public string? Date { get; set; }
    public string? LastUpdatedTime { get; set; }
    /// <summary>Null = not deleted (soft delete).</summary>
    public string? DeletedTime { get; set; }
    public int? FollowUpId { get; set; }
    public decimal? ToAmount { get; set; }
    /// <summary>wxColour stored as unsigned 32-bit int; must be long to avoid int32 overflow.</summary>
    public long Color { get; set; } = -1;

    // Navigation
    public Account? Account { get; set; }
    public Account? ToAccount { get; set; }
    public Payee? Payee { get; set; }
    public Category? Category { get; set; }
    public ICollection<SplitTransaction> Splits { get; set; } = new List<SplitTransaction>();
    public ICollection<TagLink> TagLinks { get; set; } = new List<TagLink>();
}
