using FUNewsTradingSystem_DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    public interface INewsArticleRepository
    {
        Task<List<NewsArticle>> GetActiveAsync();
        Task<NewsArticle?> GetByIdAsync(int id);
        Task<List<NewsArticle>> GetByCreatorAsync(int accountId);
        Task<List<NewsArticle>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc);
        Task<int> CreateWithTagAsync(NewsArticle article, int tagId);
        Task ToggleStatusAsync(int newsArticleId, int updatedByAccountId);
        Task<List<NewsArticle>> GetActiveReportsAsync();
        Task<NewsArticle?> GetReportDetailAsync(int id);
    }
}
