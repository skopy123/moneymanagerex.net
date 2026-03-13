using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface ITransactionService
{
    Task<IList<Transaction>> GetByAccountAsync(int accountId, DateOnly? from = null, DateOnly? to = null);
    Task<IList<(Transaction Transaction, decimal RunningBalance)>> GetRunningBalanceAsync(int accountId);
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> CreateAsync(Transaction transaction, IList<SplitTransaction>? splits = null);
    Task<Transaction> UpdateAsync(Transaction transaction, IList<SplitTransaction>? splits = null);
    Task SoftDeleteAsync(int id);
    Task HardDeleteAsync(int id);
}
