using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface INewsArticleService
    {
        Task<List<NewsArticle>> GetActiveAsync();
        Task<NewsArticle?> GetByIdAsync(int id);
        Task<List<NewsArticle>> GetByCreatorAsync(int accountId);
        Task<List<NewsArticle>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc);
        Task<ServiceResult> CreateWithTagAsync(NewsArticle article, int tagId);
        Task<ServiceResult> ToggleStatusAsync(int newsArticleId, int updatedByAccountId);
    }
}
