using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmex.net.core.Data;
using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Extensions;

namespace mmex.net.core.Services;

public class AccountService : IAccountService
{
    private readonly MmexDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(MmexDbContext db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<IList<Account>> GetAllAsync() =>
        _db.Accounts.Include(a => a.Currency).OrderBy(a => a.Name)
            .ToListAsync().ContinueWith(t => (IList<Account>)t.Result);

    public async Task<IList<Account>> GetAllOpenAsync()
    {
        var sw = Stopwatch.StartNew();
        var result = await _db.Accounts.Include(a => a.Currency)
            .Where(a => a.Status == AccountStatus.Open)
            .OrderBy(a => a.Name)
            .ToListAsync();
        _logger.LogInformation("GetAllOpenAsync: {Count} accounts in {Elapsed}ms", result.Count, sw.ElapsedMilliseconds);
        return result;
    }

    public Task<IList<Account>> GetFavoritesAsync() =>
        _db.Accounts.Include(a => a.Currency)
            .Where(a => a.IsFavorite && a.Status == AccountStatus.Open)
            .OrderBy(a => a.Name)
            .ToListAsync().ContinueWith(t => (IList<Account>)t.Result);

    public Task<Account?> GetByIdAsync(long id) =>
        _db.Accounts.Include(a => a.Currency).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Account> CreateAsync(Account account)
    {
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task<Account> UpdateAsync(Account account)
    {
        _db.Accounts.Update(account);
        await _db.SaveChangesAsync();
        return account;
    }

    public async Task DeleteAsync(long id)
    {
        var account = await _db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException($"Account {id} not found.");
        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetBalanceAsync(long accountId)
    {
        var sw = Stopwatch.StartNew();
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        var transactions = await GetBalanceTransactionsQuery(accountId).ToListAsync();
        var balance = account.InitialBalance + transactions.Sum(t => t.GetFlow(accountId));
        _logger.LogInformation("GetBalanceAsync({AccountId}): {Count} transactions in {Elapsed}ms, balance={Balance}",
            accountId, transactions.Count, sw.ElapsedMilliseconds, balance);
        return balance;
    }

    public async Task<decimal> GetBalanceAtDateAsync(long accountId, DateOnly date)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        var dateStr = date.ToString("yyyy-MM-dd");
        var transactions = await GetBalanceTransactionsQuery(accountId)
            .Where(t => string.Compare(t.Date, dateStr) <= 0)
            .ToListAsync();

        return account.InitialBalance + transactions.Sum(t => t.GetFlow(accountId));
    }

    private IQueryable<Transaction> GetBalanceTransactionsQuery(long accountId) =>
        _db.Transactions
            .Where(t => t.Status != TransactionStatus.Void)
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId);
}
