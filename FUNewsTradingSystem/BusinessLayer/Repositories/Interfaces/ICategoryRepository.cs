using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(); // eager-load ParentCategory
        Task<List<Category>> GetActiveAsync();
        Task<List<Category>> GetTopLevelAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<bool> IsReferencedByAnyArticleAsync(int categoryId);
        Task<bool> HasChildCategoriesReferencedByArticlesAsync(int categoryId);
        Task<Category> CreateAsync(Category category);
        Task UpdateAsync(Category category);
        Task ToggleActiveAsync(int categoryId);
        Task DeleteWithReparentChildrenAsync(int categoryId);
    }
}
