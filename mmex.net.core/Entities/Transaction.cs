using mmex.net.core.Enums;

namespace mmex.net.core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int? ToAccountId { get; set; }
    /// <summary>C++ stores -1 or 0 for "no payee". Treat &lt;= 0 as null.</summary>
    public int PayeeId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Number { get; set; }
    public string? Notes { get; set; }
    public int? CategoryId { get; set; }
    /// <summary>Stored as ISO 8601 TEXT "YYYY-MM-DD" in SQLite.</summary>
    public string? Date { get; set; }
    public string? LastUpdatedTime { get; set; }
    /// <summary>Null = not deleted (soft delete).</summary>
    public string? DeletedTime { get; set; }
    public int? FollowUpId { get; set; }
    public decimal? ToAmount { get; set; }
    public int Color { get; set; } = -1;

    // Navigation
    public Account? Account { get; set; }
    public Account? ToAccount { get; set; }
    public Payee? Payee { get; set; }
    public Category? Category { get; set; }
    public ICollection<SplitTransaction> Splits { get; set; } = new List<SplitTransaction>();
    public ICollection<TagLink> TagLinks { get; set; } = new List<TagLink>();
}
