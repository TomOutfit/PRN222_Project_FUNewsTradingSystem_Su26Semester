using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<List<Category>> GetActiveAsync();
        Task<List<Category>> GetTopLevelAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Category category);
        Task<ServiceResult> UpdateAsync(Category category);
        Task<ServiceResult> ToggleActiveAsync(int categoryId);
        Task<ServiceResult> DeleteAsync(int categoryId);
    }
}
