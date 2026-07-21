using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements;

public class SavedReportService : ISavedReportService
{
    private readonly ISavedReportRepository _repo;

    public SavedReportService(ISavedReportRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<SavedReport>> GetUserSavedReportsAsync(int accountId)
        => await _repo.GetByAccountAsync(accountId);

    public async Task<bool> SaveReportAsync(int accountId, int articleId)
    {
        if (await _repo.IsSavedAsync(accountId, articleId))
            return false; // already saved

        await _repo.SaveAsync(new SavedReport
        {
            AccountID = accountId,
            NewsArticleID = articleId,
            SavedDate = DateTime.UtcNow
        });
        return true;
    }

    public async Task<bool> RemoveBookmarkAsync(int accountId, int articleId)
    {
        if (!await _repo.IsSavedAsync(accountId, articleId))
            return false; // not saved

        await _repo.RemoveAsync(accountId, articleId);
        return true;
    }

    public async Task<bool> IsBookmarkedAsync(int accountId, int articleId)
        => await _repo.IsSavedAsync(accountId, articleId);
}
