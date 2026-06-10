using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Implements
{
    public class SystemAccountRepository : ISystemAccountRepository
    {
        private readonly FUNewsManagementContext _context;

        public SystemAccountRepository(FUNewsManagementContext context)
        {
            _context = context;
        }

        public async Task<SystemAccount?> GetByEmailAsync(string email)
        {
            return await _context.SystemAccounts.FirstOrDefaultAsync(x => x.AccountEmail == email);
        }

        public async Task<List<SystemAccount>> GetAllAsync()
        {
            return await _context.SystemAccounts.ToListAsync();
        }

        public async Task<SystemAccount?> GetByIdAsync(int id)
        {
            return await _context.SystemAccounts.FindAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.SystemAccounts.AnyAsync(x => x.AccountEmail == email && x.AccountID != excludeId.Value);
            }
            return await _context.SystemAccounts.AnyAsync(x => x.AccountEmail == email);
        }

        public async Task<SystemAccount> CreateAsync(SystemAccount account)
        {
            _context.SystemAccounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task UpdateAsync(SystemAccount account)
        {
            _context.SystemAccounts.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var account = await _context.SystemAccounts.FindAsync(id);
            if (account != null)
            {
                _context.SystemAccounts.Remove(account);
                await _context.SaveChangesAsync();
            }
        }
    }
}
