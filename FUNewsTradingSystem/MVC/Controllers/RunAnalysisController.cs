using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.ViewModels.Report;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "StaffOnly")]
public class RunAnalysisController : Controller
{
    private readonly ITradingAgentService _tradingAgentService;
    private readonly ITagService _tagService;
    private readonly ICategoryService _categoryService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<FUNewsTradingSystem_MVC.Hubs.AnalysisProgressHub> _progressHub;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<FUNewsTradingSystem_MVC.Hubs.ReportHub> _reportHub;

    public RunAnalysisController(
        ITradingAgentService tradingAgentService,
        ITagService tagService,
        ICategoryService categoryService,
        Microsoft.AspNetCore.SignalR.IHubContext<FUNewsTradingSystem_MVC.Hubs.AnalysisProgressHub> progressHub,
        Microsoft.AspNetCore.SignalR.IHubContext<FUNewsTradingSystem_MVC.Hubs.ReportHub> reportHub)
    {
        _tradingAgentService = tradingAgentService;
        _tagService = tagService;
        _categoryService = categoryService;
        _progressHub = progressHub;
        _reportHub = reportHub;
    }

    /// <summary>
    /// GET /Staff/RunAnalysis - Display the Run Analysis form with dropdowns
    /// </summary>
    [HttpGet("Staff/RunAnalysis")]
    public async Task<IActionResult> Index()
    {
        var model = new RunAnalysisViewModel();

        var tags = await _tagService.GetAllTagsAsync();
        model.AvailableTags = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            tags, "TagID", "TagName");

        var categories = await _categoryService.GetActiveAsync();
        model.AvailableCategories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            categories, "CategoryID", "CategoryName");

        return View("~/Views/Staff/RunAnalysis.cshtml", model);
    }

    /// <summary>
    /// GET /Staff/RunAnalysis/GetTagsByCategory?categoryId={id} - Returns tags for selected sector (JSON)
    /// </summary>
    [HttpGet("Staff/RunAnalysis/GetTagsByCategory")]
    public async Task<IActionResult> GetTagsByCategory(int categoryId)
    {
        if (categoryId <= 0)
            return Json(new { tags = Array.Empty<object>() });

        var tags = await _tagService.GetTagsByCategoryAsync(categoryId);
        return Json(new { tags = tags.Select(t => new { id = t.TagID, name = t.TagName }) });
    }

    /// <summary>
    /// GET /Staff/RunAnalysis/GetCategoryByTag?tagId={id} - Returns category for selected ticker (JSON)
    /// </summary>
    [HttpGet("Staff/RunAnalysis/GetCategoryByTag")]
    public async Task<IActionResult> GetCategoryByTag(int tagId)
    {
        if (tagId <= 0)
            return Json(new { categoryId = (int?)null });

        var categoryId = await _tagService.GetCategoryByTagAsync(tagId);
        return Json(new { categoryId });
    }

    /// <summary>
    /// POST /Staff/RunAnalysis - Execute the AI Trading Pipeline
    /// </summary>
    [HttpPost("Staff/RunAnalysis")]
    public async Task<IActionResult> RunAnalysis([FromBody] RunAnalysisRequest request)
    {
        if (request == null || request.SelectedTagId <= 0 || request.SelectedCategoryId <= 0)
        {
            return Json(new { success = false, errorMessage = "Please select both a ticker and a sector." });
        }

        // Validate tag-category pairing
        var isValid = await _tagService.ValidateTagCategoryPairingAsync(request.SelectedTagId, request.SelectedCategoryId);
        if (!isValid)
        {
            return Json(new { success = false, errorMessage = "The selected ticker does not belong to the chosen sector. Please select a valid pairing." });
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
                accountId.Value,
                request.SelectedPipeline ?? "classic",
                async (message, progress) =>
                {
                    if (!string.IsNullOrEmpty(request.ConnectionId))
                    {
                        await _progressHub.Clients.Client(request.ConnectionId)
                            .SendAsync("ReceiveProgress", message, progress);
                    }
                });

            if (result.Success && result.NewsArticleID.HasValue)
            {
                // Retrieve the newly created report category name/ticker to broadcast to public visitors
                var tags = await _tagService.GetAllTagsAsync();
                var selectedTag = tags.Find(t => t.TagID == request.SelectedTagId);
                var categoryName = "Sector Analysis";
                try
                {
                    var cat = await _categoryService.GetActiveAsync();
                    var selectedCat = cat.Find(c => c.CategoryID == request.SelectedCategoryId);
                    if (selectedCat != null) categoryName = selectedCat.CategoryName;
                }
                catch {}

                await _reportHub.Clients.All.SendAsync("ReceiveNewReport", new
                {
                    newsArticleId = result.NewsArticleID.Value,
                    newsTitle = $"[{selectedTag?.Note ?? "New Asset"}] Automated Analysis",
                    categoryName = categoryName,
                    createdDate = System.DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm"),
                    tagName = selectedTag?.TagName ?? "Asset"
                });
            }

            return Json(new
            {
                success = result.Success,
                newsArticleId = result.NewsArticleID,
                errorMessage = result.ErrorMessage,
                pipelineType = result.PipelineType,
                richData = result.RichData
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
    public string? ConnectionId { get; set; }
    public string? SelectedPipeline { get; set; }
}
