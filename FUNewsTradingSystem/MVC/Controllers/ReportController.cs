using System.Security.Claims;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

public class ReportController : Controller
{
    private readonly INewsArticleService _newsService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;

    public ReportController(
        INewsArticleService newsService,
        ICategoryService categoryService,
        ITagService tagService)
    {
        _newsService = newsService;
        _categoryService = categoryService;
        _tagService = tagService;
    }

    /// <summary>
    /// GET /Report, /Report/Index, /News, /News/Index - Public list of active trading analysis reports with filtering & pagination
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("Report")]
    [Route("Report/Index")]
    [Route("News")]
    [Route("News/Index")]
    public async Task<IActionResult> Index(int? page, int? categoryId, int? tagId, string? decision)
    {
        var pageNumber = PaginationSettings.ValidatePageNumber(page);
        var pageSize = PaginationSettings.DefaultPageSize;

        var categories = await _categoryService.GetAllCategoriesAsync();
        var tags = await _tagService.GetAllTagsAsync();

        ViewBag.Categories = categories.Where(c => c.IsActive).ToList();
        ViewBag.Tags = tags.ToList();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedTag = tagId;
        ViewBag.SelectedDecision = decision;

        var reports = await _newsService.GetActiveReportsAsync(categoryId, tagId, decision);

        var model = reports.Select(a => new ReportListItemViewModel
        {
            NewsArticleId = a.NewsArticleID,
            Decision = ExtractDecision(a.NewsTitle),
            NewsTitle = RemoveDecisionPrefix(a.NewsTitle),
            Headline = a.Headline,
            CreatedDate = a.CreatedDate,
            CategoryName = a.Category?.CategoryName ?? "General",
            TagNames = a.NewsTagList
                .Select(t => t.Tag.TagName)
                .ToList()
        }).ToPagedList(pageNumber, pageSize);

        return View("~/Views/Report/Index.cshtml", model);
    }

    /// <summary>
    /// GET /Report/Detail/{id}, /News/Detail/{id} - Public detailed view of a report
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("Report/Detail/{id}")]
    [Route("News/Detail/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var article = await _newsService.GetReportDetailAsync(id);

        if (article == null)
            return NotFound();

        var model = new ReportDetailViewModel
        {
            NewsArticleId = article.NewsArticleID,
            Decision = ExtractDecision(article.NewsTitle),
            NewsTitle = RemoveDecisionPrefix(article.NewsTitle),
            Headline = article.Headline,
            NewsContent = article.NewsContent,
            NewsSource = article.NewsSource,
            CreatedDate = article.CreatedDate,
            CategoryName = article.Category?.CategoryName ?? "General",
            Tags = article.NewsTagList
                .Select(t => t.Tag.TagName)
                .ToList()
        };

        return View("~/Views/Report/Details.cshtml", model);
    }

    /// <summary>
    /// GET /Staff/MyReports, /Report/MyReports - Staff member's report creation history
    /// </summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpGet]
    [Route("Staff/MyReports")]
    [Route("Report/MyReports")]
    public async Task<IActionResult> MyReports(int? page, int? categoryId, int? tagId, string? decision)
    {
        var pageNumber = PaginationSettings.ValidatePageNumber(page);
        var pageSize = PaginationSettings.DefaultPageSize;

        var accountId = User.GetAccountId() ?? int.Parse(User.FindFirst("AccountID")?.Value ?? "0");

        var categories = await _categoryService.GetAllCategoriesAsync();
        var tags = await _tagService.GetAllTagsAsync();

        ViewBag.Categories = categories.Where(c => c.IsActive).ToList();
        ViewBag.Tags = tags.ToList();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedTag = tagId;
        ViewBag.SelectedDecision = decision;

        var reports = await _newsService.GetReportsByCreatorAsync(accountId, categoryId, tagId, decision);

        var pagedReports = reports
            .ToPagedList(pageNumber, pageSize);

        return View("~/Views/Staff/MyReports/Index.cshtml", pagedReports);
    }

    /// <summary>
    /// POST /Staff/MyReports/ToggleStatus/{id}, /Report/ToggleStatus/{id} - Toggle active/archive status
    /// </summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpPost]
    [Route("Staff/MyReports/ToggleStatus/{id}")]
    [Route("Report/ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var accountId = User.GetAccountId() ?? int.Parse(User.FindFirst("AccountID")?.Value ?? "0");

        var newStatus = await _newsService.ToggleStatusAsync(id, accountId);

        return Ok(new
        {
            success = true,
            newStatus
        });
    }

    private string ExtractDecision(string title)
    {
        if (title.StartsWith("[BUY]")) return "BUY";
        if (title.StartsWith("[SELL]")) return "SELL";
        if (title.StartsWith("[HOLD]")) return "HOLD";
        return "HOLD";
    }

    private string RemoveDecisionPrefix(string title)
    {
        if (title.StartsWith("[BUY]")) return title.Replace("[BUY]", "").Trim();
        if (title.StartsWith("[SELL]")) return title.Replace("[SELL]", "").Trim();
        if (title.StartsWith("[HOLD]")) return title.Replace("[HOLD]", "").Trim();
        return title;
    }
}
