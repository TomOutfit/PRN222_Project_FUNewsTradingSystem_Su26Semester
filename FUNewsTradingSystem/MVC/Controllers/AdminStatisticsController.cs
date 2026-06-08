using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Statistics")]
    public class AdminStatisticsController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            var vm = new FUNewsTradingSystem_MVC.ViewModels.Statistics.StatisticsFilterViewModel();
            return View("~/Views/AdminStatistics/Index.cshtml", vm);
        }
    }
}
