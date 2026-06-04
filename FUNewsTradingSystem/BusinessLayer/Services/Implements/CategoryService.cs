using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class CategoryService : Interfaces.ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Category>> GetAllAsync() => await _repository.GetAllAsync();
        public async Task<List<Category>> GetActiveAsync() => await _repository.GetActiveAsync();
        public async Task<List<Category>> GetTopLevelAsync() => await _repository.GetTopLevelAsync();
        public async Task<Category?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public async Task<ServiceResult> CreateAsync(Category category)
        {
            var created = await _repository.CreateAsync(category);
            return new ServiceResult { Success = true, EntityId = created.CategoryID };
        }

        public async Task<ServiceResult> UpdateAsync(Category category)
        {
            await _repository.UpdateAsync(category);
            return new ServiceResult { Success = true, EntityId = category.CategoryID };
        }

        public async Task<ServiceResult> ToggleActiveAsync(int categoryId)
        {
            await _repository.ToggleActiveAsync(categoryId);
            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> DeleteAsync(int categoryId)
        {
            if (await _repository.IsReferencedByAnyArticleAsync(categoryId))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Cannot delete category: it is referenced by one or more articles." };
            }

            if (await _repository.HasChildCategoriesReferencedByArticlesAsync(categoryId))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Cannot delete category: its child categories are referenced by one or more articles." };
            }

            await _repository.DeleteWithReparentChildrenAsync(categoryId);
            return new ServiceResult { Success = true };
        }
    }
}
