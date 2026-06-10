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
    }
}
