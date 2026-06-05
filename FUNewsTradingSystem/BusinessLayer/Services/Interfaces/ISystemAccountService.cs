using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ISystemAccountService
    {
        Task<SystemAccount?> AuthenticateAsync(string email, string passwordHash);
        Task<List<SystemAccount>> GetAllAsync();
        Task<SystemAccount?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(SystemAccount account);
        Task<ServiceResult> UpdateAsync(SystemAccount account);
        Task<ServiceResult> DeleteAsync(int id);
        Task<ServiceResult> UpdateNameAsync(int id, string name);
        Task<ServiceResult> ChangePasswordAsync(int id, string currentPassword, string newPassword);
    }
}
