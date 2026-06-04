using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ITagService
    {
        Task<List<Tag>> GetAllAsync();
        Task<Tag?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Tag tag);
        Task<ServiceResult> UpdateAsync(Tag tag);
        Task<ServiceResult> DeleteAsync(int id);
    }
}
