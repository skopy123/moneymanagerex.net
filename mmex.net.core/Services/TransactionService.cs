using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmex.net.core.Data;
using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Extensions;

namespace mmex.net.core.Services;

public class TransactionService : ITransactionService
{
    private readonly MmexDbContext _db;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(MmexDbContext db, ILogger<TransactionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IList<Transaction>> GetByAccountAsync(long accountId, DateOnly? from = null, DateOnly? to = null)
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

    public async Task<IList<(Transaction Transaction, decimal RunningBalance)>> GetRunningBalanceAsync(long accountId)
    {
        var sw = Stopwatch.StartNew();

        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");
        _logger.LogDebug("GetRunningBalanceAsync({AccountId}): account lookup {Elapsed}ms", accountId, sw.ElapsedMilliseconds);

        // Do NOT filter Void in SQL: SQLite evaluates `STATUS <> 'V'` as NULL for rows
        // where STATUS IS NULL (three-valued logic), silently dropping those rows.
        // Filter in C# after loading so all rows including transfers are fetched.
        var transactions = await _db.Transactions
            .Include(t => t.Payee)
            .Include(t => t.Category)
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId)
            .OrderBy(t => t.Date).ThenBy(t => t.Id)
            .ToListAsync();

        var transferCount = transactions.Count(t => t.Type == TransactionType.Transfer);
        _logger.LogInformation(
            "GetRunningBalanceAsync({AccountId}): {Total} rows ({Transfers} transfers) loaded in {Elapsed}ms",
            accountId, transactions.Count, transferCount, sw.ElapsedMilliseconds);

        // Exclude voids in C# (null-safe)
        transactions = transactions.Where(t => t.Status != TransactionStatus.Void).ToList();

        var result = new List<(Transaction, decimal)>(transactions.Count);
        var running = account.InitialBalance;
        foreach (var trx in transactions)
        {
            running += trx.GetFlow(accountId);
            result.Add((trx, running));
        }
        return result;
    }

    public Task<Transaction?> GetByIdAsync(long id) =>
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
            foreach (var split in splits)
                transaction.Splits.Add(split);

        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction, IList<SplitTransaction>? splits = null)
    {
        transaction.LastUpdatedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // Find the already-tracked entity (loaded by the list query) and update its
        // scalar values in-place. Attaching a second instance with the same PK would
        // throw InvalidOperationException due to the identity-map conflict.
        var tracked = await _db.Transactions.FindAsync(transaction.Id)
            ?? throw new KeyNotFoundException($"Transaction {transaction.Id} not found.");
        _db.Entry(tracked).CurrentValues.SetValues(transaction);

        if (splits != null)
        {
            var existingSplits = await _db.SplitTransactions
                .Where(s => s.TransactionId == transaction.Id).ToListAsync();
            _db.SplitTransactions.RemoveRange(existingSplits);
            _db.SplitTransactions.AddRange(splits);
        }

        await _db.SaveChangesAsync();
        return tracked;
    }

    public async Task SoftDeleteAsync(long id)
    {
        var trx = await _db.Transactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");
        trx.DeletedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        trx.LastUpdatedTime = trx.DeletedTime;
        await _db.SaveChangesAsync();
    }

    public async Task HardDeleteAsync(long id)
    {
        var trx = await _db.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");
        var splits = await _db.SplitTransactions.Where(s => s.TransactionId == id).ToListAsync();
        _db.SplitTransactions.RemoveRange(splits);
        _db.Transactions.Remove(trx);
        await _db.SaveChangesAsync();
    }
}
