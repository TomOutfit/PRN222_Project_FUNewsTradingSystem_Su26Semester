using System.Security.Claims;

namespace FUNewsTradingSystem_MVC.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetAccountId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("AccountID");
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        public static int GetAccountRole(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.Role) ?? user.FindFirst("AccountRole");
            if (claim != null && int.TryParse(claim.Value, out var role))
            {
                return role;
            }
            return 0;
        }
    }
}
