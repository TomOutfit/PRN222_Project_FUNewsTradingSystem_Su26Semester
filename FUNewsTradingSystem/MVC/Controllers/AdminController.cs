using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    [HttpGet]
    public IActionResult Dashboard() => View("Index");

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Accounts() => View();

    [HttpGet]
    public IActionResult StatisticalReport() => View();
}