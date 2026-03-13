using mmex.net.core.Entities;
using mmex.net.core.Enums;

namespace mmex.net.core.Extensions;

public static class TransactionExtensions
{
    /// <summary>
    /// Returns the signed flow of a transaction for the given account.
    /// Positive = money coming in, negative = money going out.
    /// Void and deleted transactions must be excluded before calling this.
    /// </summary>
    public static decimal GetFlow(this Transaction trx, int accountId)
    {
        return trx.Type switch
        {
            TransactionType.Withdrawal => -trx.Amount,
            TransactionType.Deposit => trx.Amount,
            TransactionType.Transfer when trx.AccountId == accountId => -trx.Amount,
            TransactionType.Transfer when trx.ToAccountId == accountId => trx.ToAmount ?? trx.Amount,
            _ => 0m
        };
    }

    public static bool IsDeleted(this Transaction trx) => trx.DeletedTime != null;
    public static bool IsVoid(this Transaction trx) => trx.Status == TransactionStatus.Void;
    public static bool AffectsBalance(this Transaction trx) => !trx.IsDeleted() && !trx.IsVoid();

    /// <summary>Returns the effective payee ID, or null if none (C++ stores -1 or 0).</summary>
    public static int? GetPayeeId(this Transaction trx) =>
        trx.PayeeId <= 0 ? null : trx.PayeeId;
}
