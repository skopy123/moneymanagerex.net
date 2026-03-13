namespace mmex.net.core.Enums;

public enum TransactionStatus
{
    /// <summary>Stored as empty string "" in DB.</summary>
    None,
    /// <summary>Stored as "R".</summary>
    Reconciled,
    /// <summary>Stored as "V".</summary>
    Void,
    /// <summary>Stored as "F".</summary>
    FollowUp,
    /// <summary>Stored as "D".</summary>
    Duplicate
}
