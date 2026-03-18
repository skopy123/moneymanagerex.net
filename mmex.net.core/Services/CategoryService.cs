using Microsoft.EntityFrameworkCore;
using mmex.net.core.Data;
using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public class CategoryService : ICategoryService
{
    private readonly MmexDbContext _db;

    public CategoryService(MmexDbContext db) => _db = db;

    public Task<IList<Category>> GetAllAsync() =>
        _db.Categories.OrderBy(c => c.Name).ToListAsync()
            .ContinueWith(t => (IList<Category>)t.Result);

    public Task<IList<Category>> GetRootCategoriesAsync() =>
        _db.Categories.Where(c => c.ParentId == null || c.ParentId == -1)
            .OrderBy(c => c.Name).ToListAsync()
            .ContinueWith(t => (IList<Category>)t.Result);

    public Task<IList<Category>> GetChildrenAsync(long parentId) =>
        _db.Categories.Where(c => c.ParentId == parentId)
            .OrderBy(c => c.Name).ToListAsync()
            .ContinueWith(t => (IList<Category>)t.Result);

    public Task<Category?> GetByIdAsync(long id) =>
        _db.Categories.Include(c => c.Parent).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<string> GetFullNameAsync(long categoryId)
    {
        var all = await _db.Categories.ToDictionaryAsync(c => c.Id);
        if (!all.TryGetValue(categoryId, out var cat)) return string.Empty;

        var parts = new List<string>();
        var current = cat;
        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ParentId.HasValue && current.ParentId > 0
                ? all.GetValueOrDefault(current.ParentId.Value)
                : null;
        }
        return string.Join(" : ", parts);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task DeleteAsync(long id)
    {
        var cat = await _db.Categories.FindAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
    }
}
