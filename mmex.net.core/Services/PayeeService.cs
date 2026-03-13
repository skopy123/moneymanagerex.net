using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public class PayeeService : IPayeeService
{
    private readonly MmexDbContext _db;

    public PayeeService(MmexDbContext db) => _db = db;

    public Task<IList<Payee>> GetAllAsync(bool activeOnly = true)
    {
        var q = _db.Payees.Include(p => p.Category).AsQueryable();
        if (activeOnly) q = q.Where(p => p.Active == 1);
        return q.OrderBy(p => p.Name).ToListAsync()
            .ContinueWith(t => (IList<Payee>)t.Result);
    }

    public Task<Payee?> GetByIdAsync(int id) =>
        _db.Payees.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Payee> CreateAsync(Payee payee)
    {
        _db.Payees.Add(payee);
        await _db.SaveChangesAsync();
        return payee;
    }

    public async Task<Payee> UpdateAsync(Payee payee)
    {
        _db.Payees.Update(payee);
        await _db.SaveChangesAsync();
        return payee;
    }

    public async Task DeleteAsync(int id)
    {
        var payee = await _db.Payees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Payee {id} not found.");
        _db.Payees.Remove(payee);
        await _db.SaveChangesAsync();
    }
}
