using System.IO;
using ClosedXML.Excel;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FUNewsTradingSystem_MVC.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Statistics")]
    public class AdminStatisticsController : Controller
    {
        private readonly INewsArticleService _newsArticleService;
        private readonly ISystemAccountService _accountService;

        public AdminStatisticsController(INewsArticleService newsArticleService, ISystemAccountService accountService)
        {
            _newsArticleService = newsArticleService;
            _accountService = accountService;
        }

        [HttpGet("/api/admin/dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var accounts = await _accountService.GetAllAsync();
                var articles = await _newsArticleService.GetActiveReportsAsync();

                int totalAccounts = accounts.Count;
                int totalStaff = accounts.Count(a => a.AccountRole == 1);
                int totalLecturers = accounts.Count(a => a.AccountRole == 2);
                int totalAdmins = accounts.Count(a => a.AccountRole == 3);

                int totalReports = articles.Count;
                int buyCount = 0;
                int sellCount = 0;
                int holdCount = 0;

                var sectorCounts = new Dictionary<string, int>();

                foreach (var a in articles)
                {
                    var title = a.NewsTitle ?? "";
                    string decision = "HOLD";
                    if (title.StartsWith("[BUY]")) decision = "BUY";
                    else if (title.StartsWith("[SELL]")) decision = "SELL";
                    else if (title.StartsWith("[HOLD]")) decision = "HOLD";

                    if (decision == "BUY") buyCount++;
                    else if (decision == "SELL") sellCount++;
                    else holdCount++;

                    var sector = a.Category?.CategoryName ?? "Unknown";
                    if (!sectorCounts.ContainsKey(sector))
                    {
                        sectorCounts[sector] = 0;
                    }
                    sectorCounts[sector]++;
                }

                var last7Days = Enumerable.Range(0, 7)
                    .Select(d => DateTime.UtcNow.Date.AddDays(-6 + d))
                    .ToList();
                var articlesLast7Days = articles
                    .Where(a => a.CreatedDate.Date >= last7Days.First())
                    .GroupBy(a => a.CreatedDate.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                var dailyAnalysisCounts = last7Days
                    .Select(d => new {
                        date = d.ToString("MM-dd"),
                        count = articlesLast7Days.ContainsKey(d) ? articlesLast7Days[d] : 0
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    totalAccounts,
                    totalStaff,
                    totalLecturers,
                    totalAdmins,
                    totalReports,
                    buyCount,
                    sellCount,
                    holdCount,
                    sectorCounts = sectorCounts.Select(kvp => new { name = kvp.Key, count = kvp.Value }).ToList(),
                    dailyCounts = dailyAnalysisCounts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? page)
        {
            var filter = new StatisticsFilterViewModel();
            if (startDate.HasValue) filter.StartDate = startDate.Value;
            if (endDate.HasValue) filter.EndDate = endDate.Value;

            var vm = new StatisticsResultViewModel
            {
                Filter = filter
            };

            if (!startDate.HasValue && !endDate.HasValue && Request.Query.Count == 0)
            {
                return View("~/Views/AdminStatistics/Index.cshtml", vm);
            }

            return await GenerateResultView(filter, page);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] StatisticsFilterViewModel filter, [FromQuery] int? page)
        {
            return await GenerateResultView(filter, page);
        }

        private async Task<IActionResult> GenerateResultView(StatisticsFilterViewModel filter, int? page)
        {
            var pageNumber = PaginationSettings.ValidatePageNumber(page);
            var pageSize = PaginationSettings.DefaultPageSize;

            var vm = new StatisticsResultViewModel
            {
                Filter = filter
            };

            if (!ModelState.IsValid)
            {
                return View("~/Views/AdminStatistics/Index.cshtml", vm);
            }

            if (filter.StartDate > filter.EndDate)
            {
                ModelState.AddModelError("Filter.StartDate", "Start date must be before or equal to end date.");
                return View("~/Views/AdminStatistics/Index.cshtml", vm);
            }

            var startUtc = filter.StartDate.Date;
            var endUtc = filter.EndDate.Date.AddDays(1).AddTicks(-1);

            var articles = await _newsArticleService.GetByDateRangeAsync(startUtc, endUtc);

            int buyCount = 0;
            int sellCount = 0;
            int holdCount = 0;
            var sectorCounts = new Dictionary<string, int>();
            
            double totalConfidence = 0;
            double buyConfidence = 0;
            double sellConfidence = 0;
            double holdConfidence = 0;

            foreach (var a in articles)
            {
                var title = a.NewsTitle ?? "";
                string decision = "HOLD";
                if (title.StartsWith("[BUY]")) decision = "BUY";
                else if (title.StartsWith("[SELL]")) decision = "SELL";
                else if (title.StartsWith("[HOLD]")) decision = "HOLD";

                if (decision == "BUY") buyCount++;
                else if (decision == "SELL") sellCount++;
                else holdCount++;

                var sector = a.Category?.CategoryName ?? "Unknown";
                if (!sectorCounts.ContainsKey(sector))
                {
                    sectorCounts[sector] = 0;
                }
                sectorCounts[sector]++;

                int confidence = a.ConfidenceScore ?? 0;
                totalConfidence += confidence;
                if (decision == "BUY") buyConfidence += confidence;
                else if (decision == "SELL") sellConfidence += confidence;
                else holdConfidence += confidence;
            }

            vm.BuyCount = buyCount;
            vm.SellCount = sellCount;
            vm.HoldCount = holdCount;
            vm.SectorCounts = sectorCounts;
            vm.AverageConfidence = articles.Count > 0 ? Math.Round(totalConfidence / articles.Count, 1) : 0;
            vm.BuyAverageConfidence = buyCount > 0 ? Math.Round(buyConfidence / buyCount, 1) : 0;
            vm.SellAverageConfidence = sellCount > 0 ? Math.Round(sellConfidence / sellCount, 1) : 0;
            vm.HoldAverageConfidence = holdCount > 0 ? Math.Round(holdConfidence / holdCount, 1) : 0;

            // Daily counts for chart (all articles, not just current page)
            vm.DailyCounts = articles
                .GroupBy(a => a.CreatedDate.Date.ToString("MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            var results = articles.OrderByDescending(a => a.CreatedDate).Select(a => new NewsArticleStatDto
            {
                NewsArticleID = a.NewsArticleID,
                NewsTitle = a.NewsTitle,
                Headline = a.Headline,
                CreatedDate = a.CreatedDate,
                CategoryName = a.Category?.CategoryName ?? "Unknown",
                CreatedByName = a.CreatedByAccount?.AccountName ?? "Deleted User",
                ConfidenceScore = a.ConfidenceScore ?? 0,
                Decision = a.NewsTitle.StartsWith("[BUY]") ? "BUY" : (a.NewsTitle.StartsWith("[SELL]") ? "SELL" : "HOLD")
            }).ToPagedList(pageNumber, pageSize);

            vm.Results = results;
            vm.HasResults = true;

            return View("~/Views/AdminStatistics/Index.cshtml", vm);
        }

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel(DateTime startDate, DateTime endDate)
        {
            var startUtc = startDate.Date;
            var endUtc = endDate.Date.AddDays(1).AddTicks(-1);

            var articles = await _newsArticleService.GetByDateRangeAsync(startUtc, endUtc);
            articles = articles.OrderByDescending(a => a.CreatedDate).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Statistics Report");

            // Header Row
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Headline";
            worksheet.Cell(1, 4).Value = "Created Date";
            worksheet.Cell(1, 5).Value = "Category";
            worksheet.Cell(1, 6).Value = "Created By";

            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0284C7");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var item in articles)
            {
                worksheet.Cell(row, 1).Value = item.NewsArticleID;
                worksheet.Cell(row, 2).Value = item.NewsTitle;
                worksheet.Cell(row, 3).Value = item.Headline;
                worksheet.Cell(row, 4).Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 5).Value = item.Category?.CategoryName ?? "Unknown";
                worksheet.Cell(row, 6).Value = item.CreatedByAccount?.AccountName ?? "Deleted User";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"Statistics_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("ExportPdf")]
        public async Task<IActionResult> ExportPdf(DateTime startDate, DateTime endDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var startUtc = startDate.Date;
            var endUtc = endDate.Date.AddDays(1).AddTicks(-1);

            var articles = await _newsArticleService.GetByDateRangeAsync(startUtc, endUtc);
            articles = articles.OrderByDescending(a => a.CreatedDate).ToList();

            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("FUNews Trading System — Statistical Report").Bold().FontSize(16).FontColor("#0284C7");
                            col.Item().Text($"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} | Total Records: {articles.Count}").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(40);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("ID");
                            header.Cell().Element(HeaderStyle).Text("Title");
                            header.Cell().Element(HeaderStyle).Text("Headline");
                            header.Cell().Element(HeaderStyle).Text("Date");
                            header.Cell().Element(HeaderStyle).Text("Category");
                            header.Cell().Element(HeaderStyle).Text("Created By");

                            static IContainer HeaderStyle(IContainer c) =>
                                c.Background("#F1F5F9").Padding(5).DefaultTextStyle(x => x.Bold().FontColor("#334155"));
                        });

                        foreach (var item in articles)
                        {
                            table.Cell().Element(CellStyle).Text(item.NewsArticleID.ToString());
                            table.Cell().Element(CellStyle).Text(item.NewsTitle ?? "");
                            table.Cell().Element(CellStyle).Text(item.Headline ?? "");
                            table.Cell().Element(CellStyle).Text(item.CreatedDate.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Element(CellStyle).Text(item.Category?.CategoryName ?? "Unknown");
                            table.Cell().Element(CellStyle).Text(item.CreatedByAccount?.AccountName ?? "Deleted User");

                            static IContainer CellStyle(IContainer c) =>
                                c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(stream);

            var fileName = $"Statistics_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }
    }
}
