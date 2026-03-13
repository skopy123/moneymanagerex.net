using mmex.net.core.Entities;

namespace mmex.net.core.Services;

public interface ICategoryService
{
    Task<IList<Category>> GetAllAsync();
    Task<IList<Category>> GetRootCategoriesAsync();
    Task<IList<Category>> GetChildrenAsync(int parentId);
    Task<Category?> GetByIdAsync(int id);
    Task<string> GetFullNameAsync(int categoryId);
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(int id);
}
