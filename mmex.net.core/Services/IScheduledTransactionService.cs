using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IScheduledTransactionService
{
    Task<IList<ScheduledTransaction>> GetAllAsync();
    Task<IList<ScheduledTransaction>> GetDueAsync(DateOnly asOfDate);
    Task<ScheduledTransaction?> GetByIdAsync(long id);
    Task<ScheduledTransaction> CreateAsync(ScheduledTransaction scheduled);
    Task<ScheduledTransaction> UpdateAsync(ScheduledTransaction scheduled);
    Task DeleteAsync(long id);
    Task<Transaction> ExecuteAsync(long scheduledId);
}
