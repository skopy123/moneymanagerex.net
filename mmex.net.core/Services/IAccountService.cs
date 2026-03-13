using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IAccountService
{
    Task<IList<Account>> GetAllAsync();
    Task<IList<Account>> GetAllOpenAsync();
    Task<IList<Account>> GetFavoritesAsync();
    Task<Account?> GetByIdAsync(int id);
    Task<Account> CreateAsync(Account account);
    Task<Account> UpdateAsync(Account account);
    Task DeleteAsync(int id);
    Task<decimal> GetBalanceAsync(int accountId);
    Task<decimal> GetBalanceAtDateAsync(int accountId, DateOnly date);
}
