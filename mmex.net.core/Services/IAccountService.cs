using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IAccountService
{
    Task<IList<Account>> GetAllAsync();
    Task<IList<Account>> GetAllOpenAsync();
    Task<IList<Account>> GetFavoritesAsync();
    Task<Account?> GetByIdAsync(long id);
    Task<Account> CreateAsync(Account account);
    Task<Account> UpdateAsync(Account account);
    Task DeleteAsync(long id);
    Task<decimal> GetBalanceAsync(long accountId);
    Task<decimal> GetBalanceAtDateAsync(long accountId, DateOnly date);
}
