using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "StaffOrLecturer")]
public class NewsController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
