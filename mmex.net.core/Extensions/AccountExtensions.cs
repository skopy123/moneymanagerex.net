using mmex.net.core.Entities;
using mmex.net.core.Enums;

namespace mmex.net.core.Extensions;

public static class AccountExtensions
{
    public static bool IsOpen(this Account account) => account.Status == AccountStatus.Open;
    public static bool IsMoneyAccount(this Account account) =>
        account.Type is AccountType.Cash or AccountType.Checking
            or AccountType.Term or AccountType.CreditCard or AccountType.Loan;

    public static decimal CalculateBalance(this Account account, IEnumerable<Transaction> transactions)
    {
        var balance = account.InitialBalance;
        foreach (var trx in transactions)
        {
            if (trx.AffectsBalance())
                balance += trx.GetFlow(account.Id);
        }
        return balance;
    }
}
