using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Statistics;

namespace FUNewsTradingSystem_MVC.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Statistics")]
    public class AdminStatisticsController : Controller
    {
        private readonly INewsArticleService _newsArticleService;

        public AdminStatisticsController(INewsArticleService newsArticleService)
        {
            _newsArticleService = newsArticleService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var vm = new StatisticsResultViewModel
            {
                Filter = new StatisticsFilterViewModel()
            };
            return View("~/Views/AdminStatistics/Index.cshtml", vm);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] StatisticsFilterViewModel filter, int? page)
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

            // Server-side filter bounds
            var startUtc = filter.StartDate.Date; // 00:00:00 UTC
            var endUtc = filter.EndDate.Date.AddDays(1).AddTicks(-1); // 23:59:59 UTC

            var articles = await _newsArticleService.GetByDateRangeAsync(startUtc, endUtc);

            var results = articles.OrderByDescending(a => a.CreatedDate).Select(a => new NewsArticleStatDto
            {
                NewsArticleID = a.NewsArticleID,
                NewsTitle = a.NewsTitle,
                Headline = a.Headline,
                CreatedDate = a.CreatedDate,
                CategoryName = a.Category?.CategoryName ?? "Unknown",
                CreatedByName = a.CreatedByAccount?.AccountName ?? "Deleted User"
            }).ToPagedList(pageNumber, pageSize);

            vm.Results = results;
            vm.HasResults = true;

            return View("~/Views/AdminStatistics/Index.cshtml", vm);
        }
    }
}
