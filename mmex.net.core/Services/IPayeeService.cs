using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface IPayeeService
{
    Task<IList<Payee>> GetAllAsync(bool activeOnly = true);
    Task<Payee?> GetByIdAsync(long id);
    Task<Payee> CreateAsync(Payee payee);
    Task<Payee> UpdateAsync(Payee payee);
    Task DeleteAsync(long id);
}
