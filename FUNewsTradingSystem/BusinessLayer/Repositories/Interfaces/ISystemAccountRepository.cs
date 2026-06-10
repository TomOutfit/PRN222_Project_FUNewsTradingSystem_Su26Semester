using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Data-access operations for <see cref="SystemAccount"/> entities.
    /// </summary>
    public interface ISystemAccountRepository
    {
        /// <summary>Retrieves the account with the given email address, or null if none exists.</summary>
        Task<SystemAccount?> GetByEmailAsync(string email);

        /// <summary>Returns all system accounts ordered by AccountID.</summary>
        Task<List<SystemAccount>> GetAllAsync();

        /// <summary>Retrieves the account with the given primary key, or null if not found.</summary>
        Task<SystemAccount?> GetByIdAsync(int id);

        /// <summary>Checks whether any account already uses the given email address.
        /// Pass <paramref name="excludeId"/> to skip the row with that ID (used during updates).</summary>
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);

        /// <summary>Inserts a new account and returns the persisted entity (including the assigned ID).</summary>
        Task<SystemAccount> CreateAsync(SystemAccount account);

        /// <summary>Persists changes to an existing account. Caller is responsible for ensuring the entity is tracked.</summary>
        Task UpdateAsync(SystemAccount account);

        /// <summary>Deletes the account with the given primary key. No-op if the row does not exist.</summary>
        Task DeleteAsync(int id);
    }
}
