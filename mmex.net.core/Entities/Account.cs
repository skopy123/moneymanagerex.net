using mmex.net.core.Enums;

namespace mmex.net.core.Entities;

public class Account
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string? AccountNum { get; set; }
    public AccountStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? HeldAt { get; set; }
    public string? Website { get; set; }
    public string? ContactInfo { get; set; }
    public string? AccessInfo { get; set; }
    public decimal InitialBalance { get; set; }
    public string? InitialDate { get; set; }
    public bool IsFavorite { get; set; }
    /// <summary>FK to CURRENCYFORMATS_V1 — standard seed IDs (1-168), safe as int.</summary>
    public int CurrencyId { get; set; }
    public int? StatementLocked { get; set; }
    public string? StatementDate { get; set; }
    public decimal? MinimumBalance { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? InterestRate { get; set; }
    public string? PaymentDueDate { get; set; }
    public decimal? MinimumPayment { get; set; }

    // Navigation
    public Currency? Currency { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> ToTransactions { get; set; } = new List<Transaction>();
}
