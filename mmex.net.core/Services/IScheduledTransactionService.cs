using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IScheduledTransactionService
{
    Task<IList<ScheduledTransaction>> GetAllAsync();
    Task<IList<ScheduledTransaction>> GetDueAsync(DateOnly asOfDate);
    Task<ScheduledTransaction?> GetByIdAsync(int id);
    Task<ScheduledTransaction> CreateAsync(ScheduledTransaction scheduled);
    Task<ScheduledTransaction> UpdateAsync(ScheduledTransaction scheduled);
    Task DeleteAsync(int id);
    /// <summary>
    /// Posts the scheduled transaction to CHECKINGACCOUNT_V1 and advances the schedule.
    /// Returns the newly created transaction.
    /// </summary>
    Task<Transaction> ExecuteAsync(int scheduledId);
}
