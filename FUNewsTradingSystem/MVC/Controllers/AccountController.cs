using System.Security.Claims;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_MVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FUNewsTradingSystem_MVC.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ISystemAccountService _accountService;
    private readonly IPasswordHasher<SystemAccount> _passwordHasher;

    public AccountController(
        ISystemAccountService accountService,
        IPasswordHasher<SystemAccount> passwordHasher)
    {
        _accountService = accountService;
        _passwordHasher = passwordHasher;
    }

    // ──────────────────────────────────────────────
    // GET /Account/Login
    // ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already authenticated, redirect by role
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectByRole();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // ──────────────────────────────────────────────
    // POST /Account/Login
    // ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        // 1. Look up account by email (do NOT hint whether email exists)
        var account = await _accountService.GetByEmailAsync(model.Email);
        if (account == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // 2. Verify password — supports both ASP.NET Identity hash AND legacy plaintext
        //    PasswordHasher throws FormatException when the stored value is not a valid
        //    Base64 hash (i.e. still holds the pre-migration plaintext). Catch that and
        //    fall through to the direct string comparison.
        bool passwordOk;
        try
        {
            var verifyResult = _passwordHasher.VerifyHashedPassword(
                account, account.AccountPassword, model.Password);
            passwordOk = verifyResult != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Stored value is plaintext (pre-migration); compare directly
            passwordOk = false;
        }

        // Legacy fallback: plaintext stored in DB (before UpdateAdminPasswordHash migration)
        if (!passwordOk && account.AccountPassword == model.Password)
            passwordOk = true;

        if (!passwordOk)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // 3. Build ClaimsPrincipal
        var claims = new List<Claim>
        {
            new Claim("AccountID",    account.AccountID.ToString()),
            new Claim(ClaimTypes.Email,  account.AccountEmail),
            new Claim(ClaimTypes.Role,   account.AccountRole.ToString()),
            new Claim(ClaimTypes.Name,   account.AccountName),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // 4. Sign in — session cookie (IsPersistent = false)
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        // 5. Redirect by role (or returnUrl if safe)
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectByRole(account.AccountRole);
    }

    // ──────────────────────────────────────────────
    // POST /Account/Logout
    // ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ──────────────────────────────────────────────
    // GET /Account/AccessDenied
    // ──────────────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ──────────────────────────────────────────────
    // Helper: redirect by the currently signed-in user's role claim
    // ──────────────────────────────────────────────
    private IActionResult RedirectByRole(int? role = null)
    {
        // If role not explicitly supplied, read it from the current principal
        if (role == null)
        {
            var roleStr = User.FindFirstValue(ClaimTypes.Role);
            if (int.TryParse(roleStr, out var parsed))
                role = parsed;
        }

        return role switch
        {
            1 => RedirectToAction("Dashboard", "Staff"),
            2 => RedirectToAction("Index",     "News"),
            3 => RedirectToAction("Dashboard", "Admin"),
            _ => RedirectToAction("Index",     "Home"),
        };
    }
}
