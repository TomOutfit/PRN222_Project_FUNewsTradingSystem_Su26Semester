using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.ViewModels.Report;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "StaffOnly")]
public class AnalysisController : Controller
{
    private readonly ITradingAgentService _tradingAgentService;
    private readonly ITagService _tagService;
    private readonly ICategoryService _categoryService;

    public AnalysisController(
        ITradingAgentService tradingAgentService,
        ITagService tagService,
        ICategoryService categoryService)
    {
        _tradingAgentService = tradingAgentService;
        _tagService = tagService;
        _categoryService = categoryService;
    }

    /// <summary>
    /// GET /Staff/RunAnalysis - Display the Run Analysis form with dropdowns
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new RunAnalysisViewModel();

        // Populate Ticker dropdown from Tags
        var tags = await _tagService.GetAllAsync();
        model.AvailableTags = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            tags, "TagID", "TagName");

        // Populate Sector dropdown from active Categories
        var categories = await _categoryService.GetActiveAsync();
        model.AvailableCategories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            categories, "CategoryID", "CategoryName");

        return View("~/Views/Staff/RunAnalysis.cshtml", model);
    }

    /// <summary>
    /// POST /Staff/RunAnalysis - Execute the AI Trading Pipeline
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RunAnalysis([FromBody] RunAnalysisRequest request)
    {
        if (request == null || request.SelectedTagId <= 0 || request.SelectedCategoryId <= 0)
        {
            return Json(new { success = false, errorMessage = "Please select both a ticker and a sector." });
        }

        // Get current user's account ID from claims
        var accountId = User.GetAccountId();
        if (!accountId.HasValue)
        {
            return Json(new { success = false, errorMessage = "Unable to identify current user." });
        }

        try
        {
            var result = await _tradingAgentService.RunAnalysisAsync(
                request.SelectedTagId,
                request.SelectedCategoryId,
                accountId.Value);

            return Json(new
            {
                success = result.Success,
                newsArticleId = result.NewsArticleID,
                errorMessage = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, errorMessage = "An unexpected error occurred: " + ex.Message });
        }
    }
}

/// <summary>
/// Request model for the RunAnalysis POST action
/// </summary>
public class RunAnalysisRequest
{
    public int SelectedTagId { get; set; }
    public int SelectedCategoryId { get; set; }
}
