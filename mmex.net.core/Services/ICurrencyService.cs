using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface ICurrencyService
{
    Task<IList<Currency>> GetAllAsync();
    Task<Currency?> GetByIdAsync(int id);
    Task<Currency?> GetBySymbolAsync(string symbol);
    Task<Currency?> GetBaseCurrencyAsync();
    Task<decimal> ConvertAsync(decimal amount, int fromCurrencyId, int toCurrencyId);
    Task<Currency> CreateAsync(Currency currency);
    Task<Currency> UpdateAsync(Currency currency);
}
