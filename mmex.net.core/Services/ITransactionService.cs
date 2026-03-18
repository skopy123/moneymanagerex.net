using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface ITransactionService
{
    Task<IList<Transaction>> GetByAccountAsync(long accountId, DateOnly? from = null, DateOnly? to = null);
    Task<IList<(Transaction Transaction, decimal RunningBalance)>> GetRunningBalanceAsync(long accountId);
    Task<Transaction?> GetByIdAsync(long id);
    Task<Transaction> CreateAsync(Transaction transaction, IList<SplitTransaction>? splits = null);
    Task<Transaction> UpdateAsync(Transaction transaction, IList<SplitTransaction>? splits = null);
    Task SoftDeleteAsync(long id);
    Task HardDeleteAsync(long id);
}
