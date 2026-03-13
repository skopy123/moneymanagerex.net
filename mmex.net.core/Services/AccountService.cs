using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Extensions;

namespace mmex.net.core.Services;

public class AccountService : IAccountService
{
    private readonly MmexDbContext _db;

    public AccountService(MmexDbContext db) => _db = db;

    public Task<IList<Account>> GetAllAsync() =>
        _db.Accounts.Include(a => a.Currency).OrderBy(a => a.Name)
            .ToListAsync().ContinueWith(t => (IList<Account>)t.Result);

    public Task<IList<Account>> GetAllOpenAsync() =>
        _db.Accounts.Include(a => a.Currency)
            .Where(a => a.Status == AccountStatus.Open)
            .OrderBy(a => a.Name)
            .ToListAsync().ContinueWith(t => (IList<Account>)t.Result);

    public Task<IList<Account>> GetFavoritesAsync() =>
        _db.Accounts.Include(a => a.Currency)
            .Where(a => a.IsFavorite && a.Status == AccountStatus.Open)
            .OrderBy(a => a.Name)
            .ToListAsync().ContinueWith(t => (IList<Account>)t.Result);

    public Task<Account?> GetByIdAsync(int id) =>
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

    public async Task DeleteAsync(int id)
    {
        var account = await _db.Accounts.FindAsync(id)
            ?? throw new KeyNotFoundException($"Account {id} not found.");
        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetBalanceAsync(int accountId)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        var transactions = await GetBalanceTransactionsQuery(accountId).ToListAsync();
        return account.InitialBalance + transactions.Sum(t => t.GetFlow(accountId));
    }

    public async Task<decimal> GetBalanceAtDateAsync(int accountId, DateOnly date)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        var dateStr = date.ToString("yyyy-MM-dd");
        var transactions = await GetBalanceTransactionsQuery(accountId)
            .Where(t => string.Compare(t.Date, dateStr) <= 0)
            .ToListAsync();

        return account.InitialBalance + transactions.Sum(t => t.GetFlow(accountId));
    }

    private IQueryable<Transaction> GetBalanceTransactionsQuery(int accountId) =>
        _db.Transactions
            // Query filter already excludes deleted; exclude void as well
            .Where(t => t.Status != TransactionStatus.Void)
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId);
}
