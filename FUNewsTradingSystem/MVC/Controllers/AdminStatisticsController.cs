using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Statistics")]
    public class AdminStatisticsController : Controller
    {
        // Controller shell for Admin Statistical Report
    }
}
