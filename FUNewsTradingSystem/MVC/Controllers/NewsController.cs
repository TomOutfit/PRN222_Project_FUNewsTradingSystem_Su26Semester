using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[AllowAnonymous]
[Route("News")]
public class NewsController : Controller
{
    private readonly INewsArticleService _newsService;

    public NewsController(INewsArticleService newsService)
    {
        _newsService = newsService;
    }

    [HttpGet("Index")]
    public async Task<IActionResult> Index(int? page)
    {
        var pageNumber = PaginationSettings.ValidatePageNumber(page);
        var pageSize = PaginationSettings.DefaultPageSize;

        var reports = await _newsService.GetActiveReportsAsync();

        var model = reports.Select(a => new ReportListItemViewModel
        {
            NewsArticleId = a.NewsArticleID,
            Decision = ExtractDecision(a.NewsTitle),
            NewsTitle = RemoveDecisionPrefix(a.NewsTitle),
            Headline = a.Headline,
            CreatedDate = a.CreatedDate,
            CategoryName = a.Category.CategoryName,
            TagNames = a.NewsTagList
                .Select(t => t.Tag.TagName)
                .ToList()
        }).OrderByDescending(r => r.CreatedDate).ToPagedList(pageNumber, pageSize);
        return View(model);
    }

    [HttpGet("Detail/{id}")]
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
            CategoryName = article.Category.CategoryName,
            Tags = article.NewsTagList
                .Select(t => t.Tag.TagName)
                .ToList()
        };

        return View(model);
    }

    private string ExtractDecision(string title)
    {
        if (title.StartsWith("[BUY]"))
            return "BUY";

        if (title.StartsWith("[SELL]"))
            return "SELL";

        if (title.StartsWith("[HOLD]"))
            return "HOLD";

        return "HOLD";
    }

    private string RemoveDecisionPrefix(string title)
    {
        if (title.StartsWith("[BUY]"))
            return title.Replace("[BUY]", "").Trim();

        if (title.StartsWith("[SELL]"))
            return title.Replace("[SELL]", "").Trim();

        if (title.StartsWith("[HOLD]"))
            return title.Replace("[HOLD]", "").Trim();

        return title;
    }
}