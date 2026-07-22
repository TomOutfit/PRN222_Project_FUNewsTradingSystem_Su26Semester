using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Data-access operations for <see cref="Tag"/> entities (ticker symbols).
    /// </summary>
    public interface ITagRepository
    {
        /// <summary>Returns all tags ordered by <see cref="Tag.TagName"/>.</summary>
        Task<List<Tag>> GetAllAsync();

        /// <summary>Retrieves a single tag by primary key, or null if not found.</summary>
        Task<Tag?> GetByIdAsync(int id);

        /// <summary>Adds a new tag to the database. The caller should normalise the name to uppercase beforehand.</summary>
        Task AddAsync(Tag tag);

        /// <summary>Persists changes to an existing tag. Caller is responsible for ensuring the entity is tracked.</summary>
        Task UpdateAsync(Tag tag);

        /// <summary>Deletes the tag with the given primary key. Throws if the tag is referenced by any <see cref="NewsTag"/>.</summary>
        Task DeleteAsync(int id);

        /// <summary>Checks whether any tag with the given name already exists. Returns true regardless of case
        /// since names are stored normalised to uppercase.</summary>
        Task<bool> ExistsByNameAsync(string tagName);

        /// <summary>Returns all tags mapped to the specified category via TagCategoryMap.</summary>
        Task<List<Tag>> GetTagsByCategoryAsync(int categoryId);

        /// <summary>Returns true if the given tag is mapped to the given category.</summary>
        Task<bool> ValidateTagCategoryPairingAsync(int tagId, int categoryId);

        /// <summary>Returns the CategoryID the given tag maps to, or null if no mapping exists.</summary>
        Task<int?> GetCategoryByTagAsync(int tagId);
    }
}
