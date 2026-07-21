using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Implements;

public class SavedReportRepository : ISavedReportRepository
{
    private readonly FUNewsManagementContext _context;

    public SavedReportRepository(FUNewsManagementContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SavedReport>> GetByAccountAsync(int accountId)
    {
        return await _context.SavedReports
            .Where(sr => sr.AccountID == accountId)
            .Include(sr => sr.NewsArticle)
                .ThenInclude(na => na.Category)
            .Include(sr => sr.NewsArticle)
                .ThenInclude(na => na.NewsTagList)
                    .ThenInclude(nt => nt.Tag)
            .OrderByDescending(sr => sr.SavedDate)
            .ToListAsync();
    }

    public async Task<SavedReport?> GetByAccountAndArticleAsync(int accountId, int articleId)
    {
        return await _context.SavedReports
            .FirstOrDefaultAsync(sr => sr.AccountID == accountId && sr.NewsArticleID == articleId);
    }

    public async Task<bool> IsSavedAsync(int accountId, int articleId)
    {
        return await _context.SavedReports
            .AnyAsync(sr => sr.AccountID == accountId && sr.NewsArticleID == articleId);
    }

    public async Task SaveAsync(SavedReport savedReport)
    {
        await _context.SavedReports.AddAsync(savedReport);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(int accountId, int articleId)
    {
        var saved = await _context.SavedReports
            .FirstOrDefaultAsync(sr => sr.AccountID == accountId && sr.NewsArticleID == articleId);

        if (saved == null) return;

        _context.SavedReports.Remove(saved);
        await _context.SaveChangesAsync();
    }
}
