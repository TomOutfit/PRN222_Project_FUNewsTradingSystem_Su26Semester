using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    public interface ITagRepository
    {
        Task<List<Tag>> GetAllAsync();
        Task<List<Tag>> GetAllForDropdownAsync();
        Task<Tag?> GetByIdAsync(int id);
        Task<bool> TagNameExistsAsync(string name, int? excludeId = null);
        Task<bool> IsReferencedByAnyArticleAsync(int tagId);
        Task<Tag> CreateAsync(Tag tag);
        Task UpdateAsync(Tag tag);
        Task DeleteAsync(int id);
    }
}
