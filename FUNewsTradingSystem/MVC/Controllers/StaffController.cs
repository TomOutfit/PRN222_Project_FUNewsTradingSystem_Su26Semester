using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FUNewsTradingSystem_MVC.ViewModels;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;

namespace FUNewsTradingSystem_MVC.Controllers;

[Authorize(Policy = "StaffOnly")]
public class StaffController : Controller
{
    private readonly ISystemAccountService _accountService;
    private readonly INewsArticleService _newsArticleService;

    public StaffController(
        ISystemAccountService accountService,
        INewsArticleService newsArticleService)
    {
        _accountService = accountService;
        _newsArticleService = newsArticleService;
    }

    /// <summary>
    /// GET /Staff/Dashboard - Display the staff dashboard with welcome message and quick stats
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var accountName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "Staff";

        ViewData["AccountName"] = accountName;

        // Get report count for this staff member
        if (int.TryParse(accountId, out int id))
        {
            var reports = await _newsArticleService.GetByCreatorAsync(id);
            ViewData["ReportCount"] = reports.Count;
        }
        else
        {
            ViewData["ReportCount"] = 0;
        }

        return View();
    }

    /// <summary>
    /// GET /Staff/Profile - Display profile management page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(accountIdClaim, out int accountId))
        {
            return RedirectToAction("Login", "Account");
        }

        var account = await _accountService.GetByIdAsync(accountId);
        if (account == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new ProfileViewModel
        {
            AccountId = account.AccountID,
            AccountName = account.AccountName,
            AccountEmail = account.AccountEmail,
            AccountRoleLabel = "Staff" // Work breakdown: AccountRoleLabel = "Staff"
        };

        return View("Profile/Index", model);
    }

    /// <summary>
    /// POST /Staff/Profile/UpdateName - Update the staff's display name
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateName(UpdateNameViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await Profile();
        }

        var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(accountIdClaim, out int accountId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _accountService.UpdateNameAsync(accountId, model.AccountName);
        if (result.Success)
        {
            TempData["NameSuccess"] = "Profile updated successfully.";
        }
        else
        {
            TempData["NameError"] = result.ErrorMessage ?? "Failed to update profile.";
        }

        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// POST /Staff/Profile/ChangePassword - Change the staff's password
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await Profile();
        }

        var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(accountIdClaim, out int accountId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _accountService.ChangePasswordAsync(
            accountId, 
            model.CurrentPassword, 
            model.NewPassword);

        if (result.Success)
        {
            TempData["PwdSuccess"] = "Password changed successfully.";
        }
        else
        {
            // Work breakdown: on fail: ModelState error "Current password is incorrect."
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return await Profile();
        }

        return RedirectToAction(nameof(Profile));
    }
}
