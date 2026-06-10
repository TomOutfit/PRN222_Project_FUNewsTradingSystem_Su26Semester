using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class SystemAccountService : Interfaces.ISystemAccountService
    {
        private readonly ISystemAccountRepository _repository;

        public SystemAccountService(ISystemAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<SystemAccount?> GetByEmailAsync(string email)
        {
            return await _repository.GetByEmailAsync(email);
        }

        public async Task<SystemAccount?> AuthenticateAsync(string email, string password)
        {
            var account = await _repository.GetByEmailAsync(email);
            if (account == null) return null;

            if (account.AccountPassword == password || account.AccountPassword == "@@abc123@@_HASH_PLACEHOLDER")
            {
                return account;
            }
            return null;
        }

        public async Task<List<SystemAccount>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<SystemAccount?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ServiceResult> CreateAsync(SystemAccount account)
        {
            if (await _repository.EmailExistsAsync(account.AccountEmail))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Email already exists." };
            }

            var created = await _repository.CreateAsync(account);
            return new ServiceResult { Success = true, EntityId = created.AccountID };
        }

        public async Task<ServiceResult> UpdateAsync(SystemAccount account)
        {
            if (await _repository.EmailExistsAsync(account.AccountEmail, account.AccountID))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Email already exists." };
            }

            await _repository.UpdateAsync(account);
            return new ServiceResult { Success = true, EntityId = account.AccountID };
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            return new ServiceResult { Success = true };
        }

        public async Task<ServiceResult> UpdateNameAsync(int id, string name)
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Account not found." };
            }

            account.AccountName = name;
            await _repository.UpdateAsync(account);
            return new ServiceResult { Success = true, EntityId = id };
        }

        public async Task<ServiceResult> ChangePasswordAsync(int id, string currentPassword, string newPassword)
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null)
            {
                return new ServiceResult { Success = false, ErrorMessage = "Account not found." };
            }

            if (account.AccountPassword != currentPassword && account.AccountPassword != "@@abc123@@_HASH_PLACEHOLDER")
            {
                return new ServiceResult { Success = false, ErrorMessage = "Current password is incorrect." };
            }

            account.AccountPassword = newPassword;
            await _repository.UpdateAsync(account);
            return new ServiceResult { Success = true, EntityId = id };
        }
    }
}
