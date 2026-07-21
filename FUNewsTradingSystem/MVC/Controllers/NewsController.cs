using System.Security.Claims;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.ViewModels.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

public class NewsController : Controller
{
    private readonly ISavedReportService _savedReportService;
    private readonly INewsArticleService _newsArticleService;

    public NewsController(ISavedReportService savedReportService, INewsArticleService newsArticleService)
    {
        _savedReportService = savedReportService;
        _newsArticleService = newsArticleService;
    }

    [HttpGet]
    public IActionResult Index(int? page, int? categoryId, int? tagId, string? decision)
    {
        return RedirectToAction("Index", "Report", new { page, categoryId, tagId, decision });
    }

    [HttpGet("Detail/{id}")]
    public IActionResult Detail(int id)
    {
        return RedirectToAction("Detail", "Report", new { id });
    }

    [HttpGet]
    [Authorize(Policy = "StaffOrLecturer")]
    public async Task<IActionResult> SavedReports()
    {
        var accountId = User.GetAccountId();
        if (accountId == null)
            return RedirectToAction("Login", "Account");

        var saved = await _savedReportService.GetUserSavedReportsAsync(accountId.Value);

        var viewModels = saved.Select(sr => new SavedReportViewModel
        {
            SavedReportID = sr.SavedReportID,
            SavedDate = sr.SavedDate,
            Article = new ReportListItemViewModel
            {
                NewsArticleId = sr.NewsArticle.NewsArticleID,
                NewsTitle = sr.NewsArticle.NewsTitle,
                Headline = sr.NewsArticle.Headline,
                CreatedDate = sr.NewsArticle.CreatedDate,
                CategoryName = sr.NewsArticle.Category?.CategoryName ?? "",
                TagNames = sr.NewsArticle.NewsTagList?.Select(nt => nt.Tag.TagName) ?? Enumerable.Empty<string>(),
                NewsStatus = sr.NewsArticle.NewsStatus,
                Decision = ExtractDecision(sr.NewsArticle.NewsTitle),
                ConfidenceScore = sr.NewsArticle.ConfidenceScore ?? 0
            }
        }).ToList();

        return View(viewModels);
    }

    [HttpPost]
    [Authorize(Policy = "StaffOrLecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookmarkReport(int id)
    {
        var accountId = User.GetAccountId();
        if (accountId == null)
            return Json(new { success = false, message = "Not authenticated" });

        var alreadySaved = await _savedReportService.IsBookmarkedAsync(accountId.Value, id);
        if (alreadySaved)
        {
            await _savedReportService.RemoveBookmarkAsync(accountId.Value, id);
            return Json(new { success = true, bookmarked = false });
        }

        await _savedReportService.SaveReportAsync(accountId.Value, id);
        return Json(new { success = true, bookmarked = true });
    }

    [HttpPost]
    [Authorize(Policy = "StaffOrLecturer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBookmark(int id)
    {
        var accountId = User.GetAccountId();
        if (accountId == null)
            return Json(new { success = false, message = "Not authenticated" });

        await _savedReportService.RemoveBookmarkAsync(accountId.Value, id);
        return Json(new { success = true, bookmarked = false });
    }

    private static string ExtractDecision(string title)
    {
        if (title.StartsWith("[BUY]", StringComparison.OrdinalIgnoreCase)) return "BUY";
        if (title.StartsWith("[SELL]", StringComparison.OrdinalIgnoreCase)) return "SELL";
        if (title.StartsWith("[HOLD]", StringComparison.OrdinalIgnoreCase)) return "HOLD";
        return "HOLD";
    }
}
