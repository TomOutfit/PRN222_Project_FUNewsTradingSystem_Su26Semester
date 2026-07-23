using System.Security.Claims;
using System.Net.Http;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.Models;
using FUNewsTradingSystem_MVC.ViewModels.Report;
using FUNewsTradingSystem_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FUNewsTradingSystem_MVC.Hubs;
using X.PagedList;

namespace FUNewsTradingSystem_MVC.Controllers;

public class ReportController : Controller
{
    private readonly INewsArticleService _newsService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly HttpClient _httpClient;
    private readonly IMarketDataService _marketDataService;
    private readonly ISavedReportService _savedReportService;
    private readonly FUNewsTradingSystem_DataAccessLayer.Models.FUNewsManagementContext _db;

    public ReportController(
        INewsArticleService newsService,
        ICategoryService categoryService,
        ITagService tagService,
        IHubContext<NotificationHub> notificationHub,
        HttpClient httpClient,
        IMarketDataService marketDataService,
        ISavedReportService savedReportService,
        FUNewsTradingSystem_DataAccessLayer.Models.FUNewsManagementContext db)
    {
        _newsService = newsService;
        _categoryService = categoryService;
        _tagService = tagService;
        _notificationHub = notificationHub;
        _httpClient = httpClient;
        _marketDataService = marketDataService;
        _savedReportService = savedReportService;
        _db = db;
    }

