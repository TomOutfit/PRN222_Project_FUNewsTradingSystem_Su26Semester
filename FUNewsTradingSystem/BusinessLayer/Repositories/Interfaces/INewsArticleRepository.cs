using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Data-access operations for <see cref="NewsArticle"/> entities and their junction table <see cref="NewsTag"/>.
    /// </summary>
    public interface INewsArticleRepository
    {
        /// <summary>Returns all articles where <see cref="NewsArticle.NewsStatus"/> is true,
        /// eager-loading <see cref="NewsArticle.Category"/> and <see cref="NewsArticle.NewsTagList"/>
        /// (which in turn eager-loades each <see cref="NewsTag.Tag"/>).</summary>
        Task<List<NewsArticle>> GetActiveAsync();

        /// <summary>Retrieves a single article by primary key, eager-loading all navigation properties.</summary>
        Task<NewsArticle?> GetByIdAsync(int id);

        /// <summary>Returns all articles created by the given account, ordered by <see cref="NewsArticle.CreatedDate"/> descending.</summary>
        Task<List<NewsArticle>> GetByCreatorAsync(int accountId);

        /// <summary>Returns articles whose <see cref="NewsArticle.CreatedDate"/> falls within the
        /// [startUtc, endUtc] range (inclusive), eager-loading <see cref="NewsArticle.Category"/>
        /// and <see cref="NewsArticle.CreatedByAccount"/>.</summary>
        Task<List<NewsArticle>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc);

        /// <summary>Inserts an article and its corresponding <see cref="NewsTag"/> entry inside a single
        /// transaction, then returns the newly assigned <see cref="NewsArticle.NewsArticleID"/>.</summary>
        Task<int> CreateWithTagAsync(NewsArticle article, int tagId);

        /// <summary>Flips <see cref="NewsArticle.NewsStatus"/> to its opposite value,
        /// sets <see cref="NewsArticle.UpdatedByID"/> to <paramref name="updatedByAccountId"/>,
        /// and updates <see cref="NewsArticle.ModifiedDate"/> to UTC now.</summary>
        Task ToggleStatusAsync(int newsArticleId, int updatedByAccountId);
    }
}
