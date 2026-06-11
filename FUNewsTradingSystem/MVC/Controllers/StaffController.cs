using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FUNewsTradingSystem_MVC.Extensions;
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
        var accountId = User.GetAccountId();
        var accountName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "Staff";

        ViewData["AccountName"] = accountName;

        // Get report count for this staff member
        if (accountId.HasValue)
        {
            var reports = await _newsArticleService.GetByCreatorAsync(accountId.Value);
            ViewData["ReportCount"] = reports.Count;
        }
        else
        {
            ViewData["ReportCount"] = 0;
        }

        return View("Dashboard/Index");
    }

    /// <summary>
    /// GET /Staff/Profile - Display profile management page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var accountId = User.GetAccountId();
        if (accountId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var account = await _accountService.GetByIdAsync(accountId.Value);
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

        var accountId = User.GetAccountId();
        if (accountId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _accountService.UpdateNameAsync(accountId.Value, model.AccountName);
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

        var accountId = User.GetAccountId();
        if (accountId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _accountService.ChangePasswordAsync(
            accountId.Value, 
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

    [HttpGet]
    [Route("Staff/MyReports")]
    public async Task<IActionResult> MyReports()
    {
        var accountId =
            int.Parse(User.FindFirst("AccountID")!.Value);

        var reports =
            await _newsArticleService.GetReportsByCreatorAsync(accountId);

        return View("~/Views/Staff/MyReports/Index.cshtml", reports);
    }

    [HttpPost]
    [Route("Staff/MyReports/ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var accountId =
            int.Parse(User.FindFirst("AccountID")!.Value);

        var newStatus =
            await _newsArticleService.ToggleStatusAsync(id, accountId);

        return Ok(new
        {
            success = true,
            newStatus
        });
    }
}
