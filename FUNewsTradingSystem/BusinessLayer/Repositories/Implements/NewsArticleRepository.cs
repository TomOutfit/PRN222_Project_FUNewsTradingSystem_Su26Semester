using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Implements
{
    public class NewsArticleRepository : INewsArticleRepository
    {
        private readonly FUNewsManagementContext _context;

        public NewsArticleRepository(FUNewsManagementContext context)
        {
            _context = context;
        }

        public async Task<List<NewsArticle>> GetActiveAsync()
        {
            return await _context.NewsArticles
                .Include(na => na.Category)
                .Include(na => na.NewsTagList)
                    .ThenInclude(nt => nt.Tag)
                .Where(na => na.NewsStatus == true)
                .ToListAsync();
        }

        public async Task<NewsArticle?> GetByIdAsync(int id)
        {
            return await _context.NewsArticles
                .Include(na => na.Category)
                .Include(na => na.CreatedByAccount)
                .Include(na => na.UpdatedByAccount)
                .Include(na => na.NewsTagList)
                    .ThenInclude(nt => nt.Tag)
                .FirstOrDefaultAsync(na => na.NewsArticleID == id);
        }

        public async Task<List<NewsArticle>> GetByCreatorAsync(int accountId)
        {
            return await _context.NewsArticles
                .Include(na => na.Category)
                .Where(na => na.CreatedByID == accountId)
                .ToListAsync();
        }

        public async Task<List<NewsArticle>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc)
        {
            return await _context.NewsArticles
                .Include(na => na.Category)
                .Include(na => na.CreatedByAccount)
                .Where(na => na.CreatedDate >= startUtc && na.CreatedDate <= endUtc)
                .ToListAsync();
        }

        public async Task<int> CreateWithTagAsync(NewsArticle article, int tagId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.NewsArticles.Add(article);
                await _context.SaveChangesAsync();

                var newsTag = new NewsTag
                {
                    NewsArticleID = article.NewsArticleID,
                    TagID = tagId
                };
                _context.NewsTags.Add(newsTag);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return article.NewsArticleID;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ToggleStatusAsync(int newsArticleId, int updatedByAccountId)
        {
            var article = await _context.NewsArticles.FindAsync(newsArticleId);
            if (article != null)
            {
                article.NewsStatus = !article.NewsStatus;
                article.UpdatedByID = updatedByAccountId;
                article.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<NewsArticle>> GetActiveReportsAsync()
        {
            return await _context.NewsArticles
                .Include(x => x.Category)
                .Include(x => x.NewsTagList)
                    .ThenInclude(x => x.Tag)
                .Where(x => x.NewsStatus)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<NewsArticle?> GetReportDetailAsync(int id)
        {
            return await _context.NewsArticles
                .Include(x => x.Category)
                .Include(x => x.NewsTagList)
                    .ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(x =>
                    x.NewsArticleID == id &&
                    x.NewsStatus);
        }
    }
}
