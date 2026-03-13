using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public class CurrencyService : ICurrencyService
{
    private readonly MmexDbContext _db;

    public CurrencyService(MmexDbContext db) => _db = db;

    public Task<IList<Currency>> GetAllAsync() =>
        _db.Currencies.OrderBy(c => c.Name).ToListAsync()
            .ContinueWith(t => (IList<Currency>)t.Result);

    public Task<Currency?> GetByIdAsync(int id) =>
        _db.Currencies.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Currency?> GetBySymbolAsync(string symbol) =>
        _db.Currencies.FirstOrDefaultAsync(c => c.Symbol == symbol);

    public async Task<Currency?> GetBaseCurrencyAsync()
    {
        // Base currency symbol is stored in INFOTABLE_V1 under key "BASECURRENCYID"
        var setting = await _db.InfoTable.FirstOrDefaultAsync(i => i.Name == "BASECURRENCYID");
        if (setting == null || !int.TryParse(setting.Value, out var id)) return null;
        return await GetByIdAsync(id);
    }

    public async Task<decimal> ConvertAsync(decimal amount, int fromCurrencyId, int toCurrencyId)
    {
        if (fromCurrencyId == toCurrencyId) return amount;

        var from = await GetByIdAsync(fromCurrencyId)
            ?? throw new KeyNotFoundException($"Currency {fromCurrencyId} not found.");
        var to = await GetByIdAsync(toCurrencyId)
            ?? throw new KeyNotFoundException($"Currency {toCurrencyId} not found.");

        var fromRate = from.BaseConvRate ?? 1m;
        var toRate = to.BaseConvRate ?? 1m;
        if (toRate == 0) return amount;

        return amount * fromRate / toRate;
    }

    public async Task<Currency> CreateAsync(Currency currency)
    {
        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync();
        return currency;
    }

    public async Task<Currency> UpdateAsync(Currency currency)
    {
        _db.Currencies.Update(currency);
        await _db.SaveChangesAsync();
        return currency;
    }
}
