using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    
}