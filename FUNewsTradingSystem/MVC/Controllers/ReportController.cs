using System.Security.Claims;
using System.Net.Http;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Report;
using FUNewsTradingSystem_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FUNewsTradingSystem_MVC.Hubs;

namespace FUNewsTradingSystem_MVC.Controllers;

public class ReportController : Controller
{
    private readonly INewsArticleService _newsService;
    private readonly ICategoryService _categoryService;
    private readonly ITagService _tagService;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly HttpClient _httpClient;
    private readonly IMarketDataService _marketDataService;

    public ReportController(
        INewsArticleService newsService,
        ICategoryService categoryService,
        ITagService tagService,
        IHubContext<NotificationHub> notificationHub,
        HttpClient httpClient,
        IMarketDataService marketDataService)
    {
        _newsService = newsService;
        _categoryService = categoryService;
        _tagService = tagService;
        _notificationHub = notificationHub;
        _httpClient = httpClient;
        _marketDataService = marketDataService;
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
            ConfidenceScore = a.ConfidenceScore ?? 0,
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
            CreatedById = article.CreatedByID ?? 0,
            CreatedByName = article.CreatedByAccount?.AccountName ?? "Deleted User",
            ConfidenceScore = article.ConfidenceScore ?? 0,
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
    public IActionResult GetChartData(string symbol)
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

        // ── 1. Yahoo Finance: 6-month daily chart (primary data source) ──
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(mappedSymbol)}?range=6mo&interval=1d");
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
