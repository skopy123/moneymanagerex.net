using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Extensions;

namespace mmex.net.core.Services;

public class TransactionService : ITransactionService
{
    private readonly MmexDbContext _db;

    public TransactionService(MmexDbContext db) => _db = db;

    public async Task<IList<Transaction>> GetByAccountAsync(int accountId, DateOnly? from = null, DateOnly? to = null)
    {
        var q = _db.Transactions
            .Include(t => t.Payee)
            .Include(t => t.Category)
            .Include(t => t.Splits)
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId);

        if (from.HasValue)
            q = q.Where(t => string.Compare(t.Date, from.Value.ToString("yyyy-MM-dd")) >= 0);
        if (to.HasValue)
            q = q.Where(t => string.Compare(t.Date, to.Value.ToString("yyyy-MM-dd")) <= 0);

        return await q.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync();
    }

    public async Task<IList<(Transaction Transaction, decimal RunningBalance)>> GetRunningBalanceAsync(int accountId)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        var transactions = await _db.Transactions
            .Include(t => t.Payee)
            .Include(t => t.Category)
            .Where(t => (t.AccountId == accountId || t.ToAccountId == accountId)
                        && t.Status != TransactionStatus.Void)
            .OrderBy(t => t.Date).ThenBy(t => t.Id)
            .ToListAsync();

        var result = new List<(Transaction, decimal)>(transactions.Count);
        var running = account.InitialBalance;
        foreach (var trx in transactions)
        {
            running += trx.GetFlow(accountId);
            result.Add((trx, running));
        }
        return result;
    }

    public Task<Transaction?> GetByIdAsync(int id) =>
        _db.Transactions
            .Include(t => t.Payee)
            .Include(t => t.Category)
            .Include(t => t.Splits)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Transaction> CreateAsync(Transaction transaction, IList<SplitTransaction>? splits = null)
    {
        transaction.LastUpdatedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        _db.Transactions.Add(transaction);

        if (splits != null)
        {
            foreach (var split in splits)
                transaction.Splits.Add(split);
        }

        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction, IList<SplitTransaction>? splits = null)
    {
        transaction.LastUpdatedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        _db.Transactions.Update(transaction);

        if (splits != null)
        {
            var existing = await _db.SplitTransactions
                .Where(s => s.TransactionId == transaction.Id).ToListAsync();
            _db.SplitTransactions.RemoveRange(existing);
            _db.SplitTransactions.AddRange(splits);
        }

        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task SoftDeleteAsync(int id)
    {
        var trx = await _db.Transactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");
        trx.DeletedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        trx.LastUpdatedTime = trx.DeletedTime;
        await _db.SaveChangesAsync();
    }

    public async Task HardDeleteAsync(int id)
    {
        // IgnoreQueryFilters needed since soft-deleted rows are normally filtered
        var trx = await _db.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");
        var splits = await _db.SplitTransactions.Where(s => s.TransactionId == id).ToListAsync();
        _db.SplitTransactions.RemoveRange(splits);
        _db.Transactions.Remove(trx);
        await _db.SaveChangesAsync();
    }
}
