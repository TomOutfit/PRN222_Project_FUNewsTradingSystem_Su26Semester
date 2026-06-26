using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_MVC.Extensions;
using FUNewsTradingSystem_MVC.Helpers;
using FUNewsTradingSystem_MVC.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Accounts")]
    public class AdminAccountController : Controller
    {
        private readonly ISystemAccountService _accountService;

        public AdminAccountController(ISystemAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int? page)
        {
            var pageNumber = PaginationSettings.ValidatePageNumber(page);
            var pageSize = PaginationSettings.DefaultPageSize;

            var accounts = await _accountService.GetAllAsync();
            var pagedAccounts = accounts
                .OrderByDescending(a => a.AccountID)
                .ToPagedList(pageNumber, pageSize);
            
            return View("~/Views/AdminAccount/Index.cshtml", pagedAccounts);
        }

        [HttpGet("/Admin/Dashboard")]
        public IActionResult Dashboard()
        {
            return View("~/Views/AdminAccount/Dashboard.cshtml");
        }

        [HttpGet("CreatePartial")]
        public IActionResult CreatePartial()
        {
            var vm = new CreateAccountViewModel();
            return PartialView("~/Views/AdminAccount/_CreateAccountModal.cshtml", vm);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateAccountViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, errors });
            }

            var account = new SystemAccount
            {
                AccountName = vm.AccountName,
                AccountEmail = vm.AccountEmail,
                AccountPassword = vm.AccountPassword, // Should be hashed by P1 later
                AccountRole = vm.AccountRole
            };

            var result = await _accountService.CreateAsync(account);
            if (!result.Success)
            {
                return Json(new { success = false, errors = new[] { result.ErrorMessage } });
            }

            return Json(new { success = true });
        }

        [HttpGet("EditPartial/{id}")]
        public async Task<IActionResult> EditPartial(int id)
        {
            var account = await _accountService.GetByIdAsync(id);
            if (account == null) return NotFound();

            var vm = new EditAccountViewModel
            {
                AccountId = account.AccountID,
                AccountName = account.AccountName,
                AccountEmail = account.AccountEmail,
                AccountRole = account.AccountRole
            };

            return PartialView("~/Views/AdminAccount/_EditAccountModal.cshtml", vm);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] EditAccountViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, errors });
            }

            var account = await _accountService.GetByIdAsync(vm.AccountId);
            if (account == null) return Json(new { success = false, errors = new[] { "Account not found." } });

            account.AccountName = vm.AccountName;
            account.AccountEmail = vm.AccountEmail;
            account.AccountRole = vm.AccountRole;

            if (!string.IsNullOrWhiteSpace(vm.AccountPassword))
            {
                account.AccountPassword = vm.AccountPassword; // Hash later
            }

            var result = await _accountService.UpdateAsync(account);
            if (!result.Success)
            {
                return Json(new { success = false, errors = new[] { result.ErrorMessage } });
            }

            return Json(new { success = true });
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.GetAccountId();
            if (currentUserId == id)
            {
                return Json(new { success = false, message = "You cannot delete your own account." });
            }

            var result = await _accountService.DeleteAsync(id);
            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Failed to delete account." });
            }

            return Json(new { success = true });
        }
    }
}
