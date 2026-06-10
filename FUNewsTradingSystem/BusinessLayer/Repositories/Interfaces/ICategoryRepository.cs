using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Data-access operations for <see cref="Category"/> entities, including hierarchical parent-child relationships.
    /// </summary>
    public interface ICategoryRepository
    {
        /// <summary>Returns all categories, eager-loading each one's <see cref="Category.ParentCategory"/>.</summary>
        Task<List<Category>> GetAllAsync();

        /// <summary>Returns only categories where <see cref="Category.IsActive"/> is true, ordered by name.</summary>
        Task<List<Category>> GetActiveAsync();

        /// <summary>Returns categories whose <see cref="Category.ParentCategoryID"/> is null (top-level only).</summary>
        Task<List<Category>> GetTopLevelAsync();

        /// <summary>Retrieves a single category by primary key, or null if not found.</summary>
        Task<Category?> GetByIdAsync(int id);

        /// <summary>Checks whether any <see cref="NewsArticle"/> references the given category.</summary>
        Task<bool> IsReferencedByAnyArticleAsync(int categoryId);

        /// <summary>Checks whether any child category of the given parent is referenced by at least one article.</summary>
        Task<bool> HasChildCategoriesReferencedByArticlesAsync(int categoryId);

        /// <summary>Inserts a new category and returns the persisted entity (including the assigned ID).</summary>
        Task<Category> CreateAsync(Category category);

        /// <summary>Persists changes to an existing category. Caller is responsible for ensuring the entity is tracked.</summary>
        Task UpdateAsync(Category category);

        /// <summary>Flips <see cref="Category.IsActive"/> to its opposite value for the given category.</summary>
        Task ToggleActiveAsync(int categoryId);

        /// <summary>Deletes the category and sets its children's <see cref="Category.ParentCategoryID"/> to null
        /// in a single transaction. Throws if the category or any of its children are still referenced by articles.</summary>
        Task DeleteWithReparentChildrenAsync(int categoryId);
    }
}
