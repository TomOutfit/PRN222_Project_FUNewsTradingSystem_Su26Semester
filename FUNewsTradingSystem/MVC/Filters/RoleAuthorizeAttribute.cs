using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FUNewsTradingSystem_MVC.Filters;

/// <summary>
/// Custom authorization filter that reads the required role(s) from the
/// <c>RequiredRole</c> property and returns 403 Forbidden or redirects to
/// Login on violation, mirroring ASP.NET Core's built-in [Authorize] behaviour
/// but with explicit 403 vs redirect differentiation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// One or more role identifiers (as strings) that are allowed to access the action.
    /// </summary>
    public string[] AllowedRoles { get; }

    /// <summary>
    /// When <c>true</c>, the filter redirects to the Login page instead of returning 403.
    /// Defaults to <c>false</c> (returns 403 Forbidden for unauthenticated users).
    /// </summary>
    public bool RedirectToLogin { get; init; }

    public RoleAuthorizeAttribute(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Not authenticated at all — redirect to login or return 403
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            if (RedirectToLogin)
            {
                var loginPath = "/Account/Login";
                var returnUrl = context.HttpContext.Request.Path;
                context.Result = new RedirectResult($"{loginPath}?returnUrl={returnUrl}");
            }
            else
            {
                context.Result = new ForbidResult();
            }
            return;
        }

        // Extract the Role claim value (stored as a string in login)
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

        // Role claim missing — treat as unauthorized
        if (string.IsNullOrEmpty(roleClaim))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check whether the user's role is in the allowed list
        if (!AllowedRoles.Contains(roleClaim, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                ContentType = "text/html",
                Content = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>403 – Access Denied</title>
    <link rel=""stylesheet"" href=""/lib/bootstrap/dist/css/bootstrap.min.css"" />
</head>
<body class=""d-flex align-items-center justify-content-center"">
    <div class=""text-center"">
        <h1 class=""display-1 text-danger"">403</h1>
        <h2>Access Denied</h2>
        <p class=""lead"">You do not have permission to view this page.</p>
        <a href=""/"" class=""btn btn-primary"">Go Home</a>
    </div>
</body>
</html>"
            };
        }
    }
}
