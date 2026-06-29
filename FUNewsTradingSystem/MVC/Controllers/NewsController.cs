using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[AllowAnonymous]
public class NewsController : Controller
{
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
}