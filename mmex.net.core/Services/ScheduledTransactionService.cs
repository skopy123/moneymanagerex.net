using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Extensions;

namespace mmex.net.core.Services;

public class ScheduledTransactionService : IScheduledTransactionService
{
    private readonly MmexDbContext _db;

    public ScheduledTransactionService(MmexDbContext db) => _db = db;

    public Task<IList<ScheduledTransaction>> GetAllAsync() =>
        _db.ScheduledTransactions
            .Include(s => s.Account).Include(s => s.Payee).Include(s => s.Category)
            .OrderBy(s => s.NextOccurrenceDate)
            .ToListAsync().ContinueWith(t => (IList<ScheduledTransaction>)t.Result);

    public async Task<IList<ScheduledTransaction>> GetDueAsync(DateOnly asOfDate)
    {
        var asOfStr = asOfDate.ToString("yyyy-MM-dd");
        return await _db.ScheduledTransactions
            .Include(s => s.Account).Include(s => s.Payee).Include(s => s.Category)
            .Where(s => s.NextOccurrenceDate != null
                        && string.Compare(s.NextOccurrenceDate, asOfStr) <= 0)
            .OrderBy(s => s.NextOccurrenceDate)
            .ToListAsync();
    }

    public Task<ScheduledTransaction?> GetByIdAsync(int id) =>
        _db.ScheduledTransactions
            .Include(s => s.Account).Include(s => s.Payee).Include(s => s.Category)
            .Include(s => s.Splits)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<ScheduledTransaction> CreateAsync(ScheduledTransaction scheduled)
    {
        _db.ScheduledTransactions.Add(scheduled);
        await _db.SaveChangesAsync();
        return scheduled;
    }

    public async Task<ScheduledTransaction> UpdateAsync(ScheduledTransaction scheduled)
    {
        _db.ScheduledTransactions.Update(scheduled);
        await _db.SaveChangesAsync();
        return scheduled;
    }

    public async Task DeleteAsync(int id)
    {
        var sched = await _db.ScheduledTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"ScheduledTransaction {id} not found.");
        _db.ScheduledTransactions.Remove(sched);
        await _db.SaveChangesAsync();
    }

    public async Task<Transaction> ExecuteAsync(int scheduledId)
    {
        var sched = await _db.ScheduledTransactions
            .Include(s => s.Splits)
            .FirstOrDefaultAsync(s => s.Id == scheduledId)
            ?? throw new KeyNotFoundException($"ScheduledTransaction {scheduledId} not found.");

        // Create the real transaction
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var trx = new Transaction
        {
            AccountId = sched.AccountId,
            ToAccountId = sched.ToAccountId,
            PayeeId = sched.PayeeId,
            Type = sched.Type,
            Amount = sched.Amount,
            Status = sched.Status,
            Number = sched.Number,
            Notes = sched.Notes,
            CategoryId = sched.CategoryId,
            Date = sched.NextOccurrenceDate ?? DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            FollowUpId = sched.FollowUpId,
            ToAmount = sched.ToAmount,
            Color = sched.Color,
            LastUpdatedTime = now
        };

        // Copy splits
        foreach (var bs in sched.Splits)
        {
            trx.Splits.Add(new SplitTransaction
            {
                CategoryId = bs.CategoryId,
                Amount = bs.Amount,
                Notes = bs.Notes
            });
        }

        _db.Transactions.Add(trx);

        // Advance schedule
        var currentDate = DateOnly.TryParse(sched.NextOccurrenceDate, out var d) ? d : DateOnly.FromDateTime(DateTime.Today);
        var nextDate = sched.CalculateNextDate(currentDate);
        var freq = sched.GetFrequency();

        if (freq == RepeatFrequency.Once || nextDate == null)
        {
            // One-time: delete the schedule
            _db.ScheduledTransactions.Remove(sched);
        }
        else
        {
            sched.NextOccurrenceDate = nextDate.Value.ToString("yyyy-MM-dd");

            // Decrement fixed-count frequencies
            if (sched.NumOccurrences.HasValue && sched.NumOccurrences > 0
                && freq is not RepeatFrequency.InXDays and not RepeatFrequency.InXMonths
                    and not RepeatFrequency.EveryXDays and not RepeatFrequency.EveryXMonths)
            {
                sched.NumOccurrences--;
                if (sched.NumOccurrences <= 0)
                    _db.ScheduledTransactions.Remove(sched);
            }
        }

        await _db.SaveChangesAsync();
        return trx;
    }
}
