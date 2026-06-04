using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    public interface INewsArticleRepository
    {
        Task<List<NewsArticle>> GetActiveAsync(); // eager-load Category + NewsTag.Tag
        Task<NewsArticle?> GetByIdAsync(int id); // all nav props
        Task<List<NewsArticle>> GetByCreatorAsync(int accountId);
        Task<List<NewsArticle>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc); // eager-load Category + CreatedByAccount
        Task<int> CreateWithTagAsync(NewsArticle article, int tagId);
        Task ToggleStatusAsync(int newsArticleId, int updatedByAccountId);
    }
}
