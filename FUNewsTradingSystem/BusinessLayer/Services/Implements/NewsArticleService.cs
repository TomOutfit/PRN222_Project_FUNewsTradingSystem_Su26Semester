using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class NewsArticleService : Interfaces.INewsArticleService
    {
        private readonly INewsArticleRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;

        public NewsArticleService(INewsArticleRepository repository, ICategoryRepository categoryRepository, ITagRepository tagRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
        }

        public async Task<List<NewsArticle>> GetActiveAsync() => await _repository.GetActiveAsync();
        public async Task<NewsArticle?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);
        public async Task<List<NewsArticle>> GetByCreatorAsync(int accountId) => await _repository.GetByCreatorAsync(accountId);
        public async Task<List<NewsArticle>> GetByDateRangeAsync(DateTime startUtc, DateTime endUtc) => await _repository.GetByDateRangeAsync(startUtc, endUtc);

        public async Task<ServiceResult> CreateWithTagAsync(NewsArticle article, int tagId)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(article.NewsTitle) || string.IsNullOrWhiteSpace(article.NewsContent))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Title and content are required." };
            }

            // Note: Add profanity/spam logic placeholder here if needed

            if (article.CategoryID > 0)
            {
                var category = await _categoryRepository.GetByIdAsync(article.CategoryID);
                if (category == null)
                {
                    return new ServiceResult { Success = false, ErrorMessage = "Invalid Category." };
                }
            }
            else
            {
                return new ServiceResult { Success = false, ErrorMessage = "Category is required." };
            }

            var tag = await _tagRepository.GetByIdAsync(tagId);
            if (tag == null)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Invalid Tag." };
            }

            var newId = await _repository.CreateWithTagAsync(article, tagId);
            return new ServiceResult { Success = true, EntityId = newId };
        }

        public async Task<ServiceResult> ToggleStatusAsync(int newsArticleId, int updatedByAccountId)
        {
            await _repository.ToggleStatusAsync(newsArticleId, updatedByAccountId);
            return new ServiceResult { Success = true };
        }

        public async Task<List<NewsArticle>> GetActiveReportsAsync()
        {
            return await _repository.GetActiveReportsAsync();
        }

        public async Task<NewsArticle?> GetReportDetailAsync(int id)
        {
            return await _repository.GetReportDetailAsync(id);
        }
    }
}
