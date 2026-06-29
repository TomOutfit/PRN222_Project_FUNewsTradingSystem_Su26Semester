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
        Task<List<NewsArticle>> GetActiveReportsAsync(int? categoryId = null, int? tagId = null, string? decision = null);
        Task<NewsArticle?> GetReportDetailAsync(int id);
        Task<List<NewsArticle>> GetReportsByCreatorAsync(int accountId, int? categoryId = null, int? tagId = null, string? decision = null);
        Task<bool> ToggleStatusAsync(int newsId, int accountId);
    }
}
