using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    public interface ITagRepository
    {
        Task<List<Tag>> GetAllAsync();
        Task<Tag?> GetByIdAsync(int id);
        Task AddAsync(Tag tag);
        Task UpdateAsync(Tag tag);
        Task DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string tagName);
    }
}
