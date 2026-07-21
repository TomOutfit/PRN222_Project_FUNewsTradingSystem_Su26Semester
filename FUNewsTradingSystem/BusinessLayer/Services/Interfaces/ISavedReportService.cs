using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces;

public interface ISavedReportService
{
    Task<IEnumerable<SavedReport>> GetUserSavedReportsAsync(int accountId);
    Task<bool> SaveReportAsync(int accountId, int articleId);
    Task<bool> RemoveBookmarkAsync(int accountId, int articleId);
    Task<bool> IsBookmarkedAsync(int accountId, int articleId);
}
