using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;

public interface ISavedReportRepository
{
    Task<IEnumerable<SavedReport>> GetByAccountAsync(int accountId);
    Task<SavedReport?> GetByAccountAndArticleAsync(int accountId, int articleId);
    Task<bool> IsSavedAsync(int accountId, int articleId);
    Task SaveAsync(SavedReport savedReport);
    Task RemoveAsync(int accountId, int articleId);
}
