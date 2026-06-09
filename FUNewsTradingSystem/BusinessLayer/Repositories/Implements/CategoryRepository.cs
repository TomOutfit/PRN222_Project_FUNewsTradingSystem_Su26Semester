using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Implements
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly FUNewsManagementContext _context;

        public CategoryRepository(FUNewsManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories
                .Include(c => c.ParentCategory)
                .ToListAsync();
        }

        public async Task<List<Category>> GetActiveAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive == true)
                .ToListAsync();
        }

        public async Task<List<Category>> GetTopLevelAsync()
        {
            return await _context.Categories
                .Where(c => c.ParentCategoryID == null)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<bool> IsReferencedByAnyArticleAsync(int categoryId)
        {
            return await _context.NewsArticles.AnyAsync(a => a.CategoryID == categoryId);
        }

        public async Task<bool> HasChildCategoriesReferencedByArticlesAsync(int categoryId)
        {
            var childCategoryIds = await _context.Categories
                .Where(c => c.ParentCategoryID == categoryId)
                .Select(c => c.CategoryID)
                .ToListAsync();

            if (!childCategoryIds.Any())
                return false;

            return await _context.NewsArticles.AnyAsync(a => childCategoryIds.Contains(a.CategoryID));
        }

        public async Task<Category> CreateAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateAsync(Category category)
        {
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == category.CategoryID);

            if (existing == null)
                throw new Exception("Category not found");

            existing.CategoryName = category.CategoryName;
            existing.CategoryDescription = category.CategoryDescription;
            existing.ParentCategoryID = category.ParentCategoryID;
            existing.IsActive = category.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task ToggleActiveAsync(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category != null)
            {
                category.IsActive = !category.IsActive;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteWithReparentChildrenAsync(int categoryId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var children = await _context.Categories.Where(c => c.ParentCategoryID == categoryId).ToListAsync();
                foreach (var child in children)
                {
                    child.ParentCategoryID = null;
                }
                
                var category = await _context.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    _context.Categories.Remove(category);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
