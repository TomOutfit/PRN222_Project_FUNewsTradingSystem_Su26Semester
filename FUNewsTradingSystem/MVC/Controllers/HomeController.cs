using FUNewsTradingSystem_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FUNewsTradingSystem_MVC.Controllers
{
    public class HomeController : Controller
    {
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult FAQ()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

        [Route("Home/Error/{statusCode?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode)
        {
            var model = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                var ex = exceptionHandlerPathFeature.Error;
                ViewData["Message"] = $"[Exception] {ex.Message}{(ex.InnerException != null ? " -> " + ex.InnerException.Message : "")}";
            }
            else if (statusCode.HasValue)
            {
                ViewData["StatusCode"] = statusCode.Value;
                if (statusCode.Value == 404)
                {
                    ViewData["Message"] = "The requested trading analysis report or page could not be found.";
                }
            }
            return View(model);
        }
    }
}
