using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<Category>> GetActiveAsync();
        Task<List<Category>> GetTopLevelAsync();
        Task<Category?> GetCategoryByIdAsync(int id); 
        Task<bool> CreateCategoryAsync(Category category); 
        Task<bool> UpdateCategoryAsync(Category category); 
        Task<bool> ToggleActiveAsync(int id);
        Task<bool> DeleteCategoryAsync(int id);
    }
}