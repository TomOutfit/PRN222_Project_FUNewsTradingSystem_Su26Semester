using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Category>> GetAllCategoriesAsync() => await _repository.GetAllAsync();
        public async Task<List<Category>> GetActiveAsync() => await _repository.GetActiveAsync();
        public async Task<List<Category>> GetTopLevelAsync() => await _repository.GetTopLevelAsync();
        public async Task<Category?> GetCategoryByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            var created = await _repository.CreateAsync(category);
            return created != null && created.CategoryID > 0;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            var existing = await _repository.GetByIdAsync(category.CategoryID);
            if (existing == null) return false;

            await _repository.UpdateAsync(category);
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return false;

            category.IsActive = !category.IsActive;
            await _repository.UpdateAsync(category);
            return category.IsActive;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            if (await _repository.IsReferencedByAnyArticleAsync(id) ||
                await _repository.HasChildCategoriesReferencedByArticlesAsync(id))
            {
                return false; 
            }

            await _repository.DeleteWithReparentChildrenAsync(id);
            return true;
        }
    }
}