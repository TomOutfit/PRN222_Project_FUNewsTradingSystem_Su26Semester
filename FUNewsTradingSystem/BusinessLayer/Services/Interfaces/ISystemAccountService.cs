using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ISystemAccountService
    {
        Task<SystemAccount?> AuthenticateAsync(string email, string passwordHash);
        Task<SystemAccount?> GetByEmailAsync(string email);
        Task<List<SystemAccount>> GetAllAsync();
        Task<SystemAccount?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(SystemAccount account);
        Task<ServiceResult> UpdateAsync(SystemAccount account);
        Task<ServiceResult> DeleteAsync(int id);
    }
}