    /// <summary>
    /// GET /Report, /Report/Index - Public list of active trading analysis reports with filtering & pagination
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("Report")]
    [Route("Report/Index")]
    public async Task<IActionResult> Index(int? page, int? categoryId, int? tagId, string? decision, bool savedOnly = false)
    {
        var pageNumber = PaginationSettings.ValidatePageNumber(page);
        var pageSize = PaginationSettings.DefaultPageSize;

        var categories = await _categoryService.GetAllCategoriesAsync();
        var tags = await _tagService.GetAllTagsAsync();

        // savedOnly only works when authenticated
        if (!User.Identity!.IsAuthenticated) savedOnly = false;

        ViewBag.Categories = categories.Where(c => c.IsActive).ToList();
        ViewBag.Tags = tags.ToList();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedTag = tagId;
        ViewBag.SelectedDecision = decision;
        ViewBag.SavedOnly = savedOnly;

        IEnumerable<NewsArticle> sourceArticles;

        if (savedOnly)
        {
            try
            {
                var accountId = User.GetAccountId()!.Value;
                var saved = await _savedReportService.GetUserSavedReportsAsync(accountId);
                sourceArticles = saved.Select(sr => sr.NewsArticle).Where(a => a != null && a.NewsStatus);

                if (categoryId.HasValue)
                    sourceArticles = sourceArticles.Where(a => a.CategoryID == categoryId.Value);
                if (tagId.HasValue)
                    sourceArticles = sourceArticles.Where(a => a.NewsTagList.Any(t => t.TagID == tagId.Value));
                if (!string.IsNullOrEmpty(decision))
                    sourceArticles = sourceArticles.Where(a => a.NewsTitle.StartsWith($"[{decision}]", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // SavedReport table may not exist yet in production — fall back to all reports
                savedOnly = false;
                ViewBag.SavedOnly = false;
                sourceArticles = await _newsService.GetActiveReportsAsync(categoryId, tagId, decision);
            }
        }
        else
        {
            sourceArticles = await _newsService.GetActiveReportsAsync(categoryId, tagId, decision);
        }

        var model = sourceArticles.Select(a => new ReportListItemViewModel
        {
            NewsArticleId = a.NewsArticleID,
            Decision = ExtractDecision(a.NewsTitle),
            NewsTitle = RemoveDecisionPrefix(a.NewsTitle),
            Headline = a.Headline,
            CreatedDate = a.CreatedDate,
            CategoryName = a.Category?.CategoryName ?? "General",
            ConfidenceScore = a.ConfidenceScore ?? 0,
            NewsSource = a.NewsSource ?? "",
            TagNames = a.NewsTagList.Select(t => t.Tag.TagName).ToList()
        }).ToPagedList(pageNumber, pageSize);

        return View("~/Views/Report/Index.cshtml", model);
    }

    /// <summary>
    /// GET /Report/Detail/{id} - Public detailed view of a report
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("Report/Detail/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var article = await _newsService.GetReportDetailAsync(id);

        if (article == null)
            return NotFound();

        var accountId = User.GetAccountId();
        bool isBookmarked = false;
        if (accountId.HasValue)
        {
            try { isBookmarked = await _savedReportService.IsBookmarkedAsync(accountId.Value, article.NewsArticleID); }
            catch { /* SavedReport table may not exist yet in production */ }
        }

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
            CreatedById = article.CreatedByID ?? 0,
            CreatedByName = article.CreatedByAccount?.AccountName ?? "Deleted User",
            ConfidenceScore = article.ConfidenceScore ?? 0,
            Tags = article.NewsTagList
                .Select(t => t.Tag.TagName)
                .ToList(),
            IsBookmarked = isBookmarked
        };

        return View("~/Views/Report/Details.cshtml", model);
    }

    /// <summary>
    /// GET /Staff/MyReports - Staff's own report history
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
        var pagedReports = reports.ToPagedList(pageNumber, pageSize);

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
        
        await _notificationHub.Clients.All.SendAsync("ReceiveCRUDNotification", "update", "Cập Nhật Thành Công", $"Report ID {id} đã được thay đổi trạng thái thành {(newStatus ? "Active" : "Archived")}.");

        return Ok(new
        {
            success = true,
            newStatus
        });
    }

    /// <summary>
    /// GET /api/market/chart/{symbol} — Returns live price history for a symbol.
    /// Priority: (1) Yahoo Finance 6-month historical chart (primary, always populated),
    ///            (2) MarketDataService rolling live ticks (supplemental),
    ///            (3) Simulation fallback (last resort).
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("api/market/chart/{symbol}")]
    public IActionResult GetChartData(string symbol, ChartPeriod period = ChartPeriod.SixMonths)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        string normalizedSymbol = symbol.Trim().ToUpperInvariant();

        string mappedSymbol = normalizedSymbol switch
        {
            "BTC" => "BTC-USD",
            "ETH" => "ETH-USD",
            "SOL" => "SOL-USD",
            "ADA" => "ADA-USD",
            "DOGE" => "DOGE-USD",
            "XRP" => "XRP-USD",
            "BNB" => "BNB-USD",
            "LTC" => "LTC-USD",
            "DOT" => "DOT-USD",
            "LINK" => "LINK-USD",
            "FNTS" => "^IXIC",
            _ => normalizedSymbol
        };

        var (range, interval) = period switch
        {
            ChartPeriod.OneDay      => ("1d",  "5m"),
            ChartPeriod.OneWeek     => ("5d",  "1h"),
            ChartPeriod.OneMonth    => ("1mo", "1d"),
            ChartPeriod.ThreeMonths => ("3mo", "1d"),
            ChartPeriod.SixMonths   => ("6mo", "1d"),
            ChartPeriod.OneYear     => ("1y",  "1d"),
            ChartPeriod.TwoYears    => ("2y",  "1wk"),
            ChartPeriod.FiveYears   => ("5y",  "1mo"),
            _                       => ("6mo", "1d")
        };

        // ── 1. Yahoo Finance chart (primary data source) ──
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(mappedSymbol)}?range={range}&interval={interval}");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            var response = _httpClient.Send(request);
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using var doc = System.Text.Json.JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("chart", out var chartNode) &&
                    chartNode.TryGetProperty("result", out var resultList) &&
                    resultList.ValueKind == System.Text.Json.JsonValueKind.Array &&
                    resultList.GetArrayLength() > 0)
                {
                    var firstResult = resultList[0];
                    var timestamps = new List<long>();
                    if (firstResult.TryGetProperty("timestamp", out var tsProp) &&
                        tsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var ts in tsProp.EnumerateArray())
                            timestamps.Add(ts.GetInt64());

                    var closePrices = new List<double>();
                    if (firstResult.TryGetProperty("indicators", out var indNode) &&
                        indNode.TryGetProperty("quote", out var quoteList) &&
                        quoteList.ValueKind == System.Text.Json.JsonValueKind.Array &&
                        quoteList.GetArrayLength() > 0 &&
                        quoteList[0].TryGetProperty("close", out var closeProp) &&
                        closeProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var cp in closeProp.EnumerateArray())
                            closePrices.Add(cp.ValueKind == System.Text.Json.JsonValueKind.Number
                                ? cp.GetDouble()
                                : (closePrices.Count > 0 ? closePrices[^1] : 0.0));
                    }

                    var labels = new List<string>();
                    var prices = new List<double>();
                    int limit = Math.Min(timestamps.Count, closePrices.Count);
                    for (int i = 0; i < limit; i++)
                    {
                        labels.Add(DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime.ToString("MMM dd"));
                        prices.Add(Math.Round(closePrices[i], normalizedSymbol == "BTC" ? 0 : 2));
                    }

                    // Build a dictionary: date → price (for fast lookup)
                    var priceByDate = new Dictionary<string, double>();
                    for (int i = 0; i < Math.Min(labels.Count, prices.Count); i++)
                        priceByDate[labels[i]] = prices[i];

                    // Append any live MarketDataService ticks that are more recent than the last Yahoo date
                    if (_marketDataService.HasData(normalizedSymbol))
                    {
                        var liveHistory = _marketDataService.GetHistory(normalizedSymbol, maxPoints: 300);
                        if (liveHistory.Count > 0)
                        {
                            var lastYahooDate = labels.LastOrDefault();
                            DateTime lastYahooDateTime = DateTime.MinValue;
                            if (!string.IsNullOrEmpty(lastYahooDate))
                                DateTime.TryParseExact(lastYahooDate, "MMM dd", null,
                                    System.Globalization.DateTimeStyles.None, out lastYahooDateTime);

                            foreach (var tick in liveHistory)
                            {
                                if (tick.Time > lastYahooDateTime)
                                {
                                    labels.Add(tick.Time.ToString("MMM dd HH:mm"));
                                    prices.Add(Math.Round(tick.Price, normalizedSymbol == "BTC" ? 0 : 2));
                                }
                            }
                        }
                    }

                    if (prices.Count > 0)
                        return Ok(new { success = true, symbol = normalizedSymbol, labels, prices, source = "Yahoo Finance" });
                }
            }
        }
        catch { /* swallow — trigger fallback */ }

        // ── 2. MarketDataService live ticks only (if Yahoo fails) ──
        if (_marketDataService.HasData(normalizedSymbol))
        {
            var history = _marketDataService.GetHistory(normalizedSymbol, maxPoints: 120);
            if (history.Count > 0)
            {
                var labels = history.Select(h => h.Time.ToString("MMM dd HH:mm")).ToList();
                var prices = history.Select(h => Math.Round(h.Price, normalizedSymbol == "BTC" ? 0 : 2)).ToList();

                return Ok(new
                {
                    success = true,
                    symbol = normalizedSymbol,
                    labels = labels,
                    prices = prices,
                    source = "Live Market Data"
                });
            }
        }

        // ── 3. Simulation fallback ──
        var fallbackLabels = Enumerable.Range(0, 180)
            .Select(i => DateTime.UtcNow.AddDays(-179 + i).ToString("MMM dd"))
            .ToList();
        var rng = new Random();
        double startPrice = rng.Next(80, 600);
        var fallbackPrices = new List<double>();
        for (int i = 0; i < 180; i++)
        {
            double change = (rng.NextDouble() - 0.5) * 8;
            startPrice = Math.Max(10, startPrice + change);
            fallbackPrices.Add(Math.Round(startPrice, 2));
        }

        return Ok(new { success = true, symbol = normalizedSymbol, labels = fallbackLabels, prices = fallbackPrices, source = "Simulated Trend" });
    }

    /// <summary>
    /// GET /Report/DbStatus — Admin-only: check if SavedReport and TagCategoryMap tables exist
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    [Route("Report/DbStatus")]
    public async Task<IActionResult> DbStatus()
    {
        var results = new Dictionary<string, object>();
        try
        {
            var savedCount = await _db.SavedReports.CountAsync();
            results["SavedReport"] = $"OK ({savedCount} rows)";
        }
        catch (Exception ex) { results["SavedReport"] = $"MISSING: {ex.Message}"; }

        try
        {
            var mapCount = await _db.TagCategoryMaps.CountAsync();
            results["TagCategoryMap"] = $"OK ({mapCount} rows)";
        }
        catch (Exception ex) { results["TagCategoryMap"] = $"MISSING: {ex.Message}"; }

        var pending = (await _db.Database.GetPendingMigrationsAsync()).ToList();
        results["PendingMigrations"] = pending.Count == 0 ? "None" : string.Join(", ", pending);

        return Json(results);
    }

    /// <summary>
    /// POST /Report/BookmarkReport/{id} — Toggle bookmark for authenticated users
    /// </summary>
    [Authorize(Policy = "StaffOrLecturer")]
    [HttpPost]
    [Route("Report/BookmarkReport/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookmarkReport(int id)
    {
        var accountId = User.GetAccountId();
        if (accountId == null)
            return Json(new { success = false, message = "Not authenticated" });

        try
        {
            var alreadySaved = await _savedReportService.IsBookmarkedAsync(accountId.Value, id);
            if (alreadySaved)
            {
                await _savedReportService.RemoveBookmarkAsync(accountId.Value, id);
                return Json(new { success = true, bookmarked = false });
            }
            await _savedReportService.SaveReportAsync(accountId.Value, id);
            return Json(new { success = true, bookmarked = true });
        }
        catch
        {
            return Json(new { success = false, message = "Bookmark feature unavailable — database migration pending." });
        }
    }

    /// <summary>
    /// POST /Report/RemoveBookmark/{id} — Remove bookmark
    /// </summary>
    [Authorize(Policy = "StaffOrLecturer")]
    [HttpPost]
    [Route("Report/RemoveBookmark/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBookmark(int id)
    {
        var accountId = User.GetAccountId();
        if (accountId == null)
            return Json(new { success = false, message = "Not authenticated" });

        try { await _savedReportService.RemoveBookmarkAsync(accountId.Value, id); }
        catch { return Json(new { success = false, message = "Bookmark feature unavailable — database migration pending." }); }
        return Json(new { success = true, bookmarked = false });
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
