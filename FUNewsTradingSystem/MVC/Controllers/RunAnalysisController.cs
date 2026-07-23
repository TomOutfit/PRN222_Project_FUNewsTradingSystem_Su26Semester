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
    private readonly IHubContext<FUNewsTradingSystem_MVC.Hubs.AnalysisProgressHub> _progressHub;
    private readonly IHubContext<FUNewsTradingSystem_MVC.Hubs.ReportHub> _reportHub;
    private readonly IServiceScopeFactory _scopeFactory;

    public RunAnalysisController(
        ITradingAgentService tradingAgentService,
        ITagService tagService,
        ICategoryService categoryService,
        IHubContext<FUNewsTradingSystem_MVC.Hubs.AnalysisProgressHub> progressHub,
        IHubContext<FUNewsTradingSystem_MVC.Hubs.ReportHub> reportHub,
        IServiceScopeFactory scopeFactory)
    {
        _tradingAgentService = tradingAgentService;
        _tagService = tagService;
        _categoryService = categoryService;
        _progressHub = progressHub;
        _reportHub = reportHub;
        _scopeFactory = scopeFactory;
    }

    [HttpGet("Staff/RunAnalysis")]
    public async Task<IActionResult> Index()
    {
        var model = new RunAnalysisViewModel();
        var tags = await _tagService.GetAllTagsAsync();
        model.AvailableTags = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(tags, "TagID", "TagName");
        var categories = await _categoryService.GetActiveAsync();
        model.AvailableCategories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "CategoryID", "CategoryName");
        return View("~/Views/Staff/RunAnalysis.cshtml", model);
    }

    [HttpGet("Staff/RunAnalysis/GetTagsByCategory")]
    public async Task<IActionResult> GetTagsByCategory(int categoryId)
    {
        if (categoryId <= 0) return Json(new { tags = Array.Empty<object>() });
        var tags = await _tagService.GetTagsByCategoryAsync(categoryId);
        return Json(new { tags = tags.Select(t => new { id = t.TagID, name = t.TagName }) });
    }

    [HttpGet("Staff/RunAnalysis/GetCategoryByTag")]
    public async Task<IActionResult> GetCategoryByTag(int tagId)
    {
        if (tagId <= 0) return Json(new { categoryId = (int?)null });
        var categoryId = await _tagService.GetCategoryByTagAsync(tagId);
        return Json(new { categoryId });
    }

    /// <summary>
    /// POST /Staff/RunAnalysis — fires the pipeline in the background and returns immediately.
    /// The result is pushed to the client via SignalR ReceiveAnalysisResult once the job finishes.
    /// This avoids Render's 30-second HTTP request timeout for long-running pipelines.
    /// </summary>
    [HttpPost("Staff/RunAnalysis")]
    public IActionResult RunAnalysis([FromBody] RunAnalysisRequest request)
    {
        if (request == null || request.SelectedTagId <= 0 || request.SelectedCategoryId <= 0)
            return Json(new { success = false, errorMessage = "Please select both a ticker and a sector." });

        var accountId = User.GetAccountId();
        if (!accountId.HasValue)
            return Json(new { success = false, errorMessage = "Unable to identify current user." });

        var connectionId = request.ConnectionId  ?? "";
        var pipeline     = request.SelectedPipeline ?? "classic";
        var depth        = request.SelectedDepth    ?? "fast";
        var provider     = request.SelectedProvider ?? "openai";
        var tagId        = request.SelectedTagId;
        var categoryId   = request.SelectedCategoryId;
        var userId       = accountId.Value;

        // Fire-and-forget: result delivered via SignalR ReceiveAnalysisResult
        _ = Task.Run(async () =>
        {
            try
            {
                // Validate tag-category pairing inside a fresh scope (scoped service)
                bool isValid;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var tagSvc = scope.ServiceProvider.GetRequiredService<ITagService>();
                    isValid = await tagSvc.ValidateTagCategoryPairingAsync(tagId, categoryId);
                }
                if (!isValid)
                {
                    await SendResult(connectionId, new { success = false, errorMessage = "The selected ticker does not belong to the chosen sector." });
                    return;
                }

                // ITradingAgentService is Singleton — safe to call directly
                var result = await _tradingAgentService.RunAnalysisAsync(
                    tagId, categoryId, userId,
                    pipeline,
                    async (message, progress) =>
                    {
                        if (!string.IsNullOrEmpty(connectionId))
                            await _progressHub.Clients.Client(connectionId)
                                .SendAsync("ReceiveProgress", message, progress);
                    },
                    depth,
                    provider);

                // Broadcast new report to all public visitors
                if (result.Success && result.NewsArticleID.HasValue)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tagSvc = scope.ServiceProvider.GetRequiredService<ITagService>();
                    var catSvc = scope.ServiceProvider.GetRequiredService<ICategoryService>();

                    var tags       = await tagSvc.GetAllTagsAsync();
                    var selectedTag = tags.Find(t => t.TagID == tagId);
                    var categoryName = "Sector Analysis";
                    try
                    {
                        var cats = await catSvc.GetActiveAsync();
                        var selectedCat = cats.Find(c => c.CategoryID == categoryId);
                        if (selectedCat != null) categoryName = selectedCat.CategoryName;
                    }
                    catch { }

                    await _reportHub.Clients.All.SendAsync("ReceiveNewReport", new
                    {
                        newsArticleId = result.NewsArticleID.Value,
                        newsTitle     = $"[{selectedTag?.Note ?? "New Asset"}] Automated Analysis",
                        categoryName,
                        createdDate   = DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm"),
                        tagName       = selectedTag?.TagName ?? "Asset"
                    });
                }

                await SendResult(connectionId, new
                {
                    success       = result.Success,
                    newsArticleId = result.NewsArticleID,
                    errorMessage  = result.ErrorMessage,
                    pipelineType  = result.PipelineType,
                    richData      = result.RichData
                });
            }
            catch (Exception ex)
            {
                await SendResult(connectionId, new { success = false, errorMessage = "Unexpected error: " + ex.Message });
            }
        });

        // Return immediately — Render 30s timeout won't be hit
        return Json(new { started = true });
    }

    private async Task SendResult(string connectionId, object payload)
    {
        if (string.IsNullOrEmpty(connectionId)) return;
        try { await _progressHub.Clients.Client(connectionId).SendAsync("ReceiveAnalysisResult", payload); }
        catch { /* client may have disconnected — swallow */ }
    }
}

public class RunAnalysisRequest
{
    public int SelectedTagId      { get; set; }
    public int SelectedCategoryId { get; set; }
    public string? ConnectionId    { get; set; }
    public string? SelectedPipeline { get; set; }
    public string? SelectedDepth    { get; set; }   // "fast" | "balanced"
    public string? SelectedProvider { get; set; }   // "openai" | "groq" | "google"
}
