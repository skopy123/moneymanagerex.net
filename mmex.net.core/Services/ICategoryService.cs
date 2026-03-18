using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface ICategoryService
{
    Task<IList<Category>> GetAllAsync();
    Task<IList<Category>> GetRootCategoriesAsync();
    Task<IList<Category>> GetChildrenAsync(long parentId);
    Task<Category?> GetByIdAsync(long id);
    Task<string> GetFullNameAsync(long categoryId);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(long id);
}
